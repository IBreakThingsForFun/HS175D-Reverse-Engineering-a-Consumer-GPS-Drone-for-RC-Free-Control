# HS175D — Full Hardware & Architecture Breakdown

_A semantic map of how the whole quadcopter is put together: the chips, how they're programmed, how they talk, and what controls what. Confidence markers: ✅ confirmed by our own RE · 🔶 chip datasheet / typical design (verify on unit) · ❓ inference/hypothesis._

> **⚠ Update (post-breakthrough):** several "RC is the blocker / WiFi can't take off /
> clean bypass = emulate the RC link" statements below were written before we achieved
> **RC-free, GPS-free flight via the SWD SRAM optical-mode hold** (see [`README.md`](README.md) §6).
> That framing is correct only for a **pure-WiFi/software** attack *without a GPS fix*. The
> actual bypass was **hardware (SWD into the FC's SRAM)**, not RC emulation. The arm/mode
> gates are real but can be overpowered by continuously holding the FC's mode-state bytes
> over SWD.
>
> **Further update (2026-07-02):** even pure-WiFi can arm **when the FC holds a real GPS
> lock** — a throttle *gesture* (up past ~240, snap back to `0x80`) arms the motors with no
> RC and no SWD. So "WiFi commands alone can't take off" holds **only indoors / without a
> fix**; the arm gate is really *"RC **or** a satisfied positioning gate."* See
> [`docs/RC_LINK_SNIFF.md`](docs/RC_LINK_SNIFF.md) §11.1.

---

## 0. The one big idea: it has TWO brains
The HS175D is **two separate computers** wired together by a single serial cable:
- **App / Linux brain — Rockchip RV1109** (you have root here). Runs Linux, the camera, video, WiFi, and the phone-app protocol. ✅
- **Flight brain — MM32F103 MCU** (you have SWD here). Actually flies the aircraft: reads motion sensors, runs stabilization, drives motors. ✅
- They talk over **one UART serial link, `ttyS1`.** ✅

Almost every confusing thing about this drone comes from forgetting it's two brains with two trust levels (Linux = open to you, flight = locked + RC-gated).

---

## 1. Flight Controller — the MCU
**Chip: MM32F103CBT6** (MindMotion Microelectronics) — confirmed off a clear photo of the chip's
own silkscreen (`MM32F103 / CBT6 / 732307-M3 / G3D6 116`). ✅
- Clone/equivalent of the **STM32F103CBT6**. 🔶
- **ARM Cortex-M3**, ~72 MHz, **128 KB flash** (the `CB` suffix = 128 KB), ~20 KB SRAM. 🔶 Tiny — a real-time controller, not a computer.
- **No OS** (bare-metal / tiny RTOS): one program in a tight loop forever. 🔶

**Does:** reads IMU (gyro+accel) hundreds of times/sec; runs PID loops; drives 4 ESCs/motors via PWM; fuses GPS, barometer, compass, optical-flow for position/altitude hold; **owns ARM authority**, gated to the RC. 🔶 / ✅ (the arm gate is *the* blocker.)

**We reach it via:** SWD (DAT/CLK/GND — soldered nose header) ✅, and a TX-only **boot-log UART** ✅. Internal flash is **read-protected** → debug reads come back lossy/scrambled (clone behavior), so no clean dump. ✅

---

## 2. Application SoC — and the "Android?" question
**Chip: Rockchip RV1109.** ✅ A **vision/AI SoC built for IP security cameras**, repurposed for the drone's camera + comms.
- **Dual-core ARM Cortex-A7 + a RISC-V core + NPU + ISP.** 🔶 A real computer, vastly more powerful than the flight MCU.
- **Runs embedded Linux — NOT Android.** ✅ The **read-only squashfs** rootfs is the classic **Buildroot/Yocto** signature, not Android (no ext4 `/system`, no ART, no `init.rc`).
- **Why does `adb` work then?** `adb` is **not Android-exclusive** — Rockchip ships an `adbd` daemon in their *Linux* SDK. You get a root shell over the same protocol, but it's Linux, not Android. ✅

**Does:** camera pipeline (sensor → ISP → H.264/H.265 encoder); hosts the WiFi AP; runs **`img_trans_app`** (speaks the phone-app protocol and **bridges commands to the flight MCU over `ttyS1`**); streams RTSP video + telemetry. 🔶 / ✅

---

## 3. The NPU — what the "AI" is for
RV1109's **NPU (~1.2 TOPS)** is a dedicated neural-net block. On a camera-drone it does the **vision smarts**: follow-me/subject tracking ❓, gesture recognition ❓, possibly visual-positioning/optical-flow assist ❓.
**Key point:** the NPU does *vision, not flight*. It feeds hints (where's the subject, ground motion) to the system; the **flight MCU still makes the motor decisions.** Advisor, not pilot. ❓

---

## 4. Storage, boot, and factory programming
**Storage: XT25F128F — 16 MiB SPI NOR flash** (128 Mbit), holds the RV1109's Linux system. ✅
Partitions (mtd): `mtd1` = U-Boot · `mtd4` = rootfs (read-only squashfs) · `mtd5` = `/netPrivate` (writable vfat, **not** power-safe). ✅

**Boot (RV1109):** power → U-Boot → Linux kernel → mounts read-only squashfs root → starts services (`img_trans_app`, WiFi AP, adbd). 🔶

**Factory-programs the RV1109:** built-in **MaskROM / loader mode** over USB; tools **RKDevTool / `upgrade_tool`** write the whole image (bootloader, kernel, rootfs) to SPI flash. Recoverable via MaskROM. 🔶

**Factory-programs the MM32:** separately, via **SWD** or its **UART bootloader** (system-memory bootloader), writing the firmware `.bin/.hex`. Then **read-protection** is set so it can't be dumped — the wall we hit. 🔶 / ✅

**Two chips, two totally different programming flows. Not programmed together.**

---

## 5. The nervous system — who talks to what
```
   [ Phone App ]
        |  WiFi (2.4/5 GHz)
        v
 ┌─────────────────────┐
 │   RV1109 (Linux)    │  ← root: adb :5555 / FTP
 │  camera·video·WiFi  │
 │  img_trans_app      │
 └─────────┬───────────┘
           │  ttyS1 (UART) — the ONE bridge   ✅
           v
 ┌─────────────────────┐
 │   MM32 Flight MCU    │  ← SWD + boot-UART
 │  IMU·PID·motors      │
 └──┬───┬───┬───┬───┬──┘
    │   │   │   │   └── 2.4 GHz RC receiver → ARM authority  ✅
    │   │   │   └────── GPS module (UART)                    🔶
    │   │   └────────── Compass / Baro / IMU (I2C)           🔶❓
    │   └────────────── Optical-flow sensor (down-facing)    🔶
    └────────────────── 4× ESC / Motor (PWM)                 🔶
```

**Phone-app protocol (WiFi → RV1109):** Control = **UDP 19798** (sticks) · Commands = **TCP 4646** · Session gate = **23333** · Video = **RTSP** · telemetry back to the app. ✅

**Internal bridge:** `ttyS1` carries decoded stick/command frames **down** and telemetry **up**. ✅ The single most important wire in the craft — and readable/writable from the rooted Linux side.

---

## 6. Where ADB fits — and its limits
- `adb` (Rockchip Linux `adbd`) = **root shell on the RV1109 only.** ✅ Does **not** reach the flight MCU directly. ✅
- Lets you: push/pull files, read mtd partitions, write `/netPrivate`, inspect/restart `img_trans_app`, and **read/write `ttyS1`** to talk to the FC indirectly. ✅
- "Programming a section" via adb = dropping files/scripts on the Linux side, or talking *through* it to the FC over `ttyS1`. To program the **FC itself** you use **SWD or its UART bootloader**, not adb. 🔶
- **FTP** is a second door into the same Linux filesystem. ✅

---

## 7. The RF / RC side (the channel we can't see in software)
- Handheld **RC transmitter** → onboard **2.4 GHz receiver** over a proprietary RF protocol. 🔶
- Receiver → **flight MCU**; **this link carries ARM authority.** ✅ Why app/WiFi commands alone can't fully take off. ✅
- The one trust boundary invisible from Linux. Clean bypass = **sniff/emulate the RC→FC link**, not the WiFi side. ❓ (the "spare nose wire" idea.)

---

## 8. Power (brief)
Battery → power distribution → motors/ESCs (high current) + regulators feeding the two brains. 🔶 The RV1109 is the power-hungry one; the MM32 sips. This is also why the drone **idle-offs** — low current when not flying powers parts down, the gotcha that's been fooling SWD tests. ✅

---

## 9. Why the architecture IS the project
- **Flight decisions live in the MM32, gated to the RC.** The RV1109 and your phone can *request*; the FC decides. ✅
- **Mode-set isn't wired SoC-side** (`CMD_VEHICLE_MODE_SET` handler = NULL) → flight mode travels **only over `ttyS1`** to the FC. ✅ That's why reading `ttyS1` is the high-value move: it's the actual language the FC obeys.

---

## 10. Searchable-terms cheat sheet
- **Rockchip RV1109** — "RV1109 datasheet", "RV1109 NPU TOPS", "RV1109 Buildroot", "RV1109 ISP", "Rockchip adbd Linux", "RKDevTool MaskROM RV1109"
- **MM32F103CBT6** — "MM32F103CB datasheet", "MM32 vs STM32F103", "MindMotion MM32", "STM32F103CBT6", "Cortex-M3 SWD", "STM32 read protection RDP"
- **XT25F128F** — "XT25F128 SPI NOR 128Mbit", "16MiB SPI flash"
- **General** — "drone flight controller vs companion computer", "squashfs read-only rootfs embedded Linux", "RTSP drone video", "optical flow positioning sensor", "ESC PWM brushless", "2.4GHz RC protocol reverse engineering", "PID flight stabilization"

---

_Confidence note: ✅ items are from our own work; ✅ the 🔶/❓ items (exact sensor bus assignments, NPU usage, RF protocol) are worth confirming against a teardown / FCC photos / the boot log before relying on them._
