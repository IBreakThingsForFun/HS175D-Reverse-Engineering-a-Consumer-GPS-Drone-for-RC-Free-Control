# HS175D — Plan of Action (start here in the morning)

> ⚠️ **Historical plan (2026-06-29).** Kept for its phase structure and the still-valid
> method notes (liveness guard, throttle centering, ttyS1 capture). **It predates the
> 2026-07-01/02 work** (RC-link SPI decode, the nRF24 impersonator, and the WiFi arm-via-
> GPS-lock finding). For current state and next moves see **`PROJECT_STATUS.md`** and
> **`docs/RC_LINK_SNIFF.md` §8–11**.

_Written 2026-06-29 evening. Goal of this session: prove RC-free motor/flight authority, working simplest → hardest, ruling things out as we go._

---

## The one-paragraph state of the world

We have **permanent hardware access** to the drone: rooted Android/Linux side (FTP `HY819/1663819`, adb `:5555`), persistent **FC UART** (TX-only boot oracle), and persistent **SWD** (DAT/CLK/GND) routed to a 2-screw nose header, plus one spare wire. The blocker is **not** reaching the drone — it's that the flight MCU (MM32F103) won't grant motor authority without the RC-side condition. Best confirmed result so far (2026-06-28): with GPS physically unplugged and RC off, **holding the COMPLETE mode-state set over a continuous SWD write loop + WiFi throttle made the motors spin.** Holding only a subset → hybrid/fault, no clean takeoff. So the real lever is the **flight-state "engage" condition**, and the throttle is only meaningful *after* that condition is set.

### Key model correction we locked in last night
All four stick axes **self-center at 128** (altitude/attitude-hold model). So:
- Throttle `0x80` (128) = **idle / hold**, `>128` = climb, `<128` = descend. It is **not** raw motor PWM — it's a climb-*rate request* to an autonomous controller that's asleep until the flight state is engaged.
- This explains every "throttle did nothing" result: on the ground, unarmed, you're feeding a rate request to a disabled controller.
- **Implication baked into every test below:** engage the flight state FIRST, *then* ramp throttle. Throttle-before-state is always a false negative.

---

## What we changed last night (already done)
In `ReWorkbench.cs` — injection tool now reflects the self-centering model:
- Stick-builder throttle default `0 → 128` (idle), all 4 axes now rest at 128.
- Ramp `start` default `0 → 128` (UI + DebugBus `inject.ramp`).
- Labels updated (throttle 128 = idle/center, >128 climb, <128 descend).

**First thing tomorrow:** build the C# app and confirm it compiles + launches with these defaults before relying on it.

---

## What we're doing DIFFERENTLY this time
1. **Liveness guard on every SWD test.** Read 2–3 "jitter" bytes (that change while the FC runs) *alongside* the target region. Write-holds only count if liveness is STILL CHANGING. Frozen liveness = idle-off = inconclusive, discard. No more confusing idle-off with a real result.
2. **Throttle baselined at 128, ramp upward** — never from 0.
3. **Order = engage state, THEN throttle.**
4. **Stop chasing mode/arm SRAM holds as the endgame.** Use them only as *references*. The trusted thing lives upstream of those bytes.
5. **Read the trusted command stream off the wire (ttyS1), not inferred from SRAM.** We already own that bridge.

---

## STEP-BY-STEP (simplest → hardest, with go/no-go gates)

### Phase 0 — Did reassembly break anything? (5–10 min, do first)
- [ ] Power on, **RC off**, let it boot.
- [ ] **UART boot log** capture → confirm solder joints survived; note firmware version, sensor init lines, any reset cause / sensor-init failure.
- [ ] **SWD sanity:** OpenOCD connect → IDCODE (Cortex-M3 enumerates) → halt → `mdw` a known-good SRAM addr → sane bytes back. Confirms DAT/CLK/GND joints are usable, not just enumerating.
- [ ] **Build + launch the C# app** (verify last night's throttle=128 edits compile).
- **GATE:** all four green → proceed. Any red → stop and triage the joint/that subsystem before going further.

### Phase 1 — ttyS1 capture (SOFTWARE ONLY, highest ROI, low risk) ⭐
This is the move that can actually crack it open. We read the **decoded command stream the FC actually consumes**, at the moment authority is granted.
- [ ] Props OFF. From rooted RV1109 (adb/FTP), log `ttyS1` raw.
- [ ] Capture 4 events cleanly, one at a time, timestamped: **RC bind → unlock gesture → mode toggle → takeoff command** (RC is needed to arm, so RC on for this, on the ground, props off).
- [ ] Diff **idle frame vs takeoff frame**. The bytes that change *beyond* the four self-centering stick setpoints = the **engage flag / flight-state condition**. That flag is the thing the FC trusts.
- **Why this matters:** `CMD_VEHICLE_MODE_SET` is NULL SoC-side (proven in img_trans RE) → mode travels only via ttyS1 framing. This is the ground truth both SWD mode-holds and the app have been approximating indirectly.
- **Deliverable:** the exact engage-flag byte(s) and the takeoff frame layout.

### Phase 2 — SWD stick map: single-axis captures (precision pass)
The current map is ambiguous (diagonal tests confused channel identity). Fix it with clean single-axis captures + liveness guard.
- [ ] Liveness guard active (read jitter bytes + stick region together).
- [ ] One axis at a time, full deflection, watch which byte moves:
  - **Throttle up only** → the byte that goes `0x80 → 0xFF` and **holds ≥0x80**. That byte (and only that) is throttle. (Anything diving to `0x00` in this pass is a different axis — ignore.)
  - Pitch fwd / pitch back
  - Yaw left / yaw right
  - Roll left / roll right
- **Note:** `0x200001ca` / `0x200001ce` go `0x80→0x00` → those are **bidirectional axes (roll/pitch/yaw), NOT throttle** (throttle never goes below 0x80). Throttle is a *different* address — the one that climbs above 0x80.
- **Candidate region to focus:** RC/stick area ~`0x200001c0–0x200001d0`.
- **Deliverable:** exact SRAM address for each of the 4 axes, confirmed by isolated motion.

### Phase 3 — Forceability test, correctly ordered (with liveness guard)
Prove three things at once: FC is running, stick bytes can be written, writes persist long enough to be consumed.
- [ ] Engage the **complete mode/flight-state set FIRST** (continuous SWD `mwb` hold loop — firmware reverts one-shots, so HOLD). Use Phase-1 engage flag + the optical mode set as the reference.
- [ ] Confirm controller is live via liveness bytes.
- [ ] **THEN** ramp throttle `128 → up` (WiFi UDP 19798 via the C# ramp, or SWD write to the Phase-2 throttle address).
- [ ] Read result against liveness:
  - holds + liveness changing → **forceable** ✅
  - holds + liveness frozen → idle-off → **inconclusive, discard**
- **Deliverable:** clear yes/no on "can we drive throttle once the state is engaged," not muddied by idle-off.

### Phase 4 — Clean optical reference vs baseline (the experiment GPT wrongly called dead)
Enumerate the COMPLETE mode set instead of guessing subsets.
- [ ] GPS unplugged, **RC → optical mode**: capture full SRAM snapshot (clean optical ref).
- [ ] Baseline (no mode / GPS-style): capture full SRAM snapshot.
- [ ] **Diff = the full mode-state delta.** That delta is the complete set to hold for coherent optical control.
- **Caveat to control for:** if the I2C compass shares the GPS connector, unplugging GPS may also drop mag input and pollute the diff with a missing-sensor fault. Confirm the GPS connector pinout so we know exactly what we remove. (Cross-check against the Phase-1 ttyS1 engage flag — the two should agree.)

### Phase 5 — Converge → coherent RC-free optical control
- [ ] Hold the **complete** Phase-4 delta (continuous SWD loop) + throttle from 128 up.
- [ ] Confirm coherent throttle-takeoff (not hybrid/fault).
- **Endgame:** move the hold off the bench probe onto the **ESP32 wireless-DAP implant** so it holds the set for permanent RC-free control. The spare nose-header wire is staged for RC-side injection if we go that route.

---

### Phase 6 — On-drone autostart / deployment (DOWNSTREAM — only after the engage frame is known)
Goal: drone runs the mechanism itself on boot, no laptop/probe in the loop. Do NOT start this until Phase 1 tells us what to send. This is the path to the elegant "no implant" outcome.

**HARD RULE — reversible + non-destructive only. Every step must satisfy all three:**
1. **Only ADD files to the writable partition (`/netPrivate`, mtd5).** Revert = delete the file. Never overwrite a system file in place without a backup.
2. **Never modify the read-only rootfs squashfs (mtd4).** It's RO by design — that's a *safety feature* (can't brick boot). Leave it alone. (Editing the squashfs image is explicitly out of scope / last-resort-only and is NOT non-destructive — skip it.)
3. **Never touch the recovery channel.** The autostart script must NOT modify networking, adb (`:5555`), FTP, or anything we use to get back in. If our script misbehaves we must still have an independent way to delete it.

**Pre-flight safety checklist (before writing ANY persistence change):**
- [ ] Confirm independent recovery access works *right now* (adb + FTP both reachable) — this is our undo button.
- [ ] Any boot script must be **non-blocking** (background / `setsid nohup`, fail-safe, self-timeout) so a bad script can't hang or prevent boot and lock us out.
- [ ] Keep a copy of any original file we touch before touching it.

**Investigation (the 5-min check):**
- [ ] Grep the boot chain for an existing user-script hook: `/etc/init.d/rcS`, `/etc/inittab`, startup scripts — look for references to `/netPrivate`, `/userdata`, `/data`, or an "if script exists, run it" pattern. If the vendor left a hook → just drop our script there (cleanest, zero system modification).
- [ ] If no hook exists, decide later — do not force one in if it can't be made reversible.

**Power-safety caveat:** `/netPrivate` (mtd5) is vfat-over-mtdblock — survives normal reboots but can **self-revert on a hard battery yank** (same gotcha as the WiFi band switch). 
- [ ] **Test persistence by a FULL power-cycle, not a soft reboot** — that's the only way to know it actually held.
- This flakiness is also a free safety net: worst case, a hard power-off wipes our change.

---

## Parked / deprioritized (don't rabbit-hole)
- **SPI flash dump** — drone is buttoned up; tight fit. Not worth reopening now. Flash isn't going anywhere. (Also: verify whether the XT25F128 16MiB is the RV1109's storage we already own, or a separate FC flash, *before* ever reopening.)
- **Empty I2C port** — passive logic-analyzer only, low priority. Silkscreen SCL/SDA/VCC/GND = almost certainly compass/baro/optical sensor expansion, not a command port. Relevant mainly for the GPS-connector/compass caveat in Phase 4.
- **UART RX** — currently TX-only. If we ever want a console, buzz out the MCU UART RX pin to an unpopulated series resistor/pad. Not needed for the plan above.
- **Deauth / WiFi hijack** — proven dead-end for *this* goal (our gate is RC/FC state, not WiFi session ownership).

---

## Quick reference
- **Tools already in repo:** `tools/watch_rc.cfg`, `tools/force_arm.cfg`, `tools/hold_optical.cfg`, `tools/dump_sram.sh`, `tools/bench_ramp.sh`, plus `stk*.bin` dumps.
- **C# headless API:** DebugBus JSON @ `http://localhost:8731` (status/log/send/adb/inject/`inject.ramp`) — drives all windows from curl during bench tests.
- **Flight UART:** `ttyS1` on RV1109. **Control path:** CMD=TCP 4646, control=UDP 19798, session gate 23333. **Arm = RC-only.**
- **Safety:** props OFF for all Phase 1–3 bench work.

## Suggested order for the morning
**0 (sanity) → 1 (ttyS1 capture) → 2 (stick map) → 3 (forceability) → 4 (optical diff) → 5 (converge).**
Phase 1 is the highest-value, lowest-risk step — if time is short, do 0 then 1.
