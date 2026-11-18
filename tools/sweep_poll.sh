#!/bin/sh
# No restart (assumes img_trans_app already up under the ttyinject shim).
# Perturb each payload byte of the accepted 0x0a poll frame and watch 0x8b pos17.
# poll = 68 0a 0f | P(15) | csum ; default P = 00x12 1d 74 00 ; csum = xor(0a,0f,P...).
P=$(pidof img_trans_app)
echo "pid=$P shim=$(grep -c ttyinject /proc/$P/maps 2>/dev/null)"
base=$(tail -60 /tmp/ttyinject.log | awk '$2=="R" && $5=="8b"{m=$20} END{print m}')
echo "BASELINE pos17=$base"

DEF="00 00 00 00 00 00 00 00 00 00 00 00 1d 74 00"   # 15 payload bytes
hits=0
i=0
while [ $i -lt 15 ]; do
  for V in 01 02 0e ff; do
    # rebuild payload with byte i = V
    j=0; pay=""; xs=$((0x0a ^ 0x0f))
    for b in $DEF; do
      if [ $j -eq $i ]; then b=$V; fi
      pay="$pay $b"; xs=$((xs ^ 0x$b)); j=$((j+1))
    done
    csh=$(printf '%02x' $xs)
    echo "30 68 0a 0f$pay $csh" > /tmp/ttyinject.cmd
    sleep 0.5
    cur=$(tail -40 /tmp/ttyinject.log | awk '$2=="R" && $5=="8b"{m=$20} END{print m}')
    if [ "$cur" != "$base" ] && [ -n "$cur" ]; then
      echo "*** CHANGE pos$i=$V  pos17: $base -> $cur"
      hits=$((hits+1))
    fi
  done
  i=$((i+1))
done
: > /tmp/ttyinject.cmd
echo "=== poll-perturb done. baseline=$base changes=$hits ==="
echo "pos17 distinct now:"; tail -100 /tmp/ttyinject.log | awk '$2=="R" && $5=="8b"{print $20}' | sort | uniq -c
echo "app up: pid=$(pidof img_trans_app)"
