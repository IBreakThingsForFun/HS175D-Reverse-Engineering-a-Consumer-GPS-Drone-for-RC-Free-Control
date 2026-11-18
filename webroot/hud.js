// HUD + sensor panel — renders every readable value, decoded and raw, from each telemetry frame.
(function () {
  const $ = id => document.getElementById(id);
  const fmt = v => (v === null || v === undefined) ? '—' : v;

  DS.onTel(t => {
    // overlay
    $('hMode').textContent = fmt(t.mode);
    $('hSats').textContent = fmt(t.sats);
    $('hFix').textContent = t.fix === null ? '—' : (t.fix ? 'LOCK' : 'no');
    $('hThr').textContent = t.sticks ? t.sticks.throttle : '—';
    $('hPos').textContent = (t.hasGps && t.lat != null)
      ? t.lat.toFixed(6) + ', ' + t.lon.toFixed(6) : 'no GPS';

    // top-bar pills
    const arm = $('armPill');
    arm.textContent = t.armed ? 'ARMED' : 'DISARMED';
    arm.className = 'pill' + (t.armed ? ' armed' : '');
    $('ownerPill').textContent = 'owner: ' + (t.control ? t.control.owner : '—');

    // readouts grid
    const rows = [
      ['connected', t.connected], ['armed', t.armed], ['model', fmt(t.model)],
      ['mode', fmt(t.mode)], ['modeByte', t.modeByte], ['sats', fmt(t.sats)],
      ['fix', t.fix === null ? '—' : t.fix], ['lat', t.lat != null ? t.lat.toFixed(7) : '—'],
      ['lon', t.lon != null ? t.lon.toFixed(7) : '—'],
      ['yaw', t.yawDeg != null ? t.yawDeg.toFixed(1) + '°' : '—'],
      ['roll', t.rollDeg != null ? t.rollDeg.toFixed(1) + '°' : '—'],
      ['pitch', t.pitchDeg != null ? t.pitchDeg.toFixed(1) + '°' : '—'],
      ['thr', t.sticks && t.sticks.throttle], ['stick yaw', t.sticks && t.sticks.yaw],
      ['stick pitch', t.sticks && t.sticks.pitch], ['stick roll', t.sticks && t.sticks.roll],
    ];
    $('readouts').innerHTML = rows.map(r =>
      '<div><span>' + r[0] + '</span><span>' + fmt(r[1]) + '</span></div>').join('');

    // raw undecoded frames
    $('raw').textContent =
      'last: ' + fmt(t.lastRaw) + '\n' +
      '8b:   ' + fmt(t.raw8b) + '\n' +
      '8c:   ' + fmt(t.raw8c);

    // 58xx subtype catalog
    if (t.subtypes) {
      $('subs').textContent = t.subtypes
        .sort((a, b) => a.sig.localeCompare(b.sig))
        .map(s => s.sig + '  n=' + String(s.count).padEnd(6) + ' len=' + String(s.len).padEnd(3) + ' ' + (s.sample || ''))
        .join('\n') || '(none yet)';
    }
  });
})();
