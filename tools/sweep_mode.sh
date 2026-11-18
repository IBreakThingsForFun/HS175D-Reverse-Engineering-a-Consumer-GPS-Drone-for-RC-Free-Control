#!/bin/sh
# One-shot: (re)deploy img_trans_app under the ttyinject shim, then sweep
# candidate "set-mode" frames on ttyS1, watching 0x8b pos17 (0e=GPS / 02=optical)
# for ANY change. Frame = 68 <F> 01 <M> <csum>, csum = F xor 01 xor M.
# Runs entirely on the drone so it finishes inside the idle-off window.

kill -9 $(pidof img_trans_app) 2>/dev/null
while pidof img_trans_app >/dev/null 2>&1; do sleep 1; done
cd /img_trans
export LD_LIBRARY_PATH=/oem/usr/lib:/img_trans/libs:/usr/lib
: > /tmp/ttyinject.log
: > /tmp/ttyinject.cmd

# launch under shim, retry until the CMD port leaves TIME_WAIT
n=0
while [ $n -lt 20 ]; do
  n=$((n+1))
  setsid env LD_PRELOAD=/tmp/ttyinject.so ./bin/img_trans_app >/tmp/app.out 2>&1 </dev/null &
  sleep 5
  pidof img_trans_app >/dev/null 2>&1 && break
  sleep 4
done
P=$(pidof img_trans_app)
echo "pid=$P  shim_mapped=$(grep -c ttyinject /proc/$P/maps 2>/dev/null)  launch_attempts=$n"
[ -z "$P" ] && { echo "FATAL: app would not stay up"; exit 1; }

sleep 3
base=$(tail -80 /tmp/ttyinject.log | awk '$2=="R" && $5=="8b"{m=$20} END{print m}')
echo "BASELINE pos17=$base"
echo "=== sweep (only CHANGES and the final table are printed) ==="

FUNCS="02 03 04 06 07 08 09 0b 0c 0d 0e 0f 10 11 12 13 14 15 16 20 21 22 30 40 50 60 70 80 90 a0 b0 c0 d0 e0 f0"
hits=0
for F in $FUNCS; do
  for M in 02 0e; do
    Fd=$((0x$F)); Md=$((0x$M)); cs=$((Fd ^ 1 ^ Md)); csh=$(printf '%02x' $cs)
    echo "30 68 $F 01 $M $csh" > /tmp/ttyinject.cmd
    sleep 0.5
    cur=$(tail -40 /tmp/ttyinject.log | awk '$2=="R" && $5=="8b"{m=$20} END{print m}')
    if [ "$cur" != "$base" ] && [ -n "$cur" ]; then
      echo "*** CHANGE  F=$F M=$M  pos17: $base -> $cur"
      hits=$((hits+1))
    fi
  done
done
: > /tmp/ttyinject.cmd
echo "=== done. baseline=$base  changes=$hits ==="
echo "post-sweep pos17 distinct (should be back near baseline):"
tail -120 /tmp/ttyinject.log | awk '$2=="R" && $5=="8b"{print $20}' | sort | uniq -c
echo "app still up: pid=$(pidof img_trans_app)"
