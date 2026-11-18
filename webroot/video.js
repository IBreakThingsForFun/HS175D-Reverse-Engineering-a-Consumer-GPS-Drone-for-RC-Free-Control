// WebRTC (WHEP) video client — pulls the MediaMTX republish of the drone's H.264 RTSP into the <video>.
(function () {
  const video = document.getElementById('video');
  const novideo = document.getElementById('novideo');

  DS.api('/api/config').then(r => r.json()).then(cfg => {
    if (!cfg.videoReady) { novideo.style.display = 'flex'; return; }
    novideo.style.display = 'none';
    const url = `http://${location.hostname}:${cfg.videoPort}/${cfg.videoPath}/whep`;
    startWhep(url).catch(e => { novideo.textContent = 'video error: ' + e.message; novideo.style.display = 'flex'; });
  }).catch(() => { novideo.style.display = 'flex'; });

  async function startWhep(url) {
    const pc = new RTCPeerConnection();
    pc.addTransceiver('video', { direction: 'recvonly' });
    pc.ontrack = (e) => { video.srcObject = e.streams[0]; };
    await pc.setLocalDescription(await pc.createOffer());
    await iceComplete(pc);
    const res = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/sdp' }, body: pc.localDescription.sdp });
    if (!res.ok) throw new Error('WHEP ' + res.status);
    await pc.setRemoteDescription({ type: 'answer', sdp: await res.text() });
  }

  function iceComplete(pc) {
    return new Promise(res => {
      if (pc.iceGatheringState === 'complete') return res();
      const t = setTimeout(res, 1500);
      pc.addEventListener('icegatheringstatechange', () => {
        if (pc.iceGatheringState === 'complete') { clearTimeout(t); res(); }
      });
    });
  }
})();
