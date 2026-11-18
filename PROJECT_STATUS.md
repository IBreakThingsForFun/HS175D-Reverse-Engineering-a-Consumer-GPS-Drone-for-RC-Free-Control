# HS175D — Project Status & Story

_The narrative companion to `PLAN_TOMORROW.md` (the checklist) and `DRONE_ARCHITECTURE.md` (the hardware map). This is the "where we've been and where we're going" version. Updated 2026-07-02 (RC-link protocol decoded but params suspect; WiFi arm-via-GPS-lock found — see `docs/RC_LINK_SNIFF.md` §8–11)._

---

## The one-line summary
We couldn't break *into* the flight firmware (it's read-protected and locked), so we've been learning to **steer it from the outside** — and we've already proven that works once: holding the full mode-state set + throttle, with no RC and no GPS, made the motors spin. Everything ahead is about turning that one brute-force success into something **precise and permanent**.

---

## The journey so far (start → now)

| # | What we did | Outcome |
|---|-------------|---------|
| 1 | **Rooted the Linux side** — adb :5555 + FTP, mapped the flash (mtd partitions) | ✅ worked |
| 2 | **Soldered permanent SWD + UART** to a 2-screw nose header | ✅ worked — permanent lab access |
| 3 | **Tried to dump the FC flash via SWD** | ❌ blocked — clone read-protection, scrambled reads |
| 4 | **Reverse-engineered the protocols** — WiFi ports + `img_trans` binary | ✅ key finding: `ttyS1` is the path; `CMD_VEHICLE_MODE_SET` is NULL SoC-side |
| 5 | **Forced arm-state / a partial mode set via SWD** | ❌ no motor authority; hybrid / fault behavior |
| 6 | **Co-injection (phone + probe together)** | ✅ breakthrough — drone accepts outside control; the phone supplies the missing gate |
| 7 | **Held the COMPLETE mode set + WiFi throttle** (GPS unplugged, RC off) | ✅✅ THE WIN — motors spin, RC-free, GPS-free |
| 8 | **Cracked the flight model** — all axes self-center at 128; throttle = climb-rate request | ✅ insight: engage flight-state FIRST, then throttle |
| 9 | **Opened the RC-link vector** — Pico-as-logic-analyzer tapping the MM32⇄BK2425 SPI bus (the arm-authority channel) | ✅ tap works; frame `25<ch> A0<payload>` decoded; optical flag = payload byte 6 `0x40`; hop + boot config captured — but from a free-running (no-CS#) decode |
| 10 | **Built an nRF24 impersonator + resolved the params (2026-07-03/04)** | ✅ address mismatch was a byte-order issue, not a bad capture — reversed-order listen confirmed the live link address *(withheld — see docs/WRITEUP.md §Ethics)*; impersonator now does full RC-free replay (neutral/optical/ARM), confirmed over the air. See `docs/WRITEUP.md`. |
| 11 | **Found a WiFi arm path under GPS lock** | ✅ throttle *gesture* (up >~240 → snap to `0x80`) arms the motors over WiFi with a real GPS fix, **no RC, no SWD** — arm gate = "RC **or** satisfied positioning gate" |

**Shape of the story:** the *access* battles were won early (root + permanent SWD/UART). The *walls* were the locked FC firmware (can't dump) and the authority gate (forcing arm or a partial mode gives nothing or a fault). The *turning points* were both about going **around** the locked firmware: co-injection (phone fills the gate) and the full-mode-set hold (motors actually spin). The model insight explained why earlier throttle pokes did nothing — throttle is a request to a controller that's asleep until the flight state is engaged.

---

## Where we are right now
- A confirmed, **repeatable hardware lab**: rooted Linux side, persistent SWD, persistent boot-UART, easy nose-cap access, one spare wire staged.
- **One proven path to RC-free motor authority** — but it currently needs brute-force holding of the full mode set over SWD.
- **Not yet done:** the complete mode set isn't fully enumerated, and the stick map isn't precise.

---

## The road ahead (the phases)
0. **Verify reassembly** — boot log + SWD sanity + app builds. (Did soldering survive the rebuild?)
1. **Capture `ttyS1`** ⭐ — read the FC's trusted command stream off the wire; diff idle vs takeoff → the **engage flag**. Software-only, highest ROI.
2. **Map the sticks** — single-axis captures with a liveness guard; pin the exact throttle/roll/pitch/yaw addresses.
3. **Forceability test** — engage the full state FIRST, confirm liveness, THEN ramp throttle 128→up.
4. **Optical vs baseline diff** — capture optical-mode SRAM vs baseline; the diff is the complete mode set to hold.
5. **Converge & deploy** — hold the complete set + throttle for coherent control; move it onto an **ESP32 implant** for permanent, laptop-free, RC-free flight.

**Immediate next move:** Phase 0, then **Phase 1 (the `ttyS1` capture)** — if it reveals the engage flag, the later phases get much easier because we stop *guessing* the mode set from SRAM and start *reading* it directly.

### Parallel track: the RC link (arm authority)
A second, independent line of attack now runs alongside the SRAM/`ttyS1` work: **read the
RC transmitter's own radio protocol** by tapping the SPI bus between its MCU and its
**BK2425** (nRF24L01+ clone) radio, using a **Raspberry Pi Pico logic analyzer**. This
targets the *actual* arm gate (RC-only) rather than overpowering firmware outputs. The tap
works and the frame is decoded (`25<ch> A0<payload>`, optical flag = payload byte 6 `0x40`,
4-channel hop, boot config). That initially came from a **free-running decode with no CS#
framing**, so the address looked misaligned — an early impersonator transmitted byte-perfect
frames the drone ignored, and a sniffer heard nothing from the real RC. **Resolved 2026-07-03/04**:
the mismatch was a byte-order issue, not a bad capture — listening on the address in reversed
order confirmed the live link address and hop set, and the bind address and bind channels
*(all withheld — see docs/WRITEUP.md §Ethics)* were confirmed separately off the cold-boot SPI burst.
`esp32_nrf24_optical_tx` now does full RC-free replay — neutral heartbeat, held optical, and the
ARM/motor-idle burst — confirmed over the air. Full detail in
[`docs/RC_LINK_SNIFF.md`](docs/RC_LINK_SNIFF.md) and [`docs/WRITEUP.md`](docs/WRITEUP.md).

### Parallel track: WiFi arm under a real GPS lock (2026-07-02)
A separate 07-02 finding widened the arm picture: with the drone holding a **real GPS fix**
(≈13 sats, valid position) and the **RC never switched to optical**, the motors arm from an
**over-WiFi throttle gesture** (drag throttle up past ~240, snap back to `0x80`). So arming is
gated by *"RC **or** a satisfied positioning gate"* — a genuine GPS lock opens the WiFi arm path
with no RC and no SWD. This makes **GPS-fix spoofing** a candidate arm lever alongside the SWD
hold and RC-link impersonation.

---

## How it all connects (the why)
The whole project is shaped by the architecture: **two chips, two trust levels.** The Linux side (RV1109) is open to us via root; the flight side (MM32) is read-protected and RC-gated. We can't reprogram the locked chip — so we reach across the `ttyS1` bridge (or the RC link) to influence it. The endgame is to do that from a tiny onboard chip instead of a tethered probe. See `DRONE_ARCHITECTURE.md` for the full hardware map and `PLAN_TOMORROW.md` for the step-by-step checklist.
