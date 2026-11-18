<!-- TODO: banner photo — add once taken
<img width="2048" height="512" alt="HS175D reverse-engineering banner" src="" />
-->

# HS175D — Reverse-Engineering a Consumer GPS Drone for RC-Free Control

A complete, replicable reverse-engineering study of the **Holy Stone HS175D** GPS drone (a
rebadged **SYMA X30 Pro**, firmware identity `SMX30PRO`). The end goal: **fly the aircraft with
the stock 2.4 GHz RC transmitter switched off**, by driving the software- and debug-reachable
control paths instead of the RF link — and, along the way, mapping every other reachable surface
(WiFi control protocol, onboard Linux, the camera SoC's NPU) end to end.

This README is written as a **method guide**, not a manual for one tool. Every result below was
obtained with commodity equipment: a USB SWD probe, OpenOCD, `adb`, a way to emit UDP/TCP packets,
and (for the newest workstream) a $4 microcontroller as a logic analyzer.

> ⚠️ **Safety.** Driving the control path spins motors and can cause uncommanded thrust, flyaway,
> or injury. **All testing is done props-off, the airframe physically clamped, battery within
> reach.** Never send throttle without first confirming the aircraft is restrained.

> 🔐 **Scope.** This documents interoperability and security research on a drone physically owned
> by the author. Credentials shown are the manufacturer's own factory defaults, identical on every
> unit. The frequency-hopping sequence and a few other replay-enabling specifics are intentionally
> withheld — see [`docs/WRITEUP.md`](docs/WRITEUP.md).

---

## 1. The one idea that explains everything: two brains

The HS175D is **two independent computers** wired together by a single serial cable, at two very
different trust levels.

| | **Application SoC** | **Flight Controller (FC)** |
|---|---|---|
| Chip | **Rockchip RV1109** (vision/AI SoC) | **MM32F103CBT6** (MindMotion, STM32F103 clone) |
| Core | Dual Cortex-A7 + RISC-V + NPU + ISP | single Cortex-M3, ~72 MHz |
| Memory | 16 MiB SPI NOR (external) | 128 KB flash + 20 KB SRAM (internal) |
| OS | **Embedded Linux** (Buildroot, kernel 4.19.111) | bare-metal / tiny RTOS, one control loop |
| Runs | camera, H.264/265 video, WiFi AP, `img_trans_app` | IMU fusion, PID, motor PWM, **arm authority** |
| You reach it via | **root shell** (adb 5555 / FTP 21) over WiFi | **SWD** + a TX-only boot-log UART |
| Trust level | open to you | locked: flash read-protected, **armed only by the RC** |

The two talk over **one UART, `/dev/ttyS1`**. The SoC's `img_trans_app` daemon translates WiFi
packets into the FC's serial protocol and relays telemetry back. The RC transmitter talks to the
**FC directly over its own RF link**, invisible to the SoC — until the newest workstream (§6)
taps that link directly.

A deeper chip-by-chip breakdown (NPU, power, boot, sensor buses) is in
[`DRONE_ARCHITECTURE.md`](DRONE_ARCHITECTURE.md).

```
   [ Phone app / your packet source ]
        │  WiFi 2.4/5 GHz   (AP = 192.168.100.1)
        ▼
 ┌──────────────────────────┐
 │   RV1109  (Linux)        │  ← root: adb :5555, FTP :21
 │   camera · video · WiFi  │
 │   NPU (vision, advisor)  │
 │   img_trans_app          │
 └───────────┬──────────────┘
             │  ttyS1 (UART) — the ONE internal bridge
             ▼
 ┌──────────────────────────┐
 │   MM32F103  Flight MCU    │  ← SWD (DAT/CLK/RST) + boot-UART
 │   IMU · PID · motors      │
 └─┬───┬───┬───┬────────────┘
   │   │   │   └── 2.4 GHz RC receiver  → ARM authority
   │   │   └────── GPS module (UART)
   │   └────────── gyro/compass/baro (I²C: SDA/SCL/INT)
   └────────────── optical-flow camera (down-facing) + 4× ESC/motor (PWM)
```

---

## 2. Board access — pads and chips

The FC board carries the MM32 on one side (next to the SWD pads) and a screw-down gyro/IMU on the
reverse (on the I²C bus). Silkscreen: `X3C` / `LGS79011RX7` / `REV:07` / `2021.07.21`.

**Test pads broken out at the FC (5 wires + shared ground):**

| Pad(s) | Bus | Reaches |
|---|---|---|
| `DAT` (SWDIO/PA13), `CLK` (SWCLK/PA14), `RST` (NRST), `GND`, `VDD` | **SWD** | FC core debug — SRAM read/write, the main lever in §5 |
| `TX` (+ `RX`) | **UART** | the FC's serial link = the SoC-side `ttyS1` |
| `SDA`, `SCL`, `INT` | **I²C** | the gyro/IMU/compass (sensor inputs) |

<!-- TODO: photo pending — SWD wiring at the FC
<img width="500" alt="SWD probe wired to the flight controller's DAT/CLK/GND pads" src="" />
-->

So there are **three independent buses into the flight controller** at one corner of the board.
SWD is the one that produced results; UART and I²C are open avenues. There is no broken-out BOOT0
pad, so the MM32's ROM UART-bootloader can't be requested in software.

---

## 3. Getting into the OS (the RV1109 Linux side)

The drone is its own WiFi access point at **`192.168.100.1`**. Join it, then:

- **ADB — port 5555:** `adb connect 192.168.100.1:5555` → **root shell** (uid 0). The silent,
  undocumented door and the most useful one.
- **FTP — port 21:** user `HY819` / password `1663819` (*stupid-ftpd*, serverroot `/`, full
  read/write). Hardcoded in the stock Android app; identical on every unit.

The drone **idle-powers-off in ~1–2 min** without an active control stream, which kills adb/FTP
mid-task — keep a control/keepalive stream running, or work quickly.

### Flash map — the SoC's flash, not the FC's

| Partition | Size | Contents |
|---|---|---|
| mtd0 | 192 KB | IDBlock |
| mtd1 | 320 KB | u-boot |
| mtd2 | 128 KB | vnvm |
| mtd3 | 3 MB | boot (kernel) |
| **mtd4** | **11 MB** | **rootfs — read-only squashfs** |
| mtd5 | 1.3 MB | userdata → **`/netPrivate`** (vfat rw, the only persistent writable area) |

Total ≈ 16 MiB SPI NOR (**XT25F128F-W**). Dump the whole SoC image with `dd if=/dev/mtdblockN`
over adb — no programmer needed. `/netPrivate` is **not power-safe** (vfat over `mtdblock`); an
unclean power loss rolls it back to the factory state — which also means a hard power loss can
never brick this side.

### WiFi band / AP — 2.4 GHz *and* 5 GHz, not just 5

Ships as **5 GHz ch36** (`hw_mode=a`). A 2.4 GHz-only radio (ESP32-class) can't see it as shipped,
but the MT7663 radio is dual-band, so switching works:

- Live config = `/tmp/hostapd.conf` (RAM); `/etc/hostapd.conf` = stock baseline;
  `/netPrivate/hostapd.conf` = persistent override.
- Band is `hw_mode` (`g`=2.4GHz / `a`=5GHz) + `channel`. Apply with
  `killall hostapd; hostapd /tmp/hostapd.conf -B`, fired **detached**
  (`nohup setsid sh -c '…' </dev/null >/dev/null 2>&1 &`) because restarting hostapd drops the very
  WiFi/adb link issuing the command.
- A boot watchdog (`/netPrivate/selfheal.sh`) reverts to the stock AP if hostapd fails to start
  within ~120s — the lockout net for band/config mistakes. It does **not** catch a valid config
  with a password you then can't match, so treat SSID/password changes as high-risk.

Full filesystem/OS detail, FTP/adb walkthrough, and the exact band-switch recipe:
[`docs/DEVICE_ACCESS.md`](docs/DEVICE_ACCESS.md). Every technique used across the whole project —
tap, dump, monitor, inject, sniff — is indexed in
[`docs/METHODS_PLAYBOOK.md`](docs/METHODS_PLAYBOOK.md).

---

## 4. How communication works

### 4a. WiFi ⇄ SoC (the phone-app protocol)

| Port | Proto | Role |
|---|---|---|
| `4646` | TCP | JSON `{"CMD":n,"PARAM":…}` — camera/video/files/system/firmware. No flight commands. |
| `19798` | UDP | binary **stick/control** packets out, **telemetry** back. |
| `23333` | UDP | a sustained **session/keepalive token** (~10 Hz) that **gates** the control path. |
| `19200` | TCP | RTSP video. |

**Stick frame** (UDP → 19798): wrapper `48 00 04 LL` + inner `68 01 0d <13-byte payload> XS`,
`XS = XOR(inner[1 .. end-1])`. Four axes (roll, pitch, **throttle**, yaw), one byte each, `0–255`,
center `0x80` — all axes self-center, including throttle (`0x80`=HOLD, `>0x80` climb, `<0x80`
descend). Discrete actions (takeoff/land/…) pulse a bit in `payload[6]` for ~1s; gimbal/calibration
ride `payload[7]`. Control packets must advance their sequence/counter or they're de-duplicated.

Full wire detail, the complete flag-bit map, and the file-browsing/camera/settings command surface:
[`docs/PROTOCOL.md`](docs/PROTOCOL.md) · [`docs/COMMANDS.md`](docs/COMMANDS.md) ·
[`docs/REFERENCE.md`](docs/REFERENCE.md) · [`docs/TIMINGS.md`](docs/TIMINGS.md).

### 4b. SoC ⇄ FC (the internal `ttyS1` protocol)

Decoded from `/img_trans/bin/ProtocolDesc.json` (the firmware's own descriptor): frame format
`head=0x58`, then `type`, `len`, payload, checksum. **`0x8b`** = FC→SoC status/telemetry
(read-only). **`0x68`** = optical-flow frame the SoC feeds *to* the FC.

**Critical:** there is **no arm or mode-set command anywhere in this protocol.** The SoC can
request camera/telemetry actions, but **flight mode and arming are decided inside the FC from the
RC link** — `CMD_VEHICLE_MODE_SET` exists on the SoC side with a NULL handler. That is *why*
WiFi-only control can't arm on its own — the lever simply isn't wired on the SoC side (see §5).

---

## 5. The control problem, and the SWD breakthrough

Two nested gates, both normally driven by the RC, block a pure-software takeoff: an **arm gate**
(motors won't spin until unlocked) and a **mode gate** (GPS vs optical positioning). In GPS mode
the FC also refuses to fly without a fix (indoors = no fix = refusal); in optical mode there's no
GPS dependency.

> **Exception: WiFi arm *does* work under a real GPS lock.** With the drone holding a genuine GPS
> fix (≈13 sats) and the RC never switched to optical, the motors arm from an **over-WiFi throttle
> gesture** — throttle dragged up past ~240 then snapped back to `0x80`, mirroring the physical
> stick. So the arm gate is really *"RC **or** a satisfied positioning gate"* — a real GPS fix
> satisfies it with no RC and no SWD. Detail: [`docs/RC_LINK_SNIFF.md`](docs/RC_LINK_SNIFF.md) §11.

**The FC's internal flash is read-protected** (see [`docs/SWD_REREAD_PROCEDURE.md`](docs/SWD_REREAD_PROCEDURE.md)
for the controlled experiment that confirmed it's a real per-batch data-bus scrambler, not a
measurement artifact), **but its SRAM (`0x20000000`, 20 KB) is fully readable and writable over
SWD while the core runs.** The mode/arm state the firmware computes lives in that SRAM. By
overwriting the optical-mode state faster than the firmware recomputes it, you can hold the FC in
optical mode with **no RC and no GPS** — and the WiFi control path then accepts throttle and flies
the aircraft.

**Equipment:** any CMSIS-DAP or ST-Link SWD probe (a Raspberry Pi Pico flashed with `debugprobe`
works), wired `GP→CLK`, `GP→DAT`, `GND→GND` to the FC SWD pads; the FC on its own battery; props
off. Driver: **OpenOCD**.

```
openocd -f interface/cmsis-dap.cfg -c "transport select swd" -f target/stm32f1x.cfg \
        -c init -c "dump_image sram.bin 0x20000000 0x5000" -c shutdown
```

The load-bearing discrete flag is **`0x20000230`** (`00`=GPS → `01`=optical), found by capturing
SRAM in three RC-driven states (locked-GPS, unlocked-GPS, unlocked-optical) and diffing with a
within-state noise filter. The firmware **reverts a one-shot write within ~0.3–1 s**, so holding
the mode means re-writing the whole flag set continuously (~every 20 ms) on the running target —
an OpenOCD Tcl loop of `mwb` writes. With that loop running (RC off, GPS unplugged), a pure
WiFi throttle-up takes off — genuine optical-mode behavior, no RC, no GPS, no takeoff command.

**What it is and isn't:** this is a continuous *overpower* of firmware **outputs**, not a clean
switch — no single SRAM byte is a usable *input* lever; the nested RC-gated state machine cannot
be tripped by one write, only held. That's exactly the limitation the RC-link work in §6 set out
to remove.

> **Other candidates observed, not promoted into the hold set.** `0x20000bad` was originally read
> as a clean flight-state counter (`00`=RC off → `02`=RC on → `04`=armed), but a later boot showed
> `01→02` instead — per-boot variance means it's held as part of the set but not trusted as a clean
> enum. `0x2000021C` logged a clean `0→1` arm latch fired on the throttle-down gesture — crisper
> than `0xbad`'s fuzzy counter, but never cross-checked across boots or added to the hold set. Also
> observed: the stick array at `0x200001B8..0x1BF` (throttle = `0x200001BE`, confirmed µs values),
> a takeoff motor-ramp float at `0x200001C8/0x1CC`, and several unlabeled button-pulse addresses
> (`0x2000023C`, `0x2000043C`, `0x200004D4`, `0x20000738`) still awaiting a targeted pinning pass.
> Full list: [`dumps/control_map_2026-07-01.txt`](dumps/control_map_2026-07-01.txt).

The real, tested hold script — not just the inline sketch above — is
[`tools/hold_optical.cfg`](tools/hold_optical.cfg). The app's own one-click "SWD: Hold Optical"
button (`SwdHold.cs`) runs this exact file, but it looks for an `openocd-bin/` folder next to the
app containing the [xPack OpenOCD 0.12.0](https://github.com/xpack-dev-tools/openocd-xpack/releases)
distribution at `openocd-bin/xpack-openocd-0.12.0-7/` — that folder isn't vendored here (same
reasoning as the other external tools below), so download it and drop it in place, or just run the
command by hand as shown above with `-f tools/hold_optical.cfg -c init -c hold_optical` appended.

Full method including the three-confounds pitfalls and firmware acquisition status: this README's
companion [`docs/SWD_REREAD_PROCEDURE.md`](docs/SWD_REREAD_PROCEDURE.md)
and the original hardware bench notes in [`Bench_Plan_2026-06-28.md`](Bench_Plan_2026-06-28.md),
[`PLAN_TOMORROW.md`](PLAN_TOMORROW.md), and [`PROJECT_STATUS.md`](PROJECT_STATUS.md).

---

## 6. The real lever: tapping the RC's own radio link

The SWD hold in §5 *overpowers* firmware outputs; it never cleanly **arms**. Arm authority lives
entirely on the **2.4 GHz RC link**, invisible to both WiFi and SWD — so the newest and most
complete workstream attacks that link head-on: tap the SPI bus **between the RC transmitter's own
MCU and its radio chip**, read the arm/bind/channel protocol in the clear, and rebuild it from
scratch on commodity hardware.

<!-- TODO: photo pending — RC transmitter SPI tap
<img width="500" alt="Pico logic analyzer soldered to the RC transmitter's BK2425 SPI pads" src="" />
-->

- **Radio:** the RC link uses a **BK2425** — a Beken **nRF24L01+ clone** (same SPI command set,
  register map, and air-protocol family) behind a small PA. Because it's nRF24-compatible, an
  **nRF24L01+ module can impersonate it** once the protocol is known.
- **Tool:** a **Raspberry Pi Pico flashed with `sigrok-pico`** as a cheap SPI logic analyzer
  (nightly builds required — the `raspberrypi-pico` driver isn't in stable PulseView/sigrok-cli).
  Soldered pigtails (not clips — low-capacitance, don't slip on a fine-pitch header), read with
  `sigrok-cli`/PulseView at ≥6 MHz.
- **The gotcha that cost the most time:** probing **CE or CSN directly stops the RC from
  booting** — they're level-sensitive control lines and the analyzer's input pull tugs them off
  idle. Fix: **drop CE entirely**, and put a **1 kΩ series resistor in the CSN line** — the
  analyzer still reads it, but the MCU's push-pull output wins the bus and the RC boots normally.
- **Decoded:** steady-state frame `25 <ch>  A0 <10-byte payload>` (`0x25`=set hop channel,
  `0xA0`=W_TX_PAYLOAD). Optical-mode flag = payload byte 6 = `0x40`, sent as a **momentary pulse**
  at the button edge (the impersonator instead *holds* it — the drone accepts both). Hop rotation
  a 4-channel round-robin *(sequence withheld — see docs/WRITEUP.md "Ethics / scope")*, 2 packets/channel, ~4.4 ms period. A separate **bind address**, present on the
  wire for only a few milliseconds at power-on, hands off to the live control address during bind.
- **Result: a cold-start bind.** A fresh, unpaired aircraft accepts a from-scratch **ESP32 +
  nRF24L01+** controller with the factory transmitter switched off — bind, arm (the throttle
  gesture), mode select, and full stick control, all reproduced from a live bus tap with no
  existing protocol reference to copy from. The whole sequence is then driven end-to-end from the
  desktop app, with no manual keypresses.
- **The full 10-byte control frame is now mapped** (throttle/pitch/yaw/roll/camera/flags/CMD/
  one-shot-commands/camera-tilt/checksum, confirmed 2026-08-04) — not just arm+mode. The bind step
  also no longer needs a real drone present at all: an ESP32 can complete the RC's pairing by
  simply echoing its own bind packet back. See [`docs/RC_LINK_SNIFF.md`](docs/RC_LINK_SNIFF.md)
  §12 for both.

**The firmware itself** — the actual ESP32/Arduino `.ino` sketches, not just the writeup — is
included at the repo root: [`esp32_nrf24_optical_tx/`](esp32_nrf24_optical_tx/) is the working
impersonator; [`esp32_nrf24_rc_listen/`](esp32_nrf24_rc_listen/) is the sketch that resolved the
byte-order bug; [`esp32_nrf24_bind_ack/`](esp32_nrf24_bind_ack/) does the no-drone-needed bind
echo; the rest of the sniffing/bench-test/EEPROM tools (`esp32_nrf24_pair_sniff/`,
`esp32_nrf24_promiscuous/`, `esp32_nrf24_channel_scanner/`, `esp32_nrf24_antenna_test/`,
`esp32_nrf24_addr_listen/`, `esp32_nrf24_button_capture/`, `esp32_nrf24_loopback/`,
`esp32_nrf24_linktest/`, `esp32_24c02_dump/`, `arduino_nano_24c02_dump/`) are each one folder,
one sketch — see `docs/RC_LINK_SNIFF.md` §12.4 for what each one actually does (and its known
bugs/caveats, found from a full source read). The raw SPI/I²C sigrok captures these were built
from are in [`scan_logs/`](scan_logs/); the raw RC-link evidence files `docs/RC_LINK_SNIFF.md`
itself cites by name (boot-config burst, control-byte map, SWD re-read results) are in
[`dumps/`](dumps/).

The polished, portfolio-style writeup of this whole bypass (method, lessons, honest scope of the
claim, and why it generalizes to the rest of the Holy Stone/Syma GPS family) is
[`docs/WRITEUP.md`](docs/WRITEUP.md) — read it for the full story. The hands-on tap/wiring/sigrok
procedure, the per-session findings log, and the RC board's EEPROM/topology notes are in
[`docs/RC_LINK_SNIFF.md`](docs/RC_LINK_SNIFF.md).

> ⚠️ This is **not** a defeat of cryptography — the link is unencrypted, unauthenticated, and uses
> no rolling codes. That's why it's reproducible at all. The frequency-hopping sequence is
> intentionally withheld from the published docs.

---

## 7. The onboard NPU — real inference, no vendor support shipped

The RV1109's ~1.2 TOPS NPU is confirmed working from a cold boot with **none** of the required
driver stack present in the stock firmware — Rockchip ships the API shim but not the kernel
module, the userspace OpenVX/GAL library, or `rknn_server`. All three are acquired from Rockchip's
own open-sourced driver repo and pushed entirely into `/tmp` (nothing persistent touched).

The result: real-time multi-object detection (YOLOv5s) running on-device, with bounding boxes
drawn live over the camera feed inside the desktop app — a one-click "⚙ Start NPU" button replays
the whole bring-up sequence, survives a drone reboot, and is stable enough to fly with. Full
recipe — driver acquisition, model conversion (ONNX → `.rknn`), cross-compiling the detector
binary, the exact memory/reliability fixes that make it survive as a persistent process, and which
model sizes actually fit this hardware's tight RAM budget — is in [`docs/NPU.md`](docs/NPU.md).

---

## 8. Replication walkthrough (equipment-agnostic)

**You need:** the drone + its RC (for capturing armed/optical states); a WiFi-capable host; `adb`;
a USB SWD probe + OpenOCD; a second cheap MCU as a logic analyzer for the RC tap (§6); optionally a
USB-UART adapter and a CH341A + SOP8 clip.

1. **Open the OS.** Join `192.168.100.1`; `adb connect 192.168.100.1:5555`; explore. Dump the SoC
   flash for offline analysis.
2. **Map the WiFi protocol.** Capture the phone↔drone traffic; confirm ports
   4646/19798/23333/19200 and the stick frame (§4a). Reproduce the control stream from any UDP
   client: 23333 keepalive + advancing, `0x80`-centered, sequence-incrementing stick frames.
3. **Wire SWD to the FC.** Identify `DAT/CLK/RST/GND` by the MM32; confirm `DPIDR` and a clean
   SRAM read with OpenOCD (§5).
4. **Capture the mode states and hold optical, RC off** — run the continuous `mwb` hold loop (one
   OpenOCD session, no watchpoints), then drive throttle over WiFi. Props **off**.
5. **Tap the RC's SPI bus** (§6) — drop CE, add the 1 kΩ on CSN, capture at ≥6 MHz, decode the
   steady-state frame and the power-on boot-config burst.
6. **Rebuild the link on an ESP32 + nRF24L01+** — flash [`esp32_nrf24_optical_tx/`](esp32_nrf24_optical_tx/)
   (or match its radio config/hop schedule/frame format from scratch): reproduce the bind exchange
   from cold, follow the hop schedule, and replay arm/mode/stick frames.
7. **(Optional) Bring up the NPU** (§7) for on-device object detection.

Pitfalls that will waste your time if ignored: the idle-off timer (§3), a second SWD session
stealing the probe mid-hold, throttle/neutral not centered at `0x80`, non-incrementing control
sequences getting de-duplicated, and reading the RC's MISO line instead of MOSI on the SPI tap.

---

## 9. The desktop app, and the AirCard hotspot used alongside it

This repo includes the custom Windows desktop app used to drive everything above (control loop,
SWD/OpenOCD wrapper, the RC-link ESP32 panel, NPU bring-up, live packet monitor/inject/replay,
video + AI-detect overlay). It is **not required** to reproduce anything above — every result can
be gotten from a shell, OpenOCD, and an ESP32 sketch — but it's included because most of it is
directly reusable for this project. The photogrammetry/3D-reconstruction and flight-simulator
subsystems from the original working repo (`AirSim`, point-cloud/triangulation/waypoint-mapping)
are **not** part of this release — they're a separate project built on the same app and unrelated
to the RE work here.

The app's one-click "SWD: Hold Optical" and "⚙ Start NPU" buttons, and the "RC-Link (nRF24)" /
"ESP32" tabs, expect certain external tools/evidence to sit alongside it at runtime —
[`tools/hold_optical.cfg`](tools/hold_optical.cfg) + a separately-downloaded OpenOCD for the SWD
button (§5); [`npu_runtime/`](npu_runtime/), included here, for the NPU button; and the firmware
in `esp32_nrf24_optical_tx/`/`WiFiClientBasic/` flashed onto actual hardware for the two serial
tabs. None of this is required to read the code or the docs — only to drive the live hardware.

**One more piece worth knowing about:** a rooted **NETGEAR AirCard 797S** LTE hotspot is used on
the bench as a WiFi router/repeater — each Holy Stone drone ships as its own fixed-IP AP
(`192.168.100.1`), so joining more than one at once collides; the AirCard (in AP-STA mode) 1:1-NATs
each drone onto its own non-colliding subnet so several can be reached from one PC without
modifying any drone. That rooting/dumping work is its own standalone project, fully written up
separately: **[NETGEAR AirCard 797S — Root & Firmware Dump](https://github.com/IBreakThingsForFun/NETGEAR-AirCard-797S-AC797S-Root-Firmware-Dump)**.
The app's `Aircard.cs`/`AircardRoot.cs`/`DroneBridge.cs`/`PcNet.cs` drive that multi-drone bridge
from this side.

---

## 10. Companion documents

- [`DRONE_ARCHITECTURE.md`](DRONE_ARCHITECTURE.md) — chip-by-chip hardware breakdown
- [`docs/PROTOCOL.md`](docs/PROTOCOL.md) — WiFi wire protocol (ports, frames, checksums, arming)
- [`docs/COMMANDS.md`](docs/COMMANDS.md) — the full TCP-4646 JSON command table (166 commands)
- [`docs/REFERENCE.md`](docs/REFERENCE.md) — complete device reference (every flight/telemetry/setting field)
- [`docs/SENSORS.md`](docs/SENSORS.md) — cameras, optical flow, GPS/IMU/compass
- [`docs/TIMINGS.md`](docs/TIMINGS.md) — every protocol cadence in one place
- [`docs/DEVICE_ACCESS.md`](docs/DEVICE_ACCESS.md) — root shell, filesystem, WiFi band switching
- [`docs/METHODS_PLAYBOOK.md`](docs/METHODS_PLAYBOOK.md) — index of every tap/dump/monitor/inject/sniff technique
- [`docs/RC_LINK_SNIFF.md`](docs/RC_LINK_SNIFF.md) — the RC-link SPI tap, wiring, sigrok setup, findings log
- [`docs/SWD_REREAD_PROCEDURE.md`](docs/SWD_REREAD_PROCEDURE.md) — the controlled "is the flash lock real" experiment
- [`docs/WRITEUP.md`](docs/WRITEUP.md) — the polished full RC-link bypass writeup
- [`docs/NPU.md`](docs/NPU.md) — on-device NPU bring-up and live object detection
- [`docs/RESEARCH.md`](docs/RESEARCH.md) — OEM lineage, app family, protocol heritage
- [`docs/STATUS_2026-07-07.md`](docs/STATUS_2026-07-07.md) — a dated correlation review cross-checking captures against the docs
- [`drone_protocol/`](drone_protocol/) — the `img_trans_app` binary, firmware descriptors, disassembly tooling, and `docs/PCAPdroid_*.pcap`/`docs/jadx_out`/`docs/extract_cmds.py`/`docs/pcap_*.py` for re-deriving the WiFi protocol and command tables from scratch
- [`esp32_nrf24_optical_tx/`](esp32_nrf24_optical_tx/) and its sibling `esp32_nrf24_*/`, `esp32_24c02_dump/`, `arduino_nano_24c02_dump/`, `esp32_xbox_*/`, and `WiFiClientBasic/` folders — every ESP32/Arduino sketch built for the RC-link and bench work (see `docs/RC_LINK_SNIFF.md` §12.4 for what each one does)
- [`tools/`](tools/) — the actual OpenOCD configs/scripts for the SWD hold loop and UART inject/hook helpers (minus the vendored Zig toolchain used to cross-compile `ttyhook.so`/`ttyinject.so` — those two `.so` files are included prebuilt)
- [`dumps/`](dumps/) and [`scan_logs/`](scan_logs/) — the raw SRAM/SPI/I²C capture evidence `docs/RC_LINK_SNIFF.md`, `docs/SWD_REREAD_PROCEDURE.md`, and `docs/WRITEUP.md` cite by filename
- [`Bench_Plan_2026-06-28.md`](Bench_Plan_2026-06-28.md) / [`PLAN_TOMORROW.md`](PLAN_TOMORROW.md) / [`PROJECT_STATUS.md`](PROJECT_STATUS.md) — the original hardware bench plan and day-by-day project history
