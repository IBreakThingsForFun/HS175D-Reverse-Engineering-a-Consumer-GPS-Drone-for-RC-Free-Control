#!/bin/sh
# dump_sram.sh <label> -- snapshot MM32 SRAM (0x20000000, 0x5000 = 20KB) to dumps/<label>.bin
# over SWD. Read-only / non-destructive. Requires NO other openocd running (one probe owner).
#
# Use for CONTROLLED single-variable captures (change ONE thing, grab two dumps each state):
#   sh tools/dump_sram.sh rcoff_A     # RC transmitter OFF
#   sh tools/dump_sram.sh rcoff_B
#   ... turn RC ON (bound, NOT armed) ...
#   sh tools/dump_sram.sh rcon_A
#   sh tools/dump_sram.sh rcon_B
# Then the rcoff_* vs rcon_* diff (with within-pair noise filter) isolates the RC-present flag.
L="${1:?usage: sh tools/dump_sram.sh <label>}"
cd "$(dirname "$0")/.."
BIN="openocd-bin/xpack-openocd-0.12.0-7/bin/openocd.exe"
SCR="openocd-bin/xpack-openocd-0.12.0-7/openocd/scripts"
"$BIN" -s "$SCR" -f interface/cmsis-dap.cfg -c "transport select swd" -f target/stm32f1x.cfg \
  -c init -c "dump_image dumps/$L.bin 0x20000000 0x5000" -c shutdown 2>&1 | grep -iE "dumped|error|fail" || true
echo "wrote dumps/$L.bin"
