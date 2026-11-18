#!/usr/bin/env python3
"""
extract_cmds.py — pull the HS175D / Vison "A17" JSON command map out of the decompiled app.

The HS GPS V5 app (package com.hy.hs_fly2) builds {"CMD":n,"PARAM":...} messages. This tool
finds, in jadx-decompiled Java sources:

  1. integer constants that look like command IDs  (e.g.  CMD_TAKE_PHOTO = 61)
  2. every place "CMD" is put into a JSON object, with the value and the ENCLOSING METHOD
     (e.g.  takePhoto() -> put("CMD", 61))
  3. cross-references both against the commands we already confirmed by live testing.

Output: a sorted Markdown table (value -> names / methods / known meaning / guess).

USAGE
  # against a jadx output directory:
  python extract_cmds.py path\\to\\jadx_out  [--out cmds.md]

  # against an APK directly (needs `jadx` on PATH; decompiles to a temp dir first):
  python extract_cmds.py com.hy.hs_fly2.apk --apk  [--out cmds.md]

Get the APK from the phone, APKMirror, or apkpure, then:  jadx-gui  or  jadx -d out app.apk
"""
import os, re, sys, subprocess, tempfile, collections, html

# ---- what we already know (from live testing + sweep) -------------------------------------
KNOWN = {
    0:  "version / hello (FirmWare, platform)",
    5:  "setter, RESULT:0 (unknown)",
    11: "setter, RESULT:0 (unknown)",
    29: "time sync (YEAR..SECOND)",
    63: "video stream status (online, format)",
    68: "WiFi SSID",
    84: "app_commit (firmware git hash)",
    85: "OTA update_status",
    87: "MAC address",
    88: "deviceID",
    91: "DroneID capability dump",
    100: "recording state (PARAM 1/0)",
    126: "camera resolution caps",
    135: "setter, RESULT:0 (unknown)",
    138: "video format negotiate (H264)",
    200: "setter, RESULT:0 (unknown)",
}
POISON_NOTE = "⚠ near a poison/reset CMD (~13-29, 127) — handle carefully"
POISON_VALUES = set(range(13, 30)) | {127, 128}

# keyword -> what it probably is; used to flag interesting constant/method names
KEYWORDS = [
    (r"PHOTO|PICTURE|TAKE_?PIC|SNAP|CAPTURE", "PHOTO"),
    (r"RECORD|VIDEO_?START|VIDEO_?STOP|MOVIE", "RECORD"),
    (r"TAKE_?OFF|ONE_?KEY_?UP|AUTO_?UP", "TAKEOFF"),
    (r"\bLAND|ONE_?KEY_?DOWN|AUTO_?LAND", "LAND"),
    (r"GO_?HOME|RETURN|RTH|HOME", "RETURN-HOME"),
    (r"HEADLESS|HEAD_?LESS", "HEADLESS"),
    (r"CALIB|GYRO|COMPASS|MAGNET", "CALIBRATE"),
    (r"GIMBAL|PTZ|TILT|EPTZ", "GIMBAL"),
    (r"FOLLOW|GESTURE|PALM|HAND", "FOLLOW/GESTURE"),
    (r"\bSPEED\b|\bRATE\b|\bGEAR\b", "SPEED"),
    (r"REBOOT|RESET|FORMAT|FACTORY|UPGRAD|\bOTA", "RESET/OTA (possible poison)"),
    (r"EMERGENC|MOTOR|ARM|STOP|LOCK|UNLOCK", "ARM/STOP"),
    (r"TRIM|CALI_?TRIM", "TRIM"),
    (r"ZOOM|RESO|FPS|WHITE|EXPOS|CONTRAST|SATURAT|ANTISHAKE|ANTI_?SHAKE", "CAMERA-SETTING"),
    (r"TIME|DATE|CLOCK", "TIME"),
    (r"VERSION|FIRMWARE|DEVICE|MAC|SSID|WIFI", "INFO"),
]

CONST_RE = re.compile(
    r'\b(?:public|private|protected|static|final|\s)+\b(?:int|byte|short|long)\s+'
    r'([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(0[xX][0-9a-fA-F]+|\d+)\s*;')
# "CMD" put into JSON: put("CMD", VALUE)  /  "CMD":VALUE  /  \"CMD\":" + VALUE
CMDPUT_RE = re.compile(r'["\\]+CMD["\\]+\s*[,:]\s*\+?\s*([A-Za-z0-9_]+)')
METHOD_RE = re.compile(
    r'(?:public|private|protected|static|final|synchronized|native|\s){1,}'
    r'[\w<>\[\],.\s]+?\s+([A-Za-z_]\w*)\s*\([^;{=]*\)\s*\{')


def keyword_tag(name):
    up = name.upper()
    tags = [tag for pat, tag in KEYWORDS if re.search(pat, up)]
    return ",".join(dict.fromkeys(tags))


def iter_java(root):
    for dp, _, files in os.walk(root):
        for f in files:
            if f.endswith(".java"):
                yield os.path.join(dp, f)


def method_index(text):
    """Return sorted list of (offset, method_name) for method declarations."""
    return sorted((m.start(), m.group(1)) for m in METHOD_RE.finditer(text))


def enclosing_method(idx, offset):
    lo, hi, best = 0, len(idx), None
    for off, name in idx:
        if off <= offset:
            best = name
        else:
            break
    return best or "<top-level>"


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    opts = [a for a in sys.argv[1:] if a.startswith("--")]
    if not args:
        print(__doc__); sys.exit(1)
    target = args[0]
    out_path = None
    if "--out" in opts:
        i = sys.argv.index("--out"); out_path = sys.argv[i + 1]

    root = target
    if "--apk" in opts or target.lower().endswith(".apk"):
        tmp = tempfile.mkdtemp(prefix="jadx_")
        print(f"[*] running jadx on {target} -> {tmp} (this can take a minute)...")
        try:
            subprocess.run(["jadx", "-d", tmp, target], check=True)
        except FileNotFoundError:
            sys.exit("ERROR: `jadx` not found on PATH. Install it (https://github.com/skylot/jadx) "
                     "or pass an already-decompiled directory instead.")
        root = tmp

    if not os.path.isdir(root):
        sys.exit(f"ERROR: {root} is not a directory.")

    consts = collections.defaultdict(set)      # value(int) -> {names}
    const_name_val = {}                          # NAME -> value
    methods_for_value = collections.defaultdict(set)   # value -> {(method, file)}
    files = 0

    # pass 1: constants
    for path in iter_java(root):
        files += 1
        try:
            text = open(path, encoding="utf-8", errors="ignore").read()
        except Exception:
            continue
        for m in CONST_RE.finditer(text):
            name, raw = m.group(1), m.group(2)
            try:
                val = int(raw, 0)
            except ValueError:
                continue
            if 0 <= val <= 4096:
                consts[val].add(name)
                const_name_val[name] = val

    # pass 2: "CMD" build sites + enclosing method (resolve identifier values via constants)
    for path in iter_java(root):
        try:
            text = open(path, encoding="utf-8", errors="ignore").read()
        except Exception:
            continue
        if '"CMD"' not in text and 'CMD\\"' not in text:
            continue
        idx = method_index(text)
        rel = os.path.relpath(path, root)
        for m in CMDPUT_RE.finditer(text):
            tok = m.group(1)
            val = None
            if tok.isdigit():
                val = int(tok)
            elif tok in const_name_val:
                val = const_name_val[tok]
            if val is None or val > 4096:
                continue
            methods_for_value[val].add((enclosing_method(idx, m.start()), rel))

    # ---- build report ----
    all_values = sorted(set(consts) | set(methods_for_value) | set(KNOWN))
    lines = []
    lines.append(f"# HS175D command map (extracted from {files} .java files)\n")
    lines.append("| CMD | constant name(s) | build-site method(s) | known meaning | guess |")
    lines.append("|----:|------------------|----------------------|---------------|-------|")
    for v in all_values:
        names = ", ".join(sorted(consts.get(v, []))) or ""
        meths = ", ".join(sorted(f"{mm}()  [{ff}]" for mm, ff in methods_for_value.get(v, []))) or ""
        known = KNOWN.get(v, "")
        tags = keyword_tag(names + " " + " ".join(mm for mm, _ in methods_for_value.get(v, [])))
        if not known and v in POISON_VALUES and (names or meths):
            tags = (tags + "; " if tags else "") + POISON_NOTE
        lines.append(f"| {v} | {names} | {meths} | {known} | {tags} |")

    report = "\n".join(lines)
    print(report)
    print(f"\n[*] {len(all_values)} distinct command values; "
          f"{sum(len(s) for s in methods_for_value.values())} CMD build-sites found.")
    if out_path:
        open(out_path, "w", encoding="utf-8").write(report + "\n")
        print(f"[*] wrote {out_path}")
    else:
        print("[*] tip: pass --out cmds.md to save the table.")


if __name__ == "__main__":
    main()
