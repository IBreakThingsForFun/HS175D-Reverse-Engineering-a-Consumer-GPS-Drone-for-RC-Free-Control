#!/bin/sh
# Bring img_trans_app back up under the ttyinject shim, retrying until the
# CMD TCP port leaves TIME_WAIT (so the bind stops failing with EADDRINUSE).
kill -9 $(pidof img_trans_app) 2>/dev/null
while pidof img_trans_app >/dev/null 2>&1; do sleep 1; done
cd /img_trans
export LD_LIBRARY_PATH=/oem/usr/lib:/img_trans/libs:/usr/lib
: > /tmp/ttyinject.log
: > /tmp/ttyinject.cmd
n=0
while [ $n -lt 25 ]; do
  n=$((n+1))
  setsid env LD_PRELOAD=/tmp/ttyinject.so ./bin/img_trans_app >/tmp/app.out 2>&1 </dev/null &
  sleep 5
  if pidof img_trans_app >/dev/null 2>&1; then
    echo "UP on attempt $n  pid=$(pidof img_trans_app)"
    break
  fi
  echo "attempt $n exited (port still busy), waiting..."
  sleep 4
done
echo "final_pid=$(pidof img_trans_app)"
echo "shim mapped: $(grep -c ttyinject /proc/$(pidof img_trans_app)/maps 2>/dev/null)"
echo "W funcs:"; awk '$2=="W"{print $5}' /tmp/ttyinject.log | sort | uniq -c
echo "mode pos17:"; awk '$2=="R" && $5=="8b"{print $20}' /tmp/ttyinject.log | sort | uniq -c
