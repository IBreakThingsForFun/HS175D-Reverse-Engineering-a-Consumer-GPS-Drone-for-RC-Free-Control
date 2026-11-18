/*
 *  HS175D / Syma X30 Pro — ESP32 ground station
 *  ------------------------------------------------------------------
 *  Implements the reverse-engineered WiFi protocol (see docs/PROTOCOL.md):
 *    - TCP 4646  : JSON command channel  {"CMD":n,"PARAM":...}
 *    - UDP 19798 : control frames (48 00 04 LL | 68 01 0d <sticks> XS),
 *                  sent FROM local port 9961; telemetry (58 xx) returns to 9961.
 *
 *  Numbers are confirmed two ways: img_trans_app dispatch table == APK enum
 *  ordinals (decimal). CMD_SYS_PARAM_GET=0, CMD_SET_HY_LOGLEVEL=137.
 *
 *  >>> BENCH SAFETY <<<
 *  Props OFF, airframe clamped, battery within reach. Defaults stream the
 *  NEUTRAL stick frame (all axes 0x80). Never arm/throttle untethered.
 *  Axis order (which of the 4 axis bytes is throttle/yaw/pitch/roll) is
 *  UNVERIFIED — the source pcap had no maneuver packets. Use the loglevel
 *  route or a one-stick capture to confirm before trusting axis labels.
 *
 *  Serial console (115200): type 'h' for help.
 */

#include <Arduino.h>
#include <WiFi.h>
#include <WiFiUdp.h>

// ---- network ----------------------------------------------------------------
static const char *AP_SSID = "HolyStoneFPV_5G_50043d";  // open network
static const char *AP_PASS = "";

static const IPAddress DRONE_IP(192, 168, 100, 1);
static const uint16_t  CTRL_PORT   = 19798;  // UDP control + telemetry
static const uint16_t  LOCAL_PORT  = 9961;   // app's fixed local source port
static const uint16_t  CMD_PORT    = 4646;   // TCP JSON command channel

// ---- command ids (decimal, == APK enum ordinal) -----------------------------
enum {
  CMD_SYS_PARAM_GET   = 0,
  CMD_BIND_APP_UDP    = 89,
  CMD_DRONE_ID        = 91,
  CMD_SET_HY_LOGLEVEL = 137,
};

WiFiUDP    udp;
WiFiClient cmd;

// ---- control state (inner 68 01 packet) -------------------------------------
// Neutral template, 17 inner bytes (verified vs pcap):
//   68 01 0d 80 80 80 80 20 18 00 00 00 00 00 00 00 34   (XS=0x34, XOR over inner[1..15])
// inner[3..6] = 4 axis bytes (0x80 neutral); inner[10] = flag/button byte (0x00 at rest).
static uint8_t axis[4] = {0x80, 0x80, 0x80, 0x80};
static uint8_t flagByte = 0x00;       // 0x04 gyro-cal, 0x08 mag-cal, 0x40/0x80 gimbal (see docs)
static bool    streaming = true;      // stream control frames at ~18 Hz
static uint32_t lastTx = 0;

// XOR checksum over inner bytes [1 .. len-1) (excludes leading 0x68 and the XS byte).
static uint8_t innerXor(const uint8_t *inner, size_t len) {
  uint8_t x = 0;
  for (size_t i = 1; i < len - 1; i++) x ^= inner[i];
  return x;
}

// Build the full UDP datagram: 48 00 04 LL <inner LL bytes>. Returns total length.
static size_t buildControlFrame(uint8_t *out) {
  uint8_t inner[17];
  inner[0] = 0x68;            // family
  inner[1] = 0x01;            // command: stick/control
  inner[2] = 0x0d;            // payload length (13)
  inner[3] = axis[0];
  inner[4] = axis[1];
  inner[5] = axis[2];
  inner[6] = axis[3];
  inner[7] = 0x20;            // constant (speed/rate?)
  inner[8] = 0x18;            // constant
  inner[9] = 0x00;
  inner[10] = flagByte;       // flag/button (neutral 0x00; doc example showed 0xFF here)
  inner[11] = 0x00;
  inner[12] = 0x00;
  inner[13] = 0x00;
  inner[14] = 0x00;
  inner[15] = 0x00;
  inner[16] = innerXor(inner, sizeof(inner));

  out[0] = 0x48;             // wrapper
  out[1] = 0x00;
  out[2] = 0x04;
  out[3] = sizeof(inner);    // LL = inner length (0x11)
  memcpy(out + 4, inner, sizeof(inner));
  return 4 + sizeof(inner);
}

static void sendControl() {
  uint8_t buf[4 + 17];
  size_t n = buildControlFrame(buf);
  udp.beginPacket(DRONE_IP, CTRL_PORT);
  udp.write(buf, n);
  udp.endPacket();
}

// ---- TCP JSON command channel -----------------------------------------------
static bool cmdConnect() {
  if (cmd.connected()) return true;
  Serial.printf("[cmd] connecting %s:%u ... ", DRONE_IP.toString().c_str(), CMD_PORT);
  if (cmd.connect(DRONE_IP, CMD_PORT)) { Serial.println("ok"); return true; }
  Serial.println("FAILED");
  return false;
}

// Send a raw JSON command. PARAM is a JSON fragment string ("-1", "{}", "{\"loglevel\":5}").
static void sendCmd(int id, const char *paramJson) {
  if (!cmdConnect()) return;
  char json[160];
  int n = snprintf(json, sizeof(json), "{\"CMD\":%d,\"PARAM\":%s}", id, paramJson);
  cmd.write((const uint8_t *)json, n);
  Serial.printf("[cmd] >> %s\n", json);
}

static void pumpCmdReplies() {
  while (cmd.available()) {
    String line = cmd.readStringUntil('\n');
    line.trim();
    if (line.length()) Serial.printf("[cmd] << %s\n", line.c_str());
  }
}

// ---- telemetry --------------------------------------------------------------
static void pumpTelemetry() {
  int sz = udp.parsePacket();
  if (sz <= 0) return;
  uint8_t b[256];
  int n = udp.read(b, min(sz, (int)sizeof(b)));
  Serial.printf("[tlm] %2d B:", n);
  for (int i = 0; i < n; i++) Serial.printf(" %02x", b[i]);
  Serial.println();
}

// ---- serial console ---------------------------------------------------------
static void help() {
  Serial.println(F(
    "\n--- HS175D ESP32 console ---\n"
    " h            help\n"
    " s            toggle control streaming (neutral) on/off\n"
    " n            reset sticks to neutral (0x80) + flag 0x00\n"
    " t<v> y<v> p<v> r<v>   set axis[0..3] byte (hex), e.g. t90\n"
    " f<v>         set flag byte (hex), e.g. f40 (gimbal), f04 (gyro-cal)\n"
    " 0            send CMD 0  (SYS_PARAM_GET, hello)  PARAM:-1\n"
    " i            send DroneID (CMD 91)               PARAM:-1\n"
    " l<v>         send SET_HY_LOGLEVEL (CMD 137) PARAM {loglevel:v}, e.g. l5\n"
    " c<id> <param>  send arbitrary CMD, e.g. 'c126 {\"camera\":0}'\n"
    " WARNING: props off, clamped. Axis labels UNVERIFIED.\n"));
}

static uint8_t hex2(const String &s, int from) {
  return (uint8_t)strtol(s.substring(from).c_str(), nullptr, 16);
}

static void handleSerial() {
  if (!Serial.available()) return;
  String s = Serial.readStringUntil('\n');
  s.trim();
  if (!s.length()) return;
  char c = s[0];
  switch (c) {
    case 'h': help(); break;
    case 's': streaming = !streaming; Serial.printf("streaming=%d\n", streaming); break;
    case 'n': axis[0]=axis[1]=axis[2]=axis[3]=0x80; flagByte=0x00; Serial.println("neutral"); break;
    case 't': axis[0]=hex2(s,1); break;
    case 'y': axis[1]=hex2(s,1); break;
    case 'p': axis[2]=hex2(s,1); break;
    case 'r': axis[3]=hex2(s,1); break;
    case 'f': flagByte=hex2(s,1); Serial.printf("flag=0x%02x\n", flagByte); break;
    case '0': sendCmd(CMD_SYS_PARAM_GET, "-1"); break;
    case 'i': sendCmd(CMD_DRONE_ID, "-1"); break;
    case 'l': { int v = s.substring(1).toInt(); char p[24]; snprintf(p,sizeof(p),"{\"loglevel\":%d}",v); sendCmd(CMD_SET_HY_LOGLEVEL, p); } break;
    case 'c': {
      int sp = s.indexOf(' ');
      int id = s.substring(1, sp < 0 ? s.length() : sp).toInt();
      String param = sp < 0 ? "-1" : s.substring(sp + 1);
      sendCmd(id, param.c_str());
    } break;
    default: Serial.println("?  (h for help)");
  }
}

void setup() {
  Serial.begin(115200);
  delay(200);
  Serial.println("\nHS175D ESP32 ground station");

  WiFi.mode(WIFI_STA);
  WiFi.begin(AP_SSID, AP_PASS);
  Serial.printf("Joining %s ", AP_SSID);
  while (WiFi.status() != WL_CONNECTED) { Serial.print('.'); delay(400); }
  Serial.printf("\nconnected, IP=%s\n", WiFi.localIP().toString().c_str());

  udp.begin(LOCAL_PORT);     // bind local source port 9961 (telemetry returns here)
  Serial.printf("UDP bound :%u -> %s:%u\n", LOCAL_PORT, DRONE_IP.toString().c_str(), CTRL_PORT);
  help();
}

void loop() {
  handleSerial();

  if (streaming && millis() - lastTx >= 55) {   // ~18 Hz
    lastTx = millis();
    sendControl();
  }

  pumpTelemetry();
  pumpCmdReplies();
}
