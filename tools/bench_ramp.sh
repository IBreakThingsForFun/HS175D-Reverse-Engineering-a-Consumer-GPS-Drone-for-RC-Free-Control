#!/bin/sh
# bench_ramp.sh -- reproduce the known-good RC-free control: optical hold + arm + TAKEOFF cmd + throttle.
#
# Pre-reqs:
#   - drone freshly power-cycled (clean SRAM), app connected
#   - optical hold running in another terminal:
#       openocd -f interface/cmsis-dap.cfg -c "transport select swd" -f target/stm32f1x.cfg \
#               -f tools/hold_optical.cfg -c init -c hold_optical
#
#   *** PROPS OFF. FC on its own battery. GPS UNPLUGGED. ***
#
# Key lesson (memory optical-mode-gate): motor START needs the takeoff command (payload[6]=0x02),
# NOT raw throttle. Throttle only controls things AFTER takeoff. So: arm -> takeoff -> throttle.
set -e
BUS=${BUS:-http://localhost:8731}
TGT=${TGT:-60}        # post-takeoff throttle target (0-255). props off, keep modest.
HOLD=${HOLD:-3}

c() { curl -s -m 8 "$BUS/c/$1"; echo; }

echo "== pre-flight =="; c "status"
printf "\nPROPS OFF + optical hold showing 230=01/bad=02 + connected? Enter to continue, Ctrl-C to abort... "; read _

echo "\n== arm =="; c "arm?on=true"

echo "\n== TAKEOFF command (payload[6]=0x02, pulsed 1500ms) =="
c "send?idx=6&val=02&ms=1500"
printf "watch motors -- did they start? Enter to follow with throttle... "; read _

echo "\n== throttle ramp to $TGT (smooth) =="
c "throttle.ramp?start=0&target=$TGT&step=5&delayMs=50&holdSec=$HOLD"
printf "motors respond to throttle now? Enter to finish... "; read _

echo "\n== stop + disarm =="; c "throttle.ramp.stop"; c "arm?on=false"
echo "== done. =="
