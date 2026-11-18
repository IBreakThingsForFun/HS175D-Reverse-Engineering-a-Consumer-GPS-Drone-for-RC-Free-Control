# Methods & Access Playbook — every way we tap, dump, monitor, inject & sniff

_One place for **all** the access/observation/injection techniques used on the HS175D across the whole
project. Motivation: we keep re-discovering easy paths we'd stopped using (e.g. watching the app's live
UDP in-app instead of pulling a PCAP). This is the checklist so that doesn't happen. Detail for each
lives in the linked source doc; this is the index + the "how do I do X again" recipe._

Sources: [DEVICE_ACCESS.md](DEVICE_ACCESS.md) · [SWD_REREAD_PROCEDURE.md](SWD_REREAD_PROCEDURE.md) ·
[RC_LINK_SNIFF.md](RC_LINK_SNIFF.md) · [PROTOCOL.md](PROTOCOL.md) · [../Bench_Plan_2026-06-28.md](../Bench_Plan_2026-06-28.md) ·
app tooling in `Form1.cs` / `ReWorkbench.cs` / `LiveDebug.cs` / `DebugBus.cs` / `DroneConsole.cs`.

---

## 0. Pick-a-technique matrix

| I want to… | Fastest method | Hardware? | Section |
|---|---|---|---|
| Watch the app's **live UDP** to/from the drone | **App PacketTap / RE Workbench Monitor** (no pcap!) | no | [3.1](#31-app-packettap--re-workbench-monitor--the-no-pcap-live-monitor) |
| Get a **root shell** on the drone | `adb connect …:5555` | no | [1.1](#11-adb--root-shell-port-5555) |
| **Browse/pull files** off the drone | FTP `HY819/1663819` or DroneConsole | no | [1.2](#12-ftp--full-filesystem) |
| Tail **logcat / kernel / MCU frames / UART** at once | **LiveDebug** (multi-source over adb) | no | [3.2](#32-livedebug--multi-source-console-over-adb) |
| See the **FC↔SoC MCU frames** | loglevel-5 dump (`CMD 137`) or LiveDebug MCU source | no | [3.3](#33-on-device-mcu-frame-dumps-loglevel-5) |
| **Wire-capture** everything the phone/app sends | **PCAPdroid** on phone → tshark | no | [4.1](#41-pcapdroid-wire-capture--tshark) |
| **Back up** the Linux side | `adb exec-out dd if=/dev/mtdblockN` | no | [2.1](#21-linux-mtd-dump-over-adb) |
| **Dump the FC firmware** | SWD `dump_image` (⚠ scrambled on our unit) | Pico+SWD | [2.2](#22-swd-flash-dump-mm32-fc) |
| **Inject control / hold a state** on the FC | SWD SRAM `mwb` flag-hold | Pico+SWD | [5.1](#51-swd-sram-flag-hold-mwb--the-inject-lever) |
| **Inject / replay** WiFi frames | RE Workbench Inject / Replay | no | [5.2](#52-wifi-packet-inject--replay) |
| **Sniff the RC-link** protocol | Pico `sigrok` on BK2425 SPI (MOSI=D5) | Pico+solder | [4.2](#42-spi-bus-tap-rc-link--pico-as-logic-analyzer) |
| **Impersonate the RC** (arm, RC-free) | ESP32 + nRF24L01+ | ESP32+nRF24 | [5.5](#55-nrf24-rc-link-impersonation-esp32) |
| Read the **FC boot log** | USART1 tap (PB6/PB7) | UART adapter | [4.3](#43-uart-taps-fc-link--usart1-console) |
| **Drive every app window headlessly** | DebugBus HTTP (`localhost`) | no | [3.4](#34-debugbus--headless-http-control) |

> **Idle-off, always:** the drone auto-powers-off in ~1–2 min with no control stream, dropping FTP/adb
> mid-task. Keep the stock app **or our app** streaming (the 23333 keepalive) while you work. adb on 5555
> is independent of the control channel, so they coexist. ([DEVICE_ACCESS.md §0](DEVICE_ACCESS.md))

---

## 1. Software access (no hardware)

### 1.1 ADB — root shell (port 5555)
```bash
adb connect 192.168.100.1:5555
adb shell            # uid=0 (root)
```
The "silent" port — unadvertised but open. platform-tools: `C:\Users\johnb\Desktop\platform-tools`.
**Windows `adb push` path gotcha:** `MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' adb push …`.

### 1.2 FTP — full filesystem
`192.168.100.1:21`, user `HY819` / pass `1663819` (hardcoded in the stock app's `FtpManager`, same on
every unit). Read/write over the **whole** FS. Our **DroneConsole** window wraps FTP + adb shell in a GUI.

### 1.3 Maskrom — RV1109 unbrickable backup
Camera board enters Rockchip maskrom (ROM, software-unbrickable). `rkdeveloptool ld` → `db <loader>` →
`rl <start> <count> full.img`. This is the authoritative restore image for the Linux side.

---

## 2. Dumping / backup

### 2.1 Linux MTD dump (over adb)
```bash
adb shell cat /proc/mtd > mtd_layout.txt
adb exec-out "dd if=/dev/mtdblock1 bs=4096" > mtd1_uboot.bin      # per partition
adb exec-out "dd if=/dev/mtdblock4 bs=4096" > mtd4_rootfs.bin     # ro squashfs
adb exec-out "dd if=/dev/mtdblock5 bs=4096" > mtd5_netPrivate.bin # the only RW partition
```
Hash everything, keep two copies. Flash map + persistence rules in [DEVICE_ACCESS.md §2–3](DEVICE_ACCESS.md).

### 2.2 SWD flash dump (MM32 FC)
Pico `debugprobe` → OpenOCD → `dump_image fcdump.bin 0x08000000 0x20000` (128 KB, CBT6).
**Status on our unit: flash reads come back SCRAMBLED** (per-batch clone-silicon scrambler, not ST RDP;
SRAM reads fine, instruction-fetch clean). Full method + the "is the lock real" sweep in
[SWD_REREAD_PROCEDURE.md](SWD_REREAD_PROCEDURE.md). **NEVER `stm32f1x unlock`** — mass-erases, no recovery image.
Open long-shot: exception/VTOR ICode-leak dump (we have clean instruction fetch — the prerequisite).

### 2.3 SPI-NOR / EEPROM reads
- **RV1109 boot flash** (`XT25F128F-W`, 16 MiB): the SoC's Linux flash — dump via adb `/dev/mtd` (no clip needed).
- **RC's 24C02 EEPROM**: SOIC-8 clip + reader → the ground-truth RC address/hop set. (The nRF24
  impersonator params ended up not needing this — see §5.5, resolved via a byte-order fix instead.)

---

## 3. Monitoring — LIVE, no pcap  ← the "easy paths we forget" section

### 3.1 App PacketTap / RE Workbench Monitor — the no-pcap live monitor
`Form1.cs` raises a static **PacketTap** on its existing send/recv paths; the **RE Workbench** Monitor
tab subscribes and shows every UDP/TCP frame the app exchanges with the drone **live**, decoded, with
no PCAPdroid round-trip. This is the "connect through the app and just watch the UDP" path — use it
first for anything about the app↔drone wire. Tabs: **Monitor** (watch), **Inject** (craft/fire),
**Replay** (record→replay a stream), **Telemetry** (decoded 58xx readout).

### 3.2 LiveDebug — multi-source console over adb
Tails several drone-side streams **at once**, merged + timestamped + filterable (`LiveDebug.cs`):
| Source | Command |
|---|---|
| LOGCAT | `logcat -v time` |
| MCU (flight frames) | `logcat \| grep -i 'app ot yt'` (needs loglevel 5 — §3.3) |
| KERNEL | `cat /proc/kmsg` |
| UART (FC) | `stty …/dev/ttyS1 raw; cat \| hexdump -C` |
| WIFI | `tcpdump -i ap0 -l -n -s 0` ⚠ **tcpdump is NOT in the stock busybox** — push a static binary or use PCAPdroid (§4.1) |
Every source is an editable adb-shell line — tweak per the drone's toolset and re-Start.

### 3.3 On-device MCU frame dumps (loglevel 5)
Raise the bridge's verbosity over TCP 4646 so it prints the FC frames to `/tmp/messages`:
`{"CMD":137,"PARAM":{"loglevel":5}}` (gates on `loglevel & 4`). Then read/grep `/tmp/messages`
(filter the `/bin/sh` respawn spam: `grep -vE "bin/sh|Scheduling|starting pid"`). Note: shows only what
the **phone sent** (the app relays) — for true axis order you still want a one-stick pcap.

### 3.4 DebugBus — headless HTTP control
Localhost JSON API (`DebugBus.cs`): `GET /log?n=200`, `GET /c/<cmd>?a=b`, `POST /` `{"cmd":…,"args":…}`.
Drives **every** window (status/log/send/adb/inject + generic `ui.*` to read/set/click any control) from
`curl` — no screen-clicking. Great for scripted capture/inject runs.

### 3.5 CommLog transcript
SQLite transcript of all comms + **PacketCatalog** novelty alerts (flags never-before-seen packet types)
+ CommLogViewer search/filter. Turns a session into a searchable record instead of scrollback.

---

## 4. Wire & RF sniffing

### 4.1 PCAPdroid wire capture + tshark
On the phone/emulator (LDPlayer), PCAPdroid captures the app↔drone wire to `.pcap`; decode with
`tshark` (`C:\Program Files\Wireshark\tshark.exe`) or the `docs/pcap_*.py` helpers. Use when you need
ground-truth off-app traffic (RemoteID, the exact bytes the native lib sends). **The Syma app's own
`db/*.log` is even better when available — it's a decoded plaintext transcript** (model/FW/battery/Armed).

### 4.2 SPI bus tap (RC link) — Pico as logic analyzer
Tap the **MM32 ⇄ BK2425 SPI** bus to read the RC arm/bind/channel protocol on the bench (no RF demod).
Pico flashed with **`sigrok-pico`** (≠ the SWD `debugprobe` — same board, different firmware). Read with
`sigrok-cli` / PulseView @ ~8 MS/s. **Decode-verified roles: CLK=D2, MOSI=D5** (D5 carries the real
`25<ch> A0<payload>` stream). **CS# needs a 1 kΩ series** or probing kills the remote. Capture the
**contiguous** block `D2,D3,D4,D5`. Full wiring/color-code/gotchas: [RC_LINK_SNIFF.md](RC_LINK_SNIFF.md).

### 4.3 UART taps (FC link + USART1 console)
- **SoC↔FC link:** `/dev/ttyS1` — tap the FC `TX`/`RXW` pads with a UART sniffer (autodetect baud:
  115200 → 57600 → 9600), or just `cat /dev/ttyS1` over adb (§3.2). Passive, zero-risk, high value.
- **FC debug console (USART1, PB6=TX/PB7=RX):** prints the FC APP boot log (`LGM4 sima`, sensor+RF init,
  build date). Separate from the SoC UART; two bauds (app vs bootloader). Open question: does RX accept a
  command shell? See [[fc-debug-console-usart1]].

### 4.4 nRF24 over-the-air (promiscuous)
Second nRF24-class radio (Flipper+nRF24 add-on, or ESP32+nRF24) in promiscuous mode to confirm the RC's
real address/channels/hop over the air — the cross-check for the SPI-tap params. (Beats SDR: 802.11
adapters and RTL-SDR can't demod the 2.4 GHz GFSK; a matching transceiver is the tool.)

### 4.5 I2C scan
On the sensor bus (`SCL/SDA/GND`) scan for the compass/IMU: `0x0D` QMC5883, `0x1E` HMC5883, `0x68`
MPU6050, `0x76/77` BMP280. (The GPS module's I2C = the magnetometer, separate from GPS UART.)

---

## 5. Injection / control

### 5.1 SWD SRAM flag-hold (`mwb`) — the inject lever
Writes work on the **running** target, but firmware **reverts one-shot writes** — so you must *hold* the
mode-state flags in a continuous `mwb` loop. Holding the complete optical-mode flag set + WiFi throttle
→ **motors spin with no RC and no GPS** ([[optical-mode-gate]]). Detach any SWD *watch/poll* first (a
watch HALTED the core mid-test — confound #1). This is the one proven RC-free control path today.

### 5.2 WiFi packet inject / replay
RE Workbench **Inject** (craft arbitrary frames, auto checksum/wrapper, fire on any port) and **Replay**
(record the live stream to a file, replay it back). Also the throttle **ramp** driver and the
`lockUnlock`/mode probe sweeps in `Form1.cs`.

### 5.3 23333 session-token replay
The control path gates on a ~10 Hz `23333` keepalive token. **A stale token replayed from an old pcap
still passes** — the drone doesn't hard-validate contents. Keep advancing the byte-23 counter (+1/s) or
the heartbeat looks frozen. ([TIMINGS.md](TIMINGS.md))

### 5.4 Throttle-gesture arm
Arm/idle = a **gesture** (throttle >~0xF0 then snap to 0x80), not a ramp. Fires over WiFi under a real
GPS lock with the RC never flipped to optical → a GPS fix opens the WiFi arm path ([[wifi-arm-gesture]]).

### 5.5 nRF24 RC-link impersonation (ESP32)
**ESP32 + nRF24L01+** transmits arm/control with no RC — **working, confirmed 2026-07-03/04**
(see [`esp32_nrf24_optical_tx`](../esp32_nrf24_optical_tx/esp32_nrf24_optical_tx.ino)). The
address mismatch that initially blocked this (drone ignoring byte-perfect TX, sniffer hearing
nothing) turned out to be a byte-order issue in the free-running SPI decode, not a wrong
capture — listening on the address reversed confirmed `LINK_ADDR = 2F 6D 23 58 A1` on hop
`19/29/39/49`. Full RC-free replay (neutral/optical/ARM) now works. See `docs/WRITEUP.md`.
**GUI control (2026-07-07):** the RE Workbench "RC-Link (nRF24)" tab (`Nrf24Panel.cs`) drives
this over serial with real buttons — Cold Start, Continuous Bind, Optical, Neutral, ARM Burst
(confirmation-gated), Brute-Force Scan, Pause/Resume, Radio Details — one-to-one with the
`.ino`'s single-char command set. This is a **separate serial connection/firmware** from
`Esp32Panel.cs`'s "ESP32" tab, which targets the unrelated `WiFiClientBasic.ino` console.

### 5.6 OF (optical-flow) frame inject on ttyS1
`ProtocolDesc.json` gives the exact OF frame (`head 104`, `[104,5,5,…]`, additive cksum). Write synthetic
OF frames to `/dev/ttyS1` → test whether it satisfies the optical-mode gate. New lever, untested.

---

## 6. Physical tap-point map (FC board)

| Signal | Pad | Wire to | Notes |
|---|---|---|---|
| SWD data (SWDIO/PA13) | **DAT** | Pico GP3 | |
| SWD clock (SWCLK/PA14) | **CLK** | Pico GP2 | |
| Ground (shared ref) | **GND** | Pico GND | **only** rail you tie |
| FC→SoC UART | **TX** | sniffer RX | passive |
| SoC→FC UART | **RXW** | sniffer TX | |
| Sensor I2C | **SCL/SDA** | (scan) | compass/IMU |
| RST (optional) | **NRST** | probe RST | connect-under-reset / boot-0 halt |

**Never drive/short:** `G_5V`, `B+`, `M+`, motor pads. `G_TX`/`G_RX`/`RXG` = GPS (UART) + `SCL/SDA` = its
compass (I2C). **Battery port (5-pin):** main `+`/`−` are the discharge leads; extra pins = midpoint /
thermistor / battery-ID (meter the real pack to map them before feeding a bench PSU).

---

## 7. Safety rules (the never-do list)
1. **NEVER `stm32f1x unlock`** / any RDP-clear on the MM32 — permanent brick, no recovery image.
2. **Props OFF** for everything on the bench.
3. **Only share GND** between probe and board — never tie the 3V3 rails.
4. **Don't drive/short** `G_5V`, `B+`, `M+`, motor pads.
5. **Meter every wire** before trusting a breakout (TX idles ~3.3 V, GND 0 V, SWD idles high).
6. **Read before write, everywhere.** One variable at a time (three-confounds discipline).
7. **Bench PSU:** current-limit LOW first (a 10 A supply into a slip = magic smoke); ≤8.4 V on a 2S pack.

---

## 8. Cross-reference
- OS access / filesystem / WiFi-band → [DEVICE_ACCESS.md](DEVICE_ACCESS.md)
- SWD flash / lock investigation → [SWD_REREAD_PROCEDURE.md](SWD_REREAD_PROCEDURE.md)
- RC-link SPI sniff / nRF24 → [RC_LINK_SNIFF.md](RC_LINK_SNIFF.md)
- Wire protocol / frames / commands → [PROTOCOL.md](PROTOCOL.md), `drone_protocol/cmd_table.txt`
- Timing / keepalive / idle-off → [TIMINGS.md](TIMINGS.md)
- Original hardware bench plan → [../Bench_Plan_2026-06-28.md](../Bench_Plan_2026-06-28.md)
- App tooling internals → `Form1.cs` (PacketTap), `ReWorkbench.cs`, `LiveDebug.cs`, `DebugBus.cs`, `DroneConsole.cs`
