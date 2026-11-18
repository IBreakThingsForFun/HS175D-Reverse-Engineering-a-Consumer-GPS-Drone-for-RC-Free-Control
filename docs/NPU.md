# HS175D / Rockchip RV1109 — NPU Bring-Up & On-Device Object Detection

The RV1109 camera SoC's onboard NPU (~1.2 TOPS) is **confirmed working** — real inference, on
the actual hardware, from a cold boot, with no vendor driver support shipped in this unit's
firmware. What started as a single classifier demo became real-time multi-object detection with
bounding boxes drawn live over the camera feed inside the project's desktop app — reconnect-proof,
survives a drone reboot with one click, stable enough to fly with.

See [`DRONE_ARCHITECTURE.md`](../DRONE_ARCHITECTURE.md) §3 for where the NPU sits in the two-brains
picture: it's vision-only — an advisor, not the pilot. The flight MCU still makes every motor
decision.

---

## 1. What's stock vs. what you have to bring over yourself

Getting this distinction right up front saves a lot of time — half the "missing file" confusion
on this kind of bring-up is really "I'm trusting a stub that's actually a useless placeholder" or
vice versa.

| Component | Stock on this firmware? | If not, where it comes from |
|---|---|---|
| `librknn_api.so`, `librknn_runtime.so` | ✅ yes — `/usr/lib/` | — |
| NPU kernel module (`galcore`) | ❌ no | acquire — §2 |
| Userspace OpenVX/GAL stack (`libOpenVX.so.1.2` + deps) | ❌ no | acquire — §2 |
| `rknn_server` + its plugins | ❌ no | acquire — §2 |
| Cross-compile toolchain (host-side) | n/a — host tool | Bootlin, §4 |
| `rknn-toolkit` v1.7.5 (host-side, converts models) | n/a — host tool | GitHub release, §3 |
| `.rknn` model files | ❌ no | convert yourself from public `.onnx` — §3 |
| Demo / persistent-loop binaries | ❌ no | build from `rknn_model_zoo` — §4 |

---

## 2. Acquiring the driver stack

The stock firmware ships the RKNN API/runtime shim but is completely missing the driver stack
underneath it — no NPU kernel module, no userspace OpenVX/GAL library, no `rknn_server`. Rockchip
open-sourced the exact missing stack under their own GitHub org:
[`rockchip-linux/rknpu`](https://github.com/rockchip-linux/rknpu). Everything below is a raw file
in that repo — no build step, just download and push.

| Piece | Path in the repo | Notes |
|---|---|---|
| Kernel module | `drivers/npu_ko/galcore_puma.ko` | "puma" = Rockchip's internal codename for the RV1109/RV1126 NPU variant. Loads clean against this unit's kernel (4.19.111) — no version-magic rejection. **Cannot be unloaded** once loaded (see §6) — only a reboot resets it. |
| Userspace OpenVX/GAL — **"mini" variant** (what production actually uses) | `drivers/linux-armhf-puma-mini/usr/lib/` | `libArchModelSw.so`, `libGAL.so`, `libNNArchPerf.so`, `libOpenVX.so`/`.so.1`/`.so.1.2`, `libOpenVXU.so`, `libVSC_Lite.so`, `librknn_runtime.so`. The mini bundle swaps the full shader compiler for a much smaller `libVSC_Lite.so` and drops several GB-scale binaries (`libNNVXCBinary.so` alone is 51MB in the full bundle) — ~5.3MB total vs. ~93MB, and this device's tight RAM budget makes the difference matter. `libOpenVX.so`/`.so.1` are git-LFS symlink placeholders (16 bytes) in a plain clone/raw-download — harmless; only `libOpenVX.so.1.2` (the real 2.4MB file) is needed at runtime. |
| `rknn_server` + plugins | `drivers/linux-armhf-puma/usr/bin/rknn_server` (note: **full** bundle path, not mini) plus `drivers/linux-armhf-puma/usr/lib/npu/rknn/plugins/{libann_plugin.so,librknn_plugin.so}` and `.../usr/lib/libneuralnetworks.so` | Not required for the single-shot classifier demo, but **required** for the persistent detection setup — hosts the actual NPU transfer session; its plugin path is set via `RKNN_SERVER_PLUGINS`. Startup logs a harmless `dlopen: libOpenVX.so.1: file too short` warning (the LFS-placeholder file) — ignore it. |

Cross-compile toolchain (host-side, needed later to build demo binaries): Bootlin
`armv7-eabihf--glibc--stable-2018.11-1` (GCC 7.3.0 / glibc 2.27) —
[toolchains.bootlin.com](https://toolchains.bootlin.com/). **Must match the drone's glibc 2.28**
(Buildroot, Nov 2021). A newer host toolchain's glibc headers (2.34+) bake in symbol requirements
that don't exist on-device — shows up as `GLIBC_2.34`/`2.38` not-found errors at runtime, and can't
be fixed by swapping runtime `.so` files after the fact.

---

## 3. Model conversion, start to finish

The stock firmware has zero `.rknn` model files. Converting a public ONNX export needs the
**old** `rknn-toolkit` (v1, not `rknn-toolkit2` — current toolkit2 releases dropped
`rv1109`/`rv1126` as valid `target_platform` values entirely). This needs Python 3.8
specifically (the wheel is `cp38`-only). Full recipe, WSL/Ubuntu-flavored:

1. Install Python 3.8 (a fresh Ubuntu release won't have it):
   `sudo add-apt-repository ppa:deadsnakes/ppa && sudo apt-get install python3.8 python3.8-venv`
2. `python3.8 -m venv ~/rknn_env_v1 && source ~/rknn_env_v1/bin/activate`
3. Download the toolkit release tarball from
   [rockchip-linux/rknn-toolkit](https://github.com/rockchip-linux/rknn-toolkit/releases) —
   use **v1.7.5** (matches the `rknn_server` version above).
4. Pre-install CPU-only PyTorch **first**, or pip will happily pull the full CUDA stack (~2.5GB —
   enough to fill a size-limited `/tmp` tmpfs and abort the whole install):
   `pip install torch==1.10.1+cpu --extra-index-url https://download.pytorch.org/whl/torch_stable.html`
5. Install the pinned dependency set — pip's own conflict-resolution error output reveals the full
   pin list if you guess wrong, but the confirmed set is: `numpy==1.19.5 protobuf==3.13.0
   onnx==1.10.0` plus `tensorflow`, `mxnet`, `psutil`. Two gotchas:
   - Old `torch`/`onnxruntime` `.so` files need `patchelf --clear-execstack <file>` to load on a
     modern kernel (executable-stack ELF flag rejected otherwise).
   - `ruamel.yaml`'s exact old pin doesn't build on a modern compiler/glibc from source — let a
     newer prebuilt wheel satisfy the import; it works fine at runtime despite pip's complaint.
6. Install the toolkit wheel: `pip install --no-deps rknn_toolkit-1.7.5-cp38-cp38-linux_x86_64.whl`
   (`--no-deps` since the deps above are already satisfied manually — letting pip resolve them
   itself tends to silently drop packages when one pin in the batch fails to build).
7. Grab a COCO calibration subset (a 20-image list, `coco_subset_20.txt` + the images it
   references, from the model_zoo repo's dataset area) and the conversion script (`convert.py`,
   bundled with the toolkit release / model_zoo examples).
8. Download the `.onnx` export for whichever model (all hosted at the same base URL, swap the
   filename): `https://ftrg.zbox.filez.com/v2/delivery/data/95f00b0fc900458ba134f8b180b3f7a1/examples/<family>/<model>.onnx`
   — e.g. `examples/yolov5/yolov5n.onnx`, `examples/yolov5/yolov5s.onnx`,
   `examples/yolov8/yolov8n.onnx`, `examples/yolo11/yolo11n.onnx` (see §5 for what actually runs).
9. Convert: `python3 convert.py <model>.onnx rv1109 u8 <model>.rknn`

---

## 4. Building the demo & persistent-loop binaries

Both the single-shot classifier demo and every YOLO detection binary come from the same repo,
[`airockchip/rknn_model_zoo`](https://github.com/airockchip/rknn_model_zoo) (a different, larger
repo than the driver-stack one in §2 — don't confuse them). Sparse-checkout just what's needed:

```bash
git clone --filter=blob:none --sparse https://github.com/airockchip/rknn_model_zoo
cd rknn_model_zoo
git sparse-checkout add examples/yolov5/cpp examples/yolov5/model utils 3rdparty
```

Then, with the Bootlin toolchain from §2:

```bash
export GCC_COMPILER=~/toolchains/armv7-eabihf--glibc--stable-2018.11-1/bin/arm-linux
bash build-linux.sh -t rv1109 -a armhf -d yolov5 -r 1
```

`GCC_COMPILER` **must** be set before running `build-linux.sh` — the script otherwise falls back
to whatever `arm-linux-gnueabihf-*` it finds on `PATH` (usually a much newer system cross-
toolchain), silently producing a binary linked against a glibc newer than the drone's, which fails
at runtime with the same `GLIBC_2.34 not found` class of error as the host-toolchain mismatch in
§2. `-r 1` disables RGA (Rockchip's hardware image scaler) so it falls back to CPU-based resize —
avoids needing an RGA kernel driver whose presence on this unit was never verified; the RGA
headers/lib are still linked unconditionally regardless of this flag (it only gates code paths,
not the build wiring), so `librga.so` still needs to be in the sparse checkout and pushed
alongside the binary.

**To build the persistent-loop variant** (loads the model once, then polls for new frames instead
of exiting after one — see §6 for why this matters): copy `main_loop.cc` (adapted per model
family — the API shape `init_<model>_model`/`inference_<model>_model`/`release_<model>_model` is
identical across `yolov5`/`yolov8`/`yolo11`, so it's a near-verbatim copy per family) into the
model's `cpp/` directory, then append a matching `add_executable` block to that directory's
`CMakeLists.txt` (mirror the existing `rknn_<model>_demo` target — same source files except
`main.cc` → `main_loop.cc`) before rebuilding with the same `build-linux.sh` command.

---

## 5. Model comparison — how big a model actually fits

With the reliability fixes in §6 in place, systematically tried bigger/newer models to see what
this hardware can sustain **as a persistent process** (the realistic case — a one-shot demo is
misleading, see the caveat below). All conversions used `rknn-toolkit` v1.7.5, targeting
`rv1109`; all binaries built via `build-linux.sh -t rv1109 -a armhf -d <name> -r 1` against each
model family's `rknpu1` C++ implementation.

| Model | `.rknn` size | Result |
|---|---|---|
| yolov5n | 2.0MB | ✅ works |
| **yolov5s** | 7.4MB | ✅ works — **current default**, ~700-800ms/inference, best accuracy actually running on this unit |
| yolov5m | 21MB | ❌ OOM-killed during model load itself (score 202, ~118MB total-vm) — a real hard ceiling |
| yolov8n | 3.3MB | ❌ works but impractical — see caveat |
| yolov8s | 11.3MB | ❌ OOM-killed (score 189, ~100MB total-vm) |
| yolo11n | 2.8MB | ❌ OOM-killed (score 188, ~90MB total-vm) despite being the smallest file tested |

**File size does not predict whether a model fits** — runtime (persistent, resident) memory
footprint is what matters. `yolov8n`'s one-shot demo looked spectacular at first (4/4 correct
detections, 77-84% confidence, better than `yolov5s`) — but a controlled re-run timed the same
one-shot invocation externally at **48.2 seconds** end-to-end. Under sustained/persistent-loop
conditions it ran with only 13-14MB free (vs. `yolov5s`'s comfortable 54-68MB) and took ~2.7s per
inference — `yolov8`'s decoupled/multi-output head (9 output tensors vs. `yolov5`'s 3) doesn't
map efficiently onto this old NPU generation's (`rknpu1`/RV1109) driver, regardless of nominal
model size. **Lesson:** a quick single-shot demo with no timing output can hide a real problem —
always test the actual usage pattern (persistent/resident), and always measure timing.

**Conclusion:** `yolov5s` remains the best real option on this hardware — newer architectures
(v8, v10, 11) exist in the model zoo for this chip family but don't run efficiently on this
particular old NPU generation, at least not at a size that fits its memory budget.

---

## 6. Reliability fixes for a persistent, always-on detector

Running the detector once per invocation (load model, run one image, exit) is fine for a demo but
far too slow for a live viewer — model init alone takes 5-28s depending on model size. The real
fix is a **persistent process** (`main_loop.cc`, §4) that loads the model once and polls a fixed
input path (`stat()` on mtime) for new frames — this is what actually made the "the camera process
crashes under NPU load" problem show up in the first place, since a resident process holds its
memory continuously instead of releasing it after one shot.

| Problem | Real cause | Fix |
|---|---|---|
| The camera/command process (`img_trans_app`) dies under memory pressure, taking WiFi video/commands down with it (survives adb/ping, but the JSON command channel and RTSP both go dark, unrecoverable without reboot) | It's a prime OOM-kill target itself — its OOM score (81) was *higher* than the detector process's (72), so the kernel picked it essentially at random under pressure | `echo -1000 > /proc/<pid>/oom_score_adj` right after each boot, before anything NPU-related. Confirmed 4/4 clean runs afterward — the kernel sacrifices the harmless, retry-able detector process instead, every time. |
| Extra free-memory headroom needed before a detection run | Page cache/buffers accumulate and aren't reclaimed automatically under light pressure | `echo 1 > /proc/sys/vm/drop_caches` before each run — reclaimed ~14MB in testing. Write-only (reads back "Permission denied" even though the write itself works). |
| A backgrounded process (`nohup ... &` as the last thing in an `adb shell` command) occasionally never actually starts — the log file stays completely empty | If the shell session has nothing left to do after backgrounding, it can tear itself (and the just-spawned child) down before the child fully detaches | Append `&& sleep 1` (or `& sleep 1`) *inside the same shell command*, after the `&`. Verify via `ps` afterward and retry once if it didn't survive. |

This SoC only has **236MB total RAM** (confirmed via `free`), and the stock system (camera
pipeline, WiFi, etc.) already uses most of it at idle — free headroom at any given moment is
roughly 15-70MB depending on what else is running, and is the binding constraint on this hardware,
more so than compute.

**The kernel module can't be unloaded.** `rmmod galcore` fails (`Device or resource busy`, shows
`[permanent]` in `lsmod`) — this class of GPU/NPU driver doesn't support safe teardown mid-session.
Once loaded, only a reboot resets it; `/tmp` is tmpfs, so a reboot also wipes everything pushed
below — the whole recipe re-runs from a fresh boot each time.

**`CMD_RKNN_LIBS_READY` (WiFi command 135) is not a control lever.** It's a pure echo/logger in
`img_trans_app` — reads a value, logs it, echoes it back. That process has no `rknn_*`/`dlopen`
imports at all and can't drive the NPU; everything here runs as a standalone process outside the
WiFi command channel entirely.

---

## 7. Full on-device bring-up (current production sequence)

This is the exact sequence the app's own "⚙ Start NPU" button (`NpuBringup.cs`) automates,
spelled out as raw `adb` commands for reproducing without the app. Everything lands in `/tmp`
(tmpfs) — nothing touches flash/rootfs (confirmed — `/usr/lib` genuinely can't be written to), so
the worst case is always "needs a reboot," never damage. Run from a fresh boot.

1. `adb connect 192.168.100.1:5555` (root shell; see [`DEVICE_ACCESS.md`](DEVICE_ACCESS.md))
2. `mkdir -p /tmp/npu/mini /tmp/npu/plugins /tmp/npu/yolov5/model`
3. Push `galcore_puma.ko`, the mini OpenVX lib set, `rknn_server` + its plugins, and the
   persistent-loop binary + `.rknn` model + labels file into those directories. (All of this is
   bundled in this release's [`npu_runtime/`](../npu_runtime/) folder.)
4. `chmod +x` the two pushed binaries (`adb push` doesn't preserve the executable bit).
5. **Protect the camera/command process from the OOM killer** before anything else:
   `echo -1000 > /proc/$(pidof img_trans_app)/oom_score_adj` (see §6).
6. `echo 1 > /proc/sys/vm/drop_caches` — reclaims a few extra MB of headroom.
7. `insmod /tmp/npu/galcore_puma.ko`
8. Start `rknn_server`:
   ```
   cd /tmp/npu/plugins && LD_LIBRARY_PATH=/tmp/npu/mini:/tmp/npu/plugins \
   RKNN_SERVER_PLUGINS=/tmp/npu/plugins nohup ./rknn_server > /tmp/npu/rknn_server.log 2>&1 & sleep 1
   ```
9. Verify via `ps` that it actually stayed running (§6) — retry once if not.
10. Start the persistent detector:
    ```
    cd /tmp/npu/yolov5 && LD_LIBRARY_PATH=/tmp/npu/mini \
    VIV_VX_ENABLE_SHADER=0 VIV_VX_ENABLE_TP_TENSOR_SOFTMAX=1 VIV_VX_ENABLE_CACHE_GRAPH_BINARY=0 \
    nohup ./rknn_yolov5_loop model/yolov5s.rknn /tmp/npu/yolov5/live_in.jpg /tmp/npu/yolov5/live_out.jpg \
      > /tmp/npu/loop_proc.log 2>&1 & sleep 1
    ```
11. Poll `/tmp/npu/loop_proc.log` for a line reading `READY` — model init takes anywhere from
    5s (`yolov5n`) to ~28s (`yolov5s` on a cold cache).

Once `READY` appears, push any JPEG to `live_in.jpg` (atomically — write to `live_in.jpg.tmp` then
`mv` it into place, so the loop never reads a half-written file) and read back `live_out.jpg`
(annotated image) or `live_out.txt` (plain `label x1 y1 x2 y2 confidence` lines, one per
detection) a few hundred ms later.

The three `VIV_VX_*` environment variables were found by extracting environment-variable strings
directly out of the pushed `.so` files (no source needed — same technique works for finding any
other undocumented vendor knob in a closed binary). They force the runtime off the shader-based
execution path — which segfaults on this driver/model combination inside
`vxCreateProgramWithBinary` — and onto the Tensor-Processor hardware path instead.

---

## 8. Original single-shot classifier recipe (historical)

The very first working recipe (in [`npu_poc/`](../npu_poc/)), kept for reference — simpler than
§7 (no `rknn_server`, no persistent loop, no OOM protection needed since it's one-and-done), but
superseded by §7 for anything beyond a quick "does the driver stack work at all" check. Uses the
**full** (not mini) OpenVX bundle.

1. Push `galcore_puma.ko` to `/tmp/` and `insmod` it.
2. Push the full OpenVX/GAL lib set into a private dir, e.g. `/tmp/npu_test/openvx_libs/`, and
   recreate the symlink chain (`libOpenVX.so` → `.so.1` → `.so.1.2`, same for `libOpenCL.so.1`)
   since they ship as git symlinks, not plain files.
3. Push the built `rknn_mobilenet_demo` binary + the `.rknn` model + a test image (any JPG/PNG —
   the demo resizes to 224×224 internally).
4. Run it:
   ```
   LD_LIBRARY_PATH=/tmp/npu_test/openvx_libs:/tmp/npu_test:/usr/lib \
   VIV_VX_ENABLE_SHADER=0 \
   VIV_VX_ENABLE_TP_TENSOR_SOFTMAX=1 \
   VIV_VX_ENABLE_CACHE_GRAPH_BINARY=0 \
   ./rknn_mobilenet_demo mobilenet_v1_rv1109_rv1126.rknn your_image.jpg
   ```

**Confirmed result** (Granny Smith apple, plain background, never seen by the model before):
```
rknn_run
   0: Elapse Time = 10.25ms, FPS = 97.57
 --- Top5 ---
949: 0.983398   (Granny Smith)
523: 0.006718
953: 0.005432
952: 0.002092
951: 0.000724
```
Also verified against a live frame grabbed straight off the drone's own RTSP camera feed, not just
the demo's stock test photo — different scenes produced different, plausible classifications,
confirming genuine inference rather than a canned result.

---

## 9. Object detection with bounding boxes

Extended past single-label classification to real multi-object detection with bounding boxes,
using [`airockchip/rknn_model_zoo`](https://github.com/airockchip/rknn_model_zoo)'s YOLOv5
example. Same underlying driver stack and `VIV_VX_*` env vars as the classifier — additive, not a
different setup.

**Confirmed result** (stock `bus.jpg` test image, unmodified):
```
model input num: 1, output num: 3
input tensors: dims=[1, 3, 640, 640] ...
rknn_run
person @ (209 251 285 502) 0.538
person @ (109 249 198 512) 0.377
```
Visually verified against the written `out.png` — both bounding boxes correctly locate the two
people in frame, no false positives.

---

## 10. Live integration: bounding boxes in the app's own video feed

The NPU detector is wired directly into the desktop app's `VideoPanel` (mpv-based RTSP viewer),
drawing real-time bounding boxes on top of the live feed — not a separate viewer, not a static
snapshot. Confirmed smooth even at a 1-second detection interval with no perceptible video delay.

| Piece | File | Role |
|---|---|---|
| On-device sidecar output | `main_loop.cc` | Alongside the annotated image, also writes a tiny plaintext `live_out.txt` (`label x1 y1 x2 y2 confidence` per line, atomically) — a few hundred bytes instead of a ~100-150KB image, so the app draws its own boxes instead of pulling a whole re-rendered frame. |
| [`NpuDetector.cs`](../NpuDetector.cs) | app | One detect cycle: grab an mpv screenshot of the currently-playing video (no separate RTSP connection — this drone's RTSP server only supports one client, confirmed the hard way, see §11), push it to `live_in.jpg`, wait ~450ms, pull & parse `live_out.txt`. |
| `DetectionOverlay.cs` | app (`Forms/`) | A borderless, click-through, per-pixel-transparent `Form` (same `TransparencyKey`/`WS_EX_LAYERED` trick as `ArmOverlay.cs`) positioned over the video host, drawing boxes mapped from the NPU's fixed 1280x720 source space into whatever scale mode the video panel is currently using. |
| [`NpuBringup.cs`](../NpuBringup.cs) | app | One-click "⚙ Start NPU" button — replays the full §7 bring-up from this release's `npu_runtime/` folder, applies the OOM-protection/drop_caches fixes, starts everything, polls for `READY`. Needed once per drone power-cycle since it all lives in tmpfs. |
| UI controls | `VideoPanel.Designer.cs`/`.cs` | "AI Detect" checkbox + interval NumericUpDown, on their own timer independent of video playback — detection cadence never touches video framerate. |

---

## 11. Video reconnect (RTSP session cap)

Separate from the NPU work, but directly blocking "usable while flying": the drone's RTSP server
(reports itself as `OnDemandRTSPServer`) hard-caps sessions at roughly **60-70 seconds regardless
of activity** — confirmed independently via a plain `ffmpeg` capture and the app's own mpv player
hitting the identical pattern. adb/AI-detect traffic keeps working fine through it, so it's just
the RTSP session dying server-side, not the whole drone connection.

- **Two simultaneous RTSP connections don't work on this server** — tested directly: a second
  connection starting while the first is active doesn't hand off cleanly.
- **Fix: proactive reconnect**, not reactive. `VideoPanel`'s existing latency watchdog timer also
  tracks mpv's `time-pos`; on a fixed interval (default/confirmed sweet spot **60s** in actual
  flight testing — 65-70s still showed a noticeable delay) it proactively reloads the stream
  *before* the server's own cutoff, so the session never actually dies. A reactive stall-check (4s
  no-progress) remains as a backup for anything that drops sooner.
- **Smoothing the transition:** `MpvClient`'s `keep-open` option is `yes` — holds the last decoded
  frame on screen through the reconnect gap instead of flashing to black, so each reconnect reads
  as a brief freeze rather than a flicker.
