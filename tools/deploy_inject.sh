#!/bin/sh
# Robust relaunch of img_trans_app under the ttyinject LD_PRELOAD shim.
kill -9 $(pidof img_trans_app) 2>/dev/null
i=0
while pidof img_trans_app >/dev/null 2>&1; do sleep 1; i=$((i+1)); [ $i -ge 15 ] && break; done
sleep 4
cd /img_trans
export LD_LIBRARY_PATH=/oem/usr/lib:/img_trans/libs:/usr/lib:$LD_LIBRARY_PATH
: > /tmp/ttyinject.log
: > /tmp/ttyinject.cmd
setsid env LD_PRELOAD=/tmp/ttyinject.so ./bin/img_trans_app >/tmp/app.out 2>&1 </dev/null &
sleep 5
echo "pid=$(pidof img_trans_app)"
echo "BASELINE mode pos17 (distinct):"
awk '$2=="R" && $5=="8b"{print $20}' /tmp/ttyinject.log | sort | uniq -c
echo "W funcs seen:"
awk '$2=="W"{print $5}' /tmp/ttyinject.log | sort | uniq -c
echo "sample 8b:"
awk '$2=="R" && $5=="8b"{ $1="";$2="";$3=""; print; exit }' /tmp/ttyinject.log
