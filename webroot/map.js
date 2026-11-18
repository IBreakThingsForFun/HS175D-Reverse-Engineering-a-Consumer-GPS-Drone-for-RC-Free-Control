// Offline GPS plot — no basemap (works on the drone's internet-less AP). Home = first fix; the drone and
// tapped waypoints are drawn in metres-from-home (local equirectangular). Tap sends an (UNVERIFIED) waypoint.
(function () {
  const canvas = document.getElementById('map');
  const ctx = canvas.getContext('2d');
  const W = canvas.width, H = canvas.height;
  let home = null, drone = null, scale = 3, wps = [];

  const mPerLat = 111320;
  function llToM(lat, lon) {
    return { x: (lon - home.lon) * mPerLat * Math.cos(home.lat * Math.PI / 180), y: (lat - home.lat) * mPerLat };
  }
  function mToPx(m) { return { x: W / 2 + m.x * scale, y: H / 2 - m.y * scale }; }
  function pxToLL(px, py) {
    const mx = (px - W / 2) / scale, my = (H / 2 - py) / scale;
    return { lat: home.lat + my / mPerLat, lon: home.lon + mx / (mPerLat * Math.cos(home.lat * Math.PI / 180)) };
  }

  function draw() {
    ctx.clearRect(0, 0, W, H);
    ctx.strokeStyle = '#2d333b'; ctx.lineWidth = 1;
    for (let x = 0; x <= W; x += 40) { ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, H); ctx.stroke(); }
    for (let y = 0; y <= H; y += 40) { ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(W, y); ctx.stroke(); }
    if (!home) { ctx.fillStyle = '#8b949e'; ctx.fillText('waiting for GPS fix…', 10, 20); return; }
    const hp = mToPx({ x: 0, y: 0 });
    ctx.fillStyle = '#3fb950'; ctx.beginPath(); ctx.arc(hp.x, hp.y, 5, 0, 7); ctx.fill();
    ctx.fillStyle = '#8b949e'; ctx.fillText('H', hp.x + 7, hp.y + 3);
    wps.forEach((w, i) => {
      const p = mToPx(llToM(w.lat, w.lon));
      ctx.fillStyle = '#f59e0b'; ctx.beginPath(); ctx.arc(p.x, p.y, 4, 0, 7); ctx.fill();
      ctx.fillText(i + 1, p.x + 6, p.y);
    });
    if (drone) {
      const p = mToPx(llToM(drone.lat, drone.lon));
      ctx.fillStyle = '#22d3ee'; ctx.beginPath(); ctx.arc(p.x, p.y, 6, 0, 7); ctx.fill();
    }
    ctx.fillStyle = '#8b949e'; ctx.fillText(scale.toFixed(1) + ' px/m', 6, H - 6);
  }

  DS.onTel(t => {
    if (t.hasGps && t.lat != null) {
      if (!home) {
        home = { lat: t.lat, lon: t.lon };
        document.getElementById('mapHome').textContent = '(' + home.lat.toFixed(5) + ', ' + home.lon.toFixed(5) + ')';
      }
      drone = { lat: t.lat, lon: t.lon };
      const m = llToM(drone.lat, drone.lon);
      const need = Math.max(Math.abs(m.x), Math.abs(m.y), 10) * 1.3;
      scale = Math.max(0.5, Math.min(8, (W / 2 - 12) / need));
    }
    draw();
  });

  canvas.addEventListener('pointerdown', e => {
    if (!home) return;
    const rect = canvas.getBoundingClientRect();
    const px = (e.clientX - rect.left) / rect.width * W, py = (e.clientY - rect.top) / rect.height * H;
    const ll = pxToLL(px, py);
    const alt = +document.getElementById('wpAlt').value || 10;
    const spd = +document.getElementById('wpSpeed').value || 5;
    wps.push(ll); draw();
    DS.api('/api/gps', { mode: 'waypoint', lat: ll.lat, lon: ll.lon, alt, speed: spd, radius: 10, cmdId: 32 }).catch(() => {});
  });
  document.getElementById('wpClear').addEventListener('click', () => { wps = []; draw(); });

  draw();
})();
