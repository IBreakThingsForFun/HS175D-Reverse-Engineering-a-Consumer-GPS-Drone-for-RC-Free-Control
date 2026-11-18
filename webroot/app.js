// Drone RE Studio — browser cockpit bootstrap. Shared state, token-aware API, SSE telemetry fan-out,
// and the top-bar controls (ARM + TAKE CONTROL) and discrete command buttons.
const DS = {
  token: new URLSearchParams(location.search).get('t') || '',
  controlling: false,
  last: null,
  lastMs: 0,
  listeners: [],
  onTel(fn) { this.listeners.push(fn); },
};

DS.api = function (path, body) {
  const url = path + (path.includes('?') ? '&' : '?') + 't=' + encodeURIComponent(DS.token);
  const opts = { method: body === undefined ? 'GET' : 'POST', headers: { 'X-Studio-Token': DS.token } };
  if (body !== undefined) { opts.headers['Content-Type'] = 'application/json'; opts.body = JSON.stringify(body); }
  return fetch(url, opts);
};

// ---- SSE telemetry ----
function connectSSE() {
  const es = new EventSource('/api/stream?t=' + encodeURIComponent(DS.token));
  es.onmessage = (e) => {
    try {
      const t = JSON.parse(e.data);
      DS.last = t; DS.lastMs = Date.now();
      DS.listeners.forEach(fn => { try { fn(t); } catch (_) {} });
    } catch (_) {}
  };
  es.onerror = () => { /* EventSource auto-reconnects */ };
}

// ---- link freshness ----
setInterval(() => {
  const stale = Date.now() - DS.lastMs > 1500;
  document.body.classList.toggle('stale', stale);
  const link = document.getElementById('link');
  if (stale) { link.textContent = 'LINK STALE'; link.className = 'pill stale'; }
  else { link.textContent = 'LINK LIVE'; link.className = 'pill live'; }
}, 500);

// ---- ARM ----
const armBtn = document.getElementById('armBtn');
armBtn.addEventListener('click', () => {
  const armed = DS.last && DS.last.armed;
  DS.api('/api/arm', { on: !armed }).catch(() => {});
});

// ---- TAKE CONTROL (claim/release the stick lease) ----
const takeBtn = document.getElementById('takeBtn');
takeBtn.addEventListener('click', () => setControlling(!DS.controlling));

function setControlling(on) {
  DS.controlling = on;
  takeBtn.classList.toggle('on', on);
  takeBtn.textContent = on ? 'RELEASE' : 'TAKE CONTROL';
  if (on) {
    DS.api('/api/control/claim', {}).catch(() => {});
    if (window.startStickLoop) window.startStickLoop();
  } else {
    if (window.stopStickLoop) window.stopStickLoop();
    DS.api('/api/control/release', {}).catch(() => {});
  }
}

// release control if the page is hidden/closed — the server deadman also catches this within ~0.6s
['pagehide', 'beforeunload'].forEach(ev => window.addEventListener(ev, () => { if (DS.controlling) setControlling(false); }));
document.addEventListener('visibilitychange', () => { if (document.hidden && DS.controlling) setControlling(false); });

// ---- discrete command buttons ----
document.querySelectorAll('.cmd').forEach(b => {
  b.addEventListener('click', () => DS.api('/api/command', { name: b.dataset.cmd }).catch(() => {}));
});

connectSSE();
