#!/bin/sh
# diff_states.sh <stateA> <stateB> -- isolate the SRAM bytes that change BETWEEN two
# states while filtering out within-state jitter. Each state needs two dumps: <state>_A.bin
# and <state>_B.bin (capture with: sh tools/dump_sram.sh <state>_A ; sh tools/dump_sram.sh <state>_B).
#
# signal = offsets where (A_A vs B_A differ) AND (A_B vs B_B differ)   [consistent across the change]
#          MINUS union( within-A jitter, within-B jitter )            [not just noise]
#
# Example: sh tools/diff_states.sh baseline rcon   -> the RC-present flag(s)
set -e
A="${1:?usage: sh tools/diff_states.sh <stateA> <stateB>}"
B="${2:?usage: sh tools/diff_states.sh <stateA> <stateB>}"
cd "$(dirname "$0")/.."
D=dumps
for f in "$A"_A "$A"_B "$B"_A "$B"_B; do
  [ -f "$D/$f.bin" ] || { echo "missing $D/$f.bin"; exit 1; }
done

offs() { cmp -l "$1" "$2" 2>/dev/null | awk '{printf "%d\n", $1-1}'; }

# within-state noise (union of both states' jitter)
offs "$D/${A}_A.bin" "$D/${A}_B.bin" >  /tmp/_noise.$$
offs "$D/${B}_A.bin" "$D/${B}_B.bin" >> /tmp/_noise.$$

# cross-state diffs (must differ in BOTH pairings to count as consistent)
offs "$D/${A}_A.bin" "$D/${B}_A.bin" > /tmp/_x1.$$
offs "$D/${A}_B.bin" "$D/${B}_B.bin" > /tmp/_x2.$$

# consistent = x1 INTERSECT x2  (set ops via awk, order-independent)
awk 'NR==FNR{a[$0]=1;next} ($0 in a)' /tmp/_x1.$$ /tmp/_x2.$$ | sort -nu > /tmp/_consistent.$$

# signal = consistent MINUS noise
SIG=$(awk 'NR==FNR{n[$0]=1;next} !($0 in n)' /tmp/_noise.$$ /tmp/_consistent.$$ | sort -nu)

echo "=== $A -> $B : isolated signal bytes (noise-filtered) ==="
if [ -z "$SIG" ]; then
  echo "(none -- no stable byte distinguishes these states above the jitter floor)"
else
  for o in $SIG; do
    va=$(od -An -tx1 -j "$o" -N1 "$D/${A}_A.bin" | tr -d ' ')
    vb=$(od -An -tx1 -j "$o" -N1 "$D/${B}_A.bin" | tr -d ' ')
    printf "0x%05x  (0x2000%04x)  %s -> %s\n" "$o" "$o" "$va" "$vb"
  done
fi
rm -f /tmp/_noise.$$ /tmp/_noiseu.$$ /tmp/_x1.$$ /tmp/_x2.$$ /tmp/_consistent.$$