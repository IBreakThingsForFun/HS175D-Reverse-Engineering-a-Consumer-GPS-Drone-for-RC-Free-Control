// Touch flight sticks (Mode 2) + the 20 Hz send loop that also serves as the deadman heartbeat.
// Left stick: vertical = throttle (HOLDS on release), horizontal = yaw (springs to centre).
// Right stick: pitch / roll (both spring to centre).
(function () {
  const state = { throttle: 128, yaw: 128, pitch: 128, roll: 128 };
  const toByte = n => Math.max(0, Math.min(255, Math.round(128 + n * 127)));

  function makeStick(canvas, onMove, springX, springY) {
    const ctx = canvas.getContext('2d');
    const R = canvas.width / 2, knobR = 22;
    let kx = 0, ky = 0, active = false, pid = null;

    function draw() {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.strokeStyle = '#444c56'; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.arc(R, R, R - 3, 0, 7); ctx.stroke();
      ctx.strokeStyle = '#373e47'; ctx.lineWidth = 1;
      ctx.beginPath(); ctx.moveTo(R, 8); ctx.lineTo(R, canvas.width - 8); ctx.moveTo(8, R); ctx.lineTo(canvas.width - 8, R); ctx.stroke();
      const px = R + kx * (R - knobR), py = R - ky * (R - knobR);
      ctx.fillStyle = active ? '#22d3ee' : '#0e7490';
      ctx.beginPath(); ctx.arc(px, py, knobR, 0, 7); ctx.fill();
    }
    function setFromEvent(e) {
      const rect = canvas.getBoundingClientRect();
      const x = (e.clientX - rect.left) / rect.width * 2 - 1;
      const y = -((e.clientY - rect.top) / rect.height * 2 - 1);
      kx = Math.max(-1, Math.min(1, x)); ky = Math.max(-1, Math.min(1, y));
      onMove(kx, ky); draw();
    }
    canvas.addEventListener('pointerdown', e => { active = true; pid = e.pointerId; canvas.setPointerCapture(pid); setFromEvent(e); });
    canvas.addEventListener('pointermove', e => { if (active && e.pointerId === pid) setFromEvent(e); });
    function release() { if (!active) return; active = false; if (springX) kx = 0; if (springY) ky = 0; onMove(kx, ky); draw(); }
    canvas.addEventListener('pointerup', release);
    canvas.addEventListener('pointercancel', release);
    draw();
    return { center() { kx = 0; ky = 0; draw(); } };
  }

  const left = makeStick(document.getElementById('stickL'), (x, y) => {
    state.yaw = toByte(x);
    state.throttle = Math.max(0, Math.min(255, Math.round((y + 1) / 2 * 255)));
  }, true, false);
  const right = makeStick(document.getElementById('stickR'), (x, y) => {
    state.roll = toByte(x); state.pitch = toByte(y);
  }, true, true);

  let timer = null;
  window.startStickLoop = function () {
    // Takeover defaults to hover (throttle 0x80 = altitude-hold) with centred axes — smooth handoff.
    state.throttle = 128; state.yaw = 128; state.pitch = 128; state.roll = 128;
    left.center(); right.center();
    if (timer) return;
    timer = setInterval(() => {
      DS.api('/api/sticks', state).then(r => { if (r.status === 409 && window.stopStickLoop) stopStickLoop(); }).catch(() => {});
    }, 50);
  };
  window.stopStickLoop = function () { if (timer) { clearInterval(timer); timer = null; } };

  DS.sticks = state;
})();
