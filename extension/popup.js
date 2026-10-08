async function refresh() {
  const state = await chrome.runtime.sendMessage({type:'claudeskin:get'});
  document.getElementById('status').textContent = !state?.connected ? 'ClaudeSkin.exe를 실행해 주세요.' : state.enabled ? `${state.name} · 적용 중` : '연결됨 · 웹 적용이 꺼져 있습니다.';
}
document.getElementById('refresh').addEventListener('click', () => refresh().catch(() => {}));
refresh().catch(() => {document.getElementById('status').textContent='연결 실패';});
