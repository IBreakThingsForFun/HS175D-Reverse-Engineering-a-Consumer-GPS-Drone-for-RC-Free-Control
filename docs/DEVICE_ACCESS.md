# HS175D / SYMA X30 Pro — Device Access, Filesystem & Wi-Fi Channel

A hands-on guide to getting a shell on the drone, navigating its filesystem, and changing the Wi-Fi
radio band/channel. This is the operations companion to the read-only architecture notes in the
[README](../README.md). Everything here is on a drone you physically own, using the manufacturer's own
factory-default credentials.

> **Idle power-off, first.** The drone auto-powers-off in **~1–2 min** with no control stream, which
> drops FTP/adb mid-task. Keep the stock app (or our app) streaming for the keepalive while you work;
> **adb on 5555 is independent of the app channel**, so they coexist fine.

---

## 1. Two ways in

The AP is `192.168.100.1` (join its Wi-Fi first).

### ADB — port 5555 → root shell
```bash
adb connect 192.168.100.1:5555
adb shell            # uid=0 (root)
```
This is the "silent" port — not advertised, but open. platform-tools on this machine:
`C:\Users\johnb\Desktop\platform-tools`.

### FTP — port 21 → full filesystem R/W
```
host: 192.168.100.1   port: 21
user: HY819           pass: 1663819
```
Server is *stupid-ftpd*, serverroot `/`, flag `A` = read/write over the **whole** filesystem.
Anonymous is disabled. (Credentials are hardcoded in the stock app's `FtpManager` — identical on
every unit.)

### Windows / Git-Bash `adb push` gotcha
MSYS rewrites the **remote** path into a Windows temp path. Force it off:
```bash
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' adb push "$(cygpath -w /tmp/x)" /tmp/x
```

---

## 2. Flash layout (`/proc/mtd`)

| Partition | Contents | Writable? | Notes |
|-----------|----------|-----------|-------|
| mtd0 | IDBlock | — | |
| **mtd1** | **uboot** (verNum 51401) | no | boot chain; needs UART to read, never entered |
| mtd2 | vnvm | — | |
| mtd3 | boot | no | |
| **mtd4** | **rootfs = read-only squashfs** | **no** | `/etc/*`, `/oem/*`, `/img_trans/*` can't be edited in place |
| **mtd5** | **userdata → `/netPrivate`** (vfat rw) | **yes** | the **only** persistent writable partition (~1.2 MB) |

Persistent edits go to **`/netPrivate`** only. rootfs/uboot/boot are untouched by anything you do, so
a bad `/netPrivate` write is **non-fatal** (init continues) — not a brick.

### ⚠️ `/netPrivate` is NOT power-safe
It's vfat over `mtdblock`. Writes + `sync` reach only the mtdblock **RAM cache**, not NAND. An unclean
power loss (battery yank — routine on a drone) discards the cache and rolls back to the last durable
factory state, silently wiping edits. **Proven 2026-06-24:** wrote config, sync'd, verified → battery
died → came back full factory.

**Force a real NAND commit** (a bare `sync` is insufficient):
```bash
umount /netPrivate && mount -t vfat /dev/mtdblock5 /netPrivate
```
Then read the files back **after** the umount — survival across the umount proves they're truly on NAND.

> Silver lining: this makes the whole approach provably non-destructive. Any hard power loss
> self-reverts to stock.

---

## 3. Filesystem map — where everything lives

| Path | What | Persistence |
|------|------|-------------|
| `/img_trans/bin/` | `img_trans_app` (the bridge), `ProtocolDesc.json`, `DroneID.json`, `run.sh` | rootfs (RO) |
| `/img_trans/bin/run.sh` | launches the app (sourced by `A17Startup.sh`) | rootfs (RO) |
| **`/netPrivate/`** | persistent overrides: `DroneID.json`, `hostapd.conf`, `A17Startup.sh`, `wifi_switch.sh` | **mtd5 (RW)** |
| `/tmp/hostapd.conf` | **live** Wi-Fi AP config (RAM) | RAM (reverts on reboot) |
| `/tmp/ProtocolDesc.json` | working copy of the protocol descriptor, copied at boot | RAM |
| `/tmp/messages` (= `/var/log/messages`) | syslog output | RAM |
| `/tmp/loglevel` (= `/var/log/loglevel`) | `all=5` loglevel file | RAM |
| `/oem/RkLunch.sh` | early init; sources `/netPrivate/A17Startup.sh` if present | rootfs (RO) |
| `/oem/A17Startup.sh` | copies `/netPrivate/wifi_switch.sh` over `/etc/`, then runs the app | rootfs (RO) |
| `/etc/wifi_switch.sh` | builds `/tmp/hostapd.conf`; pulls `/netPrivate/hostapd.conf` if present | rootfs (RO) |
| `/etc/hostapd.conf` | stock 5 GHz fallback config | rootfs (RO) |
| `/etc/inittab` | has the `::respawn:-/bin/sh` that spams the log | rootfs (RO) |
| `/proc/mtd` | flash partition map | — |
| `/dev/ttyS1` | flight-MCU (PT-GPS-SYMA / "YT") UART | — |
| `/dev/ttyS0` | the other UART | — |
| `/dev/hy2001_enc` | `sw2001` crypto chip | — |

### Busybox toolbox (what's present / missing)
- **Present:** `strings`, `iw`, `netstat`, `killall`, `pidof`, `nohup`, `setsid` (no `-f`).
- **Missing:** `tcpdump`, `strace`, `socat`, `start-stop-daemon` (effectively). Capture packets at the
  **wire** (PCAPdroid on the phone), not on-device.

### Log reading (filter the spam)
`/etc/inittab`'s `::respawn:-/bin/sh` floods `/tmp/messages` every ~1 s. Filter it:
```bash
grep -vE "bin/sh|Scheduling for restart|starting pid" /tmp/messages
```
(Don't start a grep pattern with `-`; busybox treats it as a flag.)

To enable the bridge's `app ot yt` MCU-frame dumps, set loglevel over TCP 4646:
`{"CMD":137,"PARAM":{"loglevel":5}}` (dumps gate on `loglevel & 4`). Note: the dumps only show what
the phone *sent* (the app relays) — for axis order you still need a one-stick maneuver pcap.

---

## 4. Change the Wi-Fi band / channel

Ships as **5 GHz ch36** (`hw_mode=a`), SSID `HolyStoneFPV_5G_50043d`. 2.4 GHz-only radios (ESP32-S3)
can't see it as shipped. The MT7663 is dual-band, so switching to 2.4 GHz works. The SSID string is
just a name — band is set by `hw_mode`/`channel`.

### 4a. Runtime switch (RAM-only, reverts on reboot)
Safest for testing — a battery pull undoes it.
```bash
# on the drone (adb shell):
cp /tmp/hostapd.conf /tmp/hostapd_24.conf
sed -i 's/^hw_mode=.*/hw_mode=g/' /tmp/hostapd_24.conf
sed -i 's/^channel=.*/channel=6/' /tmp/hostapd_24.conf
sed -i 's/^ht_capab=/#ht_capab=/' /tmp/hostapd_24.conf
killall hostapd
hostapd /tmp/hostapd_24.conf -B
pidof hostapd        # verify it came back up
```
If `hostapd` didn't restart, relaunch the saved 5 GHz copy (auto-fallback). Confirmed working on ch6.

> **Detached-launch gotcha:** killing/restarting `hostapd` drops the AP, which kills the
> adb-over-wifi shell that issued the command — so the restart never runs. Wrap the switch in a
> script and launch it **detached** so it survives the disconnect:
> ```bash
> nohup setsid sh /tmp/switch.sh </dev/null >/dev/null 2>&1 &
> ```
> What does **not** work on this busybox: `setsid -f` (no `-f` flag), `start-stop-daemon -S -b`
> (matches existing `/bin/sh` → "already running"), plain `setsid … &` (reaped on PTY close).
> Verify launch by checking the script's output file appears within ~1 s, *before* the `killall`.

### 4b. Persistent switch (survives reboot — with the NAND caveat)
The boot scripts are explicitly built to read overrides from `/netPrivate`:

1. Edit **`/netPrivate/hostapd.conf`** — set `channel=6`, `hw_mode=g`. Keep a backup
   (`/netPrivate/hostapd.conf.5g.bak`); `/etc/hostapd.conf` is the ultimate stock fallback.
   `/etc/wifi_switch.sh` copies `/netPrivate/hostapd.conf` over the live conf at boot if it exists.
2. **Boot-exec hook:** `/netPrivate/A17Startup.sh`, if present, is sourced **instead of**
   `/oem/A17Startup.sh`. Keep it minimal — a syntax error here breaks the whole app boot. Recommended
   pattern: background a self-heal script, then `source /oem/A17Startup.sh` verbatim:
   ```sh
   # /netPrivate/A17Startup.sh
   nohup setsid sh /netPrivate/selfheal.sh </dev/null >/dev/null 2>&1 &
   source /oem/A17Startup.sh
   ```
3. **Self-heal** (`/netPrivate/selfheal.sh`): wait up to 120 s for `pidof hostapd`; if none, restore
   stock 5 GHz so remote access returns **without UART**. (UART recovery here = ~1–2 h
   teardown/soldering, so the self-heal matters.)
4. `sh -n`-check **both** scripts before rebooting.
5. **Commit to NAND** (§2) — `umount /netPrivate && mount -t vfat /dev/mtdblock5 /netPrivate` — then
   read the files back to prove they stuck. Without this, a battery yank silently reverts everything.

---

## 5. Recovery / safety summary

- **Can't brick via these methods:** rootfs/uboot/boot are read-only; only `/netPrivate` is writable,
  and it self-reverts on unclean power loss. Worst case from a bad `/netPrivate` edit = factory boot.
- **If the AP won't come back:** pull the battery — a hard power loss rolls `/netPrivate` back to
  factory 5 GHz (and the self-heal in §4b restores it within ~120 s if the partition *did* persist).
- **No UART console is hooked up** — uboot (mtd1) has never been entered. Recovery that requires UART
  is the expensive path; the self-heal exists to avoid it.

See also: [README](../README.md) (architecture overview), [PROTOCOL.md](PROTOCOL.md) (wire protocol),
[SENSORS.md](SENSORS.md) (onboard hardware), [TIMINGS.md](TIMINGS.md) (idle-off / keepalive timing).
