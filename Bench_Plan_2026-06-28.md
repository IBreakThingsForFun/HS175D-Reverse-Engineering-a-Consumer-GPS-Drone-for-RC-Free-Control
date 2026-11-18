# HS175D Hardware RE — Bench Plan (Tomorrow, 2026-06-28)

> ⚠️ **Historical bench plan (2026-06-28) — kept for the record; two facts below are now corrected:**
> 1. **The camera SoC is the Rockchip `RV1109`**, not `RV1126` as written throughout this doc (RV1126 was
>    an early misID; RV1109 confirmed by our own RE — see `DRONE_ARCHITECTURE.md`).
> 2. **The 16 MiB SPI-NOR (`XT25F128F-W`) is the *SoC's* Linux-boot flash, not a separate "FC params"
>    chip** (§2's `MX25` hypothesis). The MM32 flight MCU has only internal flash; no separate FC-firmware
>    flash was found (see `README.md` §8).
> 3. **The flight MCU is `MM32F103CBT6`** (MindMotion; clone of STM32F103CBT6, **128 KB** flash) — not the
>    `C8T6` / "64–128 KB" written below (`C8` = 64 KB; the confirmed part is `CB` = 128 KB). Settled
>    2026-07-07 by a clear photo of the chip's own silkscreen (`MM32F103 / CBT6 / 732307-M3 / G3D6 116`).
>    `LGS79011RX7` is a separate, real marking confirmed by a second photo — it's silkscreened on the
>    **FC PCB itself** (alongside `X3C`, **not** `X30` — confirmed 2026-07-11 by direct read; this doc's
>    earlier `X30` reading was a photo `0`↔`C` misread), not on the MCU chip package.
>
> For current state see `PROJECT_STATUS.md` and `docs/RC_LINK_SNIFF.md`.

## 0. Objective

Establish **hardware-level access** to the drone's two processors, **back up everything that can be backed up**, and take the first **non-destructive** steps toward dumping the flight-controller firmware (where the arm gate lives). Perception/AI is a parallel, lower-risk track that needs no soldering.

Guiding rule: **back up first, read before write, never erase the irreplaceable chip.**

---

## 1. The system — two separate brains

```
        PC (you)
          │  WiFi JSON (UDP)  +  adb/FTP root
          ▼
  RV1126 Linux SoC  ──UART(ttyS1)──►  MM32F103 flight controller
  (camera / WiFi / NPU)               (motors / GPS / ARM LOGIC)
        │                                    │
   USB / maskrom                        SWD: DAT/CLK/GND
   (on camera board)                    I2C sensor bus (SCL/SDA)
```
> Diagram says `RV1126` — corrected to **`RV1109`** per the banner above.

- **~~RV1126~~ RV1109 board** = a small Linux computer w/ a ~~~2~~ **~1.2** TOPS NPU (2 TOPS was the RV1126 figure — RV1109 is 1.2). **You already have root.** Fully backup-able and (via maskrom) **unbrickable**.
- **MM32F103 board** = the flight controller. Holds the arm logic. **No public factory firmware** → the one place a mistake is permanent.

---

## 2. Chips / microcontrollers involved

| Chip | Role | Access | Notes |
|---|---|---|---|
| **Rockchip ~~RV1126~~ RV1109** | Camera/WiFi/AI Linux SoC | adb/FTP root; USB maskrom | Cortex-A7 + ~~2~~ **~1.2** TOPS NPU (2 = RV1126); Linux 4.19 |
| **MM32F103~~C8T6~~CBT6** | Flight controller MCU | SWD (DAT/CLK/GND) | MindMotion clone of STM32F103; Cortex-M3; ~~64–128 KB~~ **128 KB** flash; clones often have RDP **off** |
| ~~**MX25 (SOIC-8)**~~ **`XT25F128F-W` (SOIC-8)** | ~~SPI flash on FC board~~ **SoC's own Linux-boot flash** | SOIC-8 clip + flashrom | ~~Likely FC params/calibration~~ **Not FC-related — see banner; MM32 has no separate FC flash** |
| GPS module / IMU / compass | Sensors | UART (GPS) / I2C | `G_*` = GPS; `SCL/SDA` = I2C |

---

## 3. Hardware & tools checklist

- [ ] The drone + battery — **PROPS REMOVED**
- [ ] **Raspberry Pi Pico W** + USB cable
- [ ] `debugprobe_on_pico.uf2` downloaded
- [ ] Jumper wires; soldering iron, flux, fine solder
- [ ] **Multimeter** (continuity + DC volts)
- [ ] PC software: **OpenOCD**, **adb**, **rkdeveloptool**, **Zadig** (Windows WinUSB), a terminal/telnet
- [ ] (Optional) SOIC-8 test clip + **flashrom** / CH341A for the ~~MX25~~ **SoC boot flash — redundant with the MTD dump in Phase 1, see banner**
- [ ] (Optional) USB-UART adapter (the Pico debugprobe also does UART)
- [ ] USB cable / access to the **~~RV1126~~ RV1109 camera board** for maskrom (locate the port)

---

## 4. Breakout / pinout map (FC board)

| Signal | Pad | Goes to |
|---|---|---|
| SWD data (SWDIO / PA13) | **DAT** | Pico GP3 |
| SWD clock (SWCLK / PA14) | **CLK** | Pico GP2 |
| Ground (shared) | **GND** | Pico GND |
| FC→WiFi UART transmit | **TX** | sniffer RX |
| WiFi→FC UART receive | **RXW** | sniffer TX |
| I2C (sensors) | **SCL / SDA / GND / VCC** | (later) |
| 5 V rail (implant power) | **G_5V** | (later) |

**DO NOT TOUCH / DRIVE:** `G_5V`, `B+`, `M+`, motor pads (power/motor rails). `RXG`/`G_TX`/`G_RX` = GPS, ignore for now.

---

## 5. The plan — ordered phases

### Phase 0 — Prep (no hardware)
- Clean bench. Install OpenOCD / adb / rkdeveloptool. Flash Pico W with `debugprobe_on_pico.uf2` (BOOTSEL → drag-drop). Confirm `adb devices` sees the drone.

### Phase 1 — BACK UP THE LINUX SIDE (do first; safe; ~15 min)
Stream every flash partition to the PC (nothing written on device):
```sh
adb shell cat /proc/mtd          > mtd_layout.txt
adb shell cat /proc/cmdline      > cmdline.txt
adb shell "dmesg | grep -i mtd"  > mtd_dmesg.txt
# repeat per mtdblockN listed in /proc/mtd:
adb exec-out "dd if=/dev/mtdblock0 bs=4096" > mtd0.bin
adb exec-out "dd if=/dev/mtdblock1 bs=4096" > mtd1_uboot.bin
adb exec-out "dd if=/dev/mtdblock4 bs=4096" > mtd4_rootfs.bin
adb exec-out "dd if=/dev/mtdblock5 bs=4096" > mtd5_netPrivate.bin
```
Hash everything; keep **two copies** in safe locations.

### Phase 2 — Confirm the maskrom recovery path
- Locate USB on the **camera board**; enter maskrom; `rkdeveloptool ld` should list it.
- (Best backup) `rkdeveloptool db <rv1109_loader.bin>` (~~rv1126~~ — placeholder filename, corrected) → `ppt` → `rl <start> <count> fullflash.img`. This is the authoritative, restore-via-`wl` image. Maskrom is in ROM = **unbrickable by software**.

### Phase 3 — SWD first contact (THE MOMENT OF TRUTH; still non-destructive)

> ## ⛔ WIRING — DO NOT REVERSE (cost ~7h on 2026-07-10)
> **Pico `GP2 → CLK` (SWCLK), `GP3 → DAT` (SWDIO), `GND → GND`.**
> The `debugprobe` firmware drives **GP2 = SWCLK, GP3 = SWDIO** — the OPPOSITE of the intuitive "2=data, 3=clock". Wire GP2 to the drone CLK pad and GP3 to the drone DAT pad.
> - **`Error connecting DP: cannot read IDR`** while DAT idles at 3.3V ⇒ **the two signal wires are swapped.** Fix the order before touching anything else.
> - Reads DPIDR but **`Could not find MEM-AP` / `Examination failed`**, or a bit-shifted DPIDR (`0x15d00877` = `0x2ba01477` shifted 1 bit), or "works once then dead" ⇒ **cable/signal quality.** Use braided/short signal leads + a short good USB cable to the Pico. Then it's byte-stable to ≥12 MHz.
> - NRST is not wired (the resistor near the pads is on RESET, not CLK — CLK is a direct trace), so use `reset_config none` + `halt`, NOT `reset halt`.
> - ✅ Known-good invoke: `openocd -f interface/cmsis-dap.cfg -c "transport select swd" -c "reset_config none" -f target/stm32f1x.cfg -c "adapter speed 400" -c init -c halt -c "mdw 0x20000000 4" -c shutdown` → `Cortex-M3 r2p1 detected`, SRAM reads clean.

Board on its own battery, **props off**, do **not** tie 3V3 rails. Windows: Zadig WinUSB on "CMSIS-DAP v2" if needed.
```sh
openocd -f interface/cmsis-dap.cfg -c "transport select swd" -f target/stm32f1x.cfg
# in another window: telnet localhost 4444
halt
stm32f1x options_read 0            # is RDP on?
dump_image fcdump.bin 0x08000000 0x20000   # 128KB; if error, retry 0x10000
```
- **If it reads → SUCCESS:** that dump is your FC backup; you can even reflash via SWD later. Save option bytes too.
- **If empty/locked → STOP.** You have **no FC backup**. Do **NOT** `unlock`/erase. Re-plan (clone-RDP-bypass / picopwner) another day.

### Phase 4 — UART arm capture (passive, high value, zero risk)
- Sniffer RX → `TX`, sniffer TX → `RXW`, common GND. Autodetect baud (try 115200, then 57600/9600).
- Capture a full **RC arm + takeoff**. Goal: see whether the WiFi side is missing a command or waiting on FC internal state.

### Phase 5 (optional) — Secondary backups & recon
- ~~**MX25 SPI**: SOIC-8 clip + `flashrom -r mx25.bin` (non-destructive).~~ **Superseded — this chip is the SoC's own boot flash (`XT25F128F-W`), already captured by the Phase 1 MTD dump. Not a separate FC chip; see banner.**
- **I2C scan** on `SCL/SDA/GND` → identify the sensor (compass likely): `0x0D` QMC5883, `0x1E` HMC5883, `0x68` MPU6050, `0x76/77` BMP280.

### Parallel track — NPU/AI inventory (no soldering, do anytime)
```sh
adb shell "ls -l /usr/lib | grep -i rknn"     # runtime libs present?
adb shell "find / -name '*.rknn' 2>/dev/null" # existing models?
adb shell "ps"                                # what the camera daemon runs
```
Tells us how much AI is already on the device vs. what we'd add (RKNN-Toolkit v1.x → `.rknn` → run via `librknn_api`).

---

## 6. Backup & recovery matrix

| Component | Backup | Recoverable if bricked? |
|---|---|---|
| ~~RV1126~~ RV1109 boot/kernel/rootfs/config (incl. the `XT25F128F-W` SPI flash) | MTD dumps (P1) + maskrom full image (P2) | ✅ Yes — maskrom unbrickable |
| Camera/ISP tuning, `/netPrivate` | inside rootfs/mtd dumps | ✅ Yes |
| **MM32 FC firmware** | SWD dump — **only if RDP allows reading** | ⚠️ **Only if read succeeded first** |
| ~~FC params (MX25 SPI)~~ — **does not exist; MM32 has internal flash only, see banner** | ~~flashrom via clip~~ | ~~✅ Yes~~ |

---

## 7. Safety rules (the never-do list)

1. **NEVER `stm32f1x unlock`** (or any RDP-clear / mass-erase) on the MM32 — permanent brick, no recovery image.
2. **Props OFF** for everything.
3. **Don't tie the 3V3 rails** — only share GND between Pico and board.
4. **Don't drive/short** `G_5V`, `B+`, `M+`, motor pads.
5. **Meter every wire** before trusting the breakout (TX idles ~3.3 V, GND 0 V, SWD idles high).
6. **Read before write, everywhere.** Bench only.

---

## 8. Hopeful results / success criteria

- ✅ Complete, hashed **Linux backup** in hand; maskrom recovery confirmed → the camera/WiFi brain is now **unbrickable**.
- ✅ **SWD contact** with the MM32; option-byte status known.
- 🎯 **If RDP off:** full **FC firmware dump** → begin locating the **arm logic** directly (the core goal).
- 🎯 **UART arm capture** → concrete evidence of the missing command vs. internal-state gate.
- 🧠 **NPU inventory** → know what perception is already on-device for the AI track.
- 🏆 Net-new: **first hardware-level RE of this platform**, and the foundation for AI + real control across the whole board family.

---

## 9. Look up tonight (prep homework)

- **OpenOCD** + CMSIS-DAP (Pico debugprobe) basics; `dump_image`, `stm32f1x options_read`.
- **debugprobe** flashing (raspberrypi/debugprobe).
- **MM32F103~~C8T6~~CBT6** datasheet — memory map (flash @ 0x08000000), option bytes, RDP behavior vs. STM32.
- **rkdeveloptool** read/restore commands; ~~RV1126~~ RV1109 loader bin.
- **RKNN-Toolkit (v1.x / rknpu)** for ~~RV1126~~ RV1109 — model → `.rknn` workflow.
- (Contingency) **picopwner** (CTXz/stm32f1-picopwner) in case RDP is on.

---

*Two brains. Back up the one you can. Read — don't erase — the one you can't.*
