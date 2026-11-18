# HS175D / Syma X30 Pro — JSON command map (TCP 4646)

Extracted from the **HS GPS V5** app (`com.hy.hs_fly2`), enum `t3.c` in classes.dex, decompiled with jadx.
**The CMD number = the enum ordinal** (verified against 8+ live-confirmed commands).
Message shape: `{"CMD":<ordinal>,"PARAM":<value|object>}` (newline-terminated), built by `t4.m` / `t4.y`.
PARAM shapes seen: bare int, string, `{key:val}`, `{camera:n}`, `{region,channel}`, `{width,height,fps}`,
`{camera,reso,fps}`, `{YEAR..SECOND}` (time), JSONArray.

> ⚠ Hardware *_SET commands with a bad PARAM (e.g. `-1`) can reset the command server — that's the
> 'poison' behaviour. **127 = CAMERA_RESO_FPS_SET is NOT inherently poison** — with a valid PARAM
> `{"camera":n,"reso":"2.7K"|"2K_L","fps":25|50}` it returns RESULT 0 (Syma-app confirmed 2026-07-05);
> it only reset the server in our sweep because we sent `PARAM:-1`. The low-range camera/sensor
> _SET commands (≈13–30) are likely the same — bad PARAM, not the command. Send real params, not `-1`.
> Note: CMD 127 emits **two `RESULT:0` replies** per send (accepted → applied) — don't treat the
> second as a stray/duplicate in the reply matcher.

> ⚠ **App-enum names vs firmware names diverge.** This table lists the **APK enum** names (what the
> phone *sends*). The on-device firmware (`drone_protocol/cmd_table.txt`, from `img_trans_app`) uses
> its own constant names for the same IDs, and in the **72–82 range they drift apart** (e.g. 75
> app `PERSPECTIVE` → fw `CMD_CSI_SWITCH`, 76 `PALM` → `CMD_CTROL_HAND`, 78 `MAX` →
> `CMD_BACKLIGHT_COMPENSATION`, 81 `SWITCH_5G` → `CMD_5G_REGION_SEL`, 82 `AGPS_UPDATA` →
> `CMD_UPDATE_GPSMGA`). **The CMD *number* is what matters on the wire; for what the drone actually
> does with an ID, trust the firmware name** (see the firmware dispatch section below). The IDs we've
> live-confirmed (0, 29, 63, 68, 84, 85, 87, 88, 91, 100, 126, 137, 138) match on both sides.

| CMD | constant | notes |
|----:|----------|-------|
| 0 | `SYS_PARAM_GET` | hello / system params. Reply PARAM: `{FirmWare, platform, region, cur_csi}`. **`cur_csi` = current camera source (0=front)** — read-back for CMD 75 CSI_SWITCH |
| 1 | `STATE_SET` |  |
| 2 | `VID_ENC_PREVIEW_ON` |  |
| 3 | `VID_ENC_PREVIEW_OFF` |  |
| 4 | `VID_ENC_START` |  |
| 5 | `VID_ENC_STOP` |  |
| 6 | `VID_ENC_PAUSE` |  |
| 7 | `VID_ENC_RESUME` |  |
| 8 | `VID_ENC_DURATION_SET` |  |
| 9 | `VID_ENC_TIME_LAPSE_SET` |  |
| 10 | `VID_ENC_SLOW_SET` |  |
| 11 | `VID_ENC_CAPTURE` | VID_ENC_CAPTURE (alt photo path) |
| 12 | `AUD_ENC_VOLUME_SET` |  |
| 13 | `VID_ENC_CAPTURE_TIMER_SHOT_SET` |  |
| 14 | `VID_ENC_CAPTURE_AUTO_SHOT_SET` |  |
| 15 | `VID_ENC_CAPTURE_SPORT_ORBIT_SHOT_SET` |  |
| 16 | `RESOLUTION_SET` | RESOLUTION_SET (poison-range hw setter) |
| 17 | `AUTO_AWB_SET` |  |
| 18 | `AE_SET` |  |
| 19 | `AG_SET` |  |
| 20 | `HUE_SET` |  |
| 21 | `SATURATION_SET` |  |
| 22 | `BRIGHTNESS_SET` |  |
| 23 | `CONTRAST_SET` |  |
| 24 | `SHAPNESS_SET` |  |
| 25 | `ISO_SET` |  |
| 26 | `FLIP_SET` |  |
| 27 | `AUTO_FOCUS_SET` |  |
| 28 | `FRM_RATE_SET` |  |
| 29 | `DATE_TIME_SET` | time sync (confirmed) |
| 30 | `GSENSOR_SET` |  |
| 31 | `TIME_WATER_MARK_SET` |  |
| 32 | `LED_IND_SET` |  |
| 33 | `LIGHT_FREQ_SET` |  |
| 34 | `SCREEN_FLIP_SET` |  |
| 35 | `VEHICLE_MODE_SET` |  |
| 36 | `AUTO_PWR_OFF_SET` |  |
| 37 | `AUTO_SCREEN_SAVER_SET` |  |
| 38 | `FACTORY_RESTORE_SET` |  |
| 39 | `FIRMWARE_VERSION_GET` |  |
| 40 | `PWR_OFF` |  |
| 41 | `DISP_JPG_THUMB_LIST_DEC` |  |
| 42 | `DISP_JPG` |  |
| 43 | `DISP_VID_THUMB_LIST_DEC` |  |
| 44 | `VID_DEC_START` |  |
| 45 | `VID_DEC_STOP` |  |
| 46 | `VID_DEC_PAUSE` |  |
| 47 | `VID_DEC_RESUME` |  |
| 48 | `VID_DEC_SEEK` |  |
| 49 | `AUD_DEC_START` |  |
| 50 | `AUD_DEC_STOP` |  |
| 51 | `AUD_DEC_PAUSE` |  |
| 52 | `AUD_DEC_RESUME` |  |
| 53 | `AUD_DEC_VOLUME_SET` |  |
| 54 | `AUD_DEC_FF` |  |
| 55 | `AUD_DEC_FB` |  |
| 56 | `DEL_FILE` |  |
| 57 | `DEL_ALL_FILE` |  |
| 58 | `LOCK_FILE` |  |
| 59 | `LOCK_ALL_FILE` |  |
| 60 | `UNLOCK_FILE` |  |
| 61 | `UNLOCK_ALL_FILE` |  |
| 62 | `FORMAT` | FORMAT (SD format!) |
| 63 | `CARD_INFO_GET` | SD card info (CMD 63 {online,format}) |
| 64 | `MANUAL_LOCK` |  |
| 65 | `RECORDING_TIME_GET` |  |
| 66 | `PLAYING_TIME_GET` |  |
| 67 | `NET_DISCONN` |  |
| 68 | `NET_SSID` | WiFi SSID (confirmed) |
| 69 | `NET_PASSWORD` |  |
| 70 | `ENC_CAPTURE_SET_NUM` |  |
| 71 | `FIRMWARE_UPDATE` | FIRMWARE_UPDATE |
| 72 | `CFM_X_Y_UPDATE` |  |
| 73 | `FIRMWARE_VERSION` |  |
| 74 | `IND_SD_STS_UPDATE` |  |
| 75 | `PERSPECTIVE` | **= `CMD_CSI_SWITCH`, the downward/bottom camera switch.** Flips `cur_csi` (see CMD 0). Syma-app captured, PARAM shape TBD |
| 76 | `PALM` |  |
| 77 | `TRAIL` |  |
| 78 | `MAX` |  |
| 79 | `OPENPIP` |  |
| 80 | `FOLLOW_MODEL` | follow-target box; PARAM `{"W":w,"H":h,"Type":n}` (Type 18 seen; RESULT -1 when inactive) |
| 81 | `SWITCH_5G` |  |
| 82 | `AGPS_UPDATA` |  |
| 83 | `CMD_SAVE_FILE` |  |
| 84 | `CMD_COMMIT` | app_commit (confirmed) |
| 85 | `CMD_UPDATE_STATUS` | OTA update status (confirmed) |
| 86 | `CMD_LIVE_STATUS` |  |
| 87 | `CMD_MACADDR` | MAC (confirmed) |
| 88 | `CMD_GET_DEVICEID` | deviceID (confirmed) |
| 89 | `CMD_BIND_APP_UDP` | **BIND APP UDP** — telemetry-enable lead |
| 90 | `CMD_BIND_APP_UDP_STATUS` |  |
| 91 | `CMD_GET_DRONE_CONF` | DroneID capability dump (confirmed) |
| 92 | `CMD_SET_DRONE_CONF` |  |
| 93 | `CMD_GET_IMG_THUMBNAIL` |  |
| 94 | `CMD_GET_VIDEO_THUMBNAIL` |  |
| 95 | `CMD_THUMBNAIL_PREPARE` |  |
| 96 | `CMD_THUMBNAIL_RELEASE` |  |
| 97 | `CMD_IMG_RC_CAPTURE` | **TAKE PHOTO** (IMG_RC_CAPTURE) |
| 98 | `CMD_VID_RC_START` | **RECORD START** (VID_RC_START) |
| 99 | `CMD_VID_RC_STOP` | **RECORD STOP** (VID_RC_STOP) |
| 100 | `CMD_GET_VIDEO_STATUS` | video/record status (CMD 100 {recording}) |
| 101 | `CMD_STATION_ENABLE` |  |
| 102 | `CMD_STATION_NET_SSID` |  |
| 103 | `CMD_STATION_CONNECT` |  |
| 104 | `CMD_STATION_DISABLE` |  |
| 105 | `CMD_AP_DISABLE` |  |
| 106 | `CMD_AP_ENABLE` |  |
| 107 | `CMD_PHOTO_THUMB_LIST_GET` |  |
| 108 | `CMD_PHOTO_THUMB_DATA_GET` |  |
| 109 | `CMD_VIDEO_THUMB_LIST_GET` |  |
| 110 | `CMD_VIDEO_THUMB_DATA_GET` |  |
| 111 | `CMD_OPTICAL_PARAM_SET` |  |
| 112 | `TMP_112` |  |
| 113 | `TMP_113` |  |
| 114 | `TMP_114` |  |
| 115 | `TMP_115` |  |
| 116 | `CMD_RGB_LIGHTS_SET` |  |
| 117 | `TMP_117` |  |
| 118 | `TMP_118` |  |
| 119 | `TMP_119` |  |
| 120 | `TMP_120` |  |
| 121 | `TMP_121` |  |
| 122 | `TMP_122` |  |
| 123 | `TMP_123` |  |
| 124 | `TMP_124` |  |
| 125 | `TMP_125` |  |
| 126 | `CMD_CAMERA_RESO_FPS_GET` | camera reso caps (confirmed). Req `{"camera":n}`. Reply: `{"reso":["2K_L","2.7K"],"2K_L":[50],"2.7K":[25],"cur_reso":..,"cur_fps":..}` — **only legal pairs: 2K_L@50, 2.7K@25** (drives valid CMD 127 params) |
| 127 | `CMD_CAMERA_RESO_FPS_SET` | camera reso/fps SET. **Safe with a valid PARAM** `{"camera":n,"reso":"2.7K"\|"2K_L","fps":25\|50}` → RESULT 0 (Syma-app confirmed 2026-07-05). Only "poisons"/resets the server when sent a bad PARAM like `-1` (our old sweep) |
| 128 | `CMD_SAVE` |  |
| 129 | `CMD_RELAY_PAIR_DISCONNECT` |  |
| 130 | `CMD_RELAY_SSID_UPDATE` |  |
| 131 | `CMD_FLASH_LED` |  |
| 132 | `CMD_RELAY_FIRWARE_UPDATE` | RELAY firmware update |
| 133 | `CMD_RELAY_FIRWARE_VERSION` |  |
| 134 | `CMD_GET_RELAY_SSID` |  |
| 135 | `CMD_TMP` |  |
| 136 | `CMD_RK_CPU_TEMP_REPORT` | **RK1109 CPU temp °C.** Req `{"control":0}` → replies `{"data":"96.0"}` (seen ~95–97°C) |
| 137 | `CMD_SET_LOG_LEVEL` |  |
| 138 | `CMD_STREAM_FORMAT` | stream format negotiate (confirmed) |
| 139 | `CMD_TMP_139` |  |
| 140 | `CMD_BINOCULAR_PARAM` |  |
| 141 | `CMD_RK_TURN_OFF_CONTROL` | RK turn-off control |
| 142 | `CMD_GET_RELAY_INFO` |  |
| 143 | `CMD_MAINCAMERA_PIXEL_FPS_GET` |  |
| 144 | `CMD_MAINCAMERA_PIXEL_FPS_SET` |  |
| 145 | `CMD_145` |  |
| 146 | `CMD_GET_LOG_LEVEL2` |  |
| 147 | `CMD_SET_LOG_LEVEL2` |  |
| 148 | `CMD_CHANNEL_SET` |  |
| 149 | `CMD_GET_PHOTO_CONF` |  |
| 150 | `CMD_PSEUDO_COLOR_CONF` | PARAM `{"pseudo":n}` (RESULT -1 when inactive) |
| 151 | `CMD_PSEUDO_ZOOM_CONF` | **digital/pseudo ZOOM.** Syma app sends it **by string name** `{"CMD":"CMD_PSEUDO_ZOOM_CONF","PARAM":{"operation":0,"camera":0,"zoom":1}}` (dispatcher accepts named CMDs, not just ints). 720p sensor → digital crop |
| 152 | `CMD_ADAPTIVE_BIT_RATE_CONF` |  |
| 153 | `CMD_BINOCULAR_OBSTACLE_CONF` |  |
| 154 | `CMD_154` |  |
| 155 | `CMD_155` |  |
| 156 | `CMD_156` |  |
| 157 | `CMD_157` |  |
| 158 | `CMD_158` |  |
| 159 | `CMD_159` |  |
| 160 | `CMD_160` |  |
| 161 | `CMD_161` |  |
| 162 | `CMD_GET_WIFI_SIGNAL_CONF` |  |
| 163 | `CMD_SET_C7_LOG_CONF` |  |
| 164 | `CMD_SET_CAMERA_ZOOM_CONF` |  |
| 165 | `CMD_GET_CAMERA_SDP_CONF` |  |

## Firmware dispatch table (`img_trans_app`, device side)

From the binary's dispatch table at `0x77ccc` (`drone_protocol/cmd_table.txt`). **Only the IDs below
have a real handler in `img_trans_app`** — everything else is a **null handler**, meaning the firmware
replies `cmd:%d is not supported` and the action (if any) is done by the **native lib** elsewhere
(`97` photo, `98/99` record, `89` bind-UDP, all the `*_SET` image setters, etc.). So a command being
*in the enum* ≠ the bridge handling it.

| CMD | Firmware name (device side) | Handler | Notes |
|----:|-----------------------------|---------|-------|
| 0 | `CMD_SYS_PARAM_GET` | `0x1d834` | hello (confirmed) |
| 1 | `CMD_STATE_SET` | `0x1d844` | |
| 4 | `CMD_VID_ENC_START` | `0x1dc78` | |
| 5 | `CMD_VID_ENC_STOP` | `0x1dcb4` | |
| 11 | `CMD_VID_ENC_CAPTURE` | `0x1dc40` | |
| 29 | `CMD_DATE_TIME_SET` | `0x1d758` | time sync (confirmed) |
| 56 | `CMD_DEL_FILE` | `0x1ebd0` | |
| 62 | `CMD_FORMAT` | `0x2b890` | SD format |
| 63 | `CMD_CARD_INFO_GET` | `0x2b788` | SD info (confirmed) |
| 68 | `CMD_NET_SSID` | `0x1dcec` | Wi-Fi SSID (confirmed) |
| 71 | `CMD_FIRMWARE_UPDATE` | `0x1de8c` | |
| 75 | `CMD_CSI_SWITCH` | `0x1df24` | app calls this `PERSPECTIVE` |
| 77 | `CMD_TRAIL` | `0x1dfd0` | |
| 78 | `CMD_BACKLIGHT_COMPENSATION` | `0x1d920` | app calls this `MAX` |
| **81** | **`CMD_5G_REGION_SEL`** | `0x1da6c` | **Wi-Fi band/region select** (app `SWITCH_5G`) — real radio setter |
| 84 | `CMD_COMMIT` | `0x1e26c` | git commit (confirmed) |
| 85 | `CMD_UPDATE_STATUS` | `0x1d9e4` | OTA status (confirmed) |
| 87 | `CMD_MACADDR` | `0x1e154` | MAC (confirmed) |
| 88 | `CMD_GETDEVID` | `0x1e0d4` | device id (confirmed) |
| 91 | `CMD_GET_DRONE_CONF` | `0x1e218` | DroneID dump (confirmed) |
| 92 | `CMD_SET_DRONE_CONF` | `0x1e2cc` | writes DroneID (persists to `/netPrivate`) |
| 100 | `CMD_GET_VIDEO_STATUS` | `0x21c3c` | record status (confirmed) |
| 112 | `CMD_FLASH_CTRL` | `0x1e7d8` | LED/flash control (app `TMP_112`) |
| 126 | `CMD_CAMERA_RESO_GET` | `0x1e5c4` | reso caps (confirmed) |
| 127 | `CMD_CAMERA_RESO_SET` | `0x1e438` | **POISON** (bad PARAM resets server) |
| 135 | `CMD_RKNN_LIBS_READY` | `0x1e9c0` | NPU/RKNN ready (app `CMD_TMP`) |
| 136 | `CMD_RK_CPU_TEMP_REPORT` | `0x1ed44` | CPU temp (confirmed) |
| 137 | `CMD_SET_HY_LOGLEVEL` | `0x1ee28` | loglevel (confirmed) |
| 138 | `CMD_VIDEO_ENCODE_FORMAT` | `0x1f0f8` | stream format (confirmed) |
| 140 | `CMD_BINOCULAR_PARAM` | `0x1ef38` | |
| 141 | `CMD_TURN_OFF_PARAM` | `0x1f034` | power-off params |
| 200 | `CMD_MAX` | `0x1ea0c` | table sentinel / max |

## Video (RTSP, TCP 19200) — credentials recovered
```
H.264:  rtsp://hy_syma:ls155633@192.168.100.1:19200/HYSymaH264VideoSMS
H.265:  rtsp://hy_syma:ls155633@192.168.100.1:19200/HYSymaH265VideoSMS
```

## Flight / camera-action channel (native, UDP 0x68 stick packets)
Photo/record/takeoff/calibrate are driven by the **native lib** (`nativeTakePicture`,
`Android_JNI_OnTakeOff`, `MSG_FLY_SEND_GYRO_ADJUST`/`MAG_ADJUST`, `MSG_FLY_RECEIVE_TAKEOFF/LANDED`),
not Java — so their exact wire PARAM/flag isn't in this base APK (native .so is in a split APK).
Confirmed by test: stick flag `0x04`=gyro calib, `0x08`=compass (mag) calib, `0x40`/`0x80`=gimbal (ePTZ).
Takeoff/land/photo/record likely ride other flag-byte values or the native control-expand field —
capture one app session per action to get exact bytes.

## Flight-control inventory (`FlySendInfo`, packed natively into the 0x68 stick packet)

From `org/libsdl/app/FlySendInfo.java`. The native builder `nativeGetFlySendDataA9HY` (worker at
`libmain.so` vaddr `0x626c8`) packs these fields into the control packet. Defaults confirm our wire
capture: sticks neutral 128 (`0x80`), trims 32 (`0x20`).

**Sticks / trim:** `roll=128`, `pitch=128`, `accelerate=128` (throttle), `yaw=128`,
`TrimRoll=32`, `TrimPitch=32`, `speedLevel`, `FastMode=1`.

**Flight actions:** `AutoTakeoff`, `AutoLand`, `GoHome`, `Goaway`, `Stop` (emergency),
`LockUnlock` (arm/disarm), `Fastup`, `Turnaround`.

**Camera / gimbal:** `Photo`, `Video`, `PTZ_H`, `PTZ_V` (gimbal tilt — our `0x40`/`0x80`).

**Calibration:** `LevelCor` (gyro — our `0x04`), `MagCor` (compass — our `0x08`).

**Modes:** `CFMode` (headless), `GpsMode`, `SportMode`, `IndoorMode`, `FlipMode`,
`FarFly`, `SkyFly`, `SpiraFly`, `onfirm_120m`, `visualOA`/`visualOAWarn` (obstacle avoidance).

**Follow / autonomous:** `FollowMe`, `VisionFollow`, `VisionType`, `followType`, `followDirection`,
`CircleFly`, `PointFly`, `CtrlPanel`, plus target coords `EX,EY,SX,SY`.

**Packet sizes:** base `data[13]` (our captured `68 01 0d`=13-byte payload), `data2[17]`,
`dataA9[17]`, `dataA9hy[18]`.

> The exact byte/bit offset of each flag inside the packet is set by the native worker. The cleanest
> way to finish the map: one PCAPdroid capture of the app doing **arm → takeoff → photo → record →
> land** (and a slow gimbal sweep). Each action toggles one field; diffing the control packets against
> the neutral baseline reveals exactly which byte/bit it is. Decode with `docs/pcap_camera.py`.
