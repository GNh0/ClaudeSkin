'use strict';
const manager = 'http://127.0.0.1:9164';
let cache = null, revision = null, pending = null;
async function refresh() {
  if (pending) return pending;
  pending = (async () => {
    try {
      const response = await fetch(manager + '/state', {signal: AbortSignal.timeout(1800), cache:'no-store'});
      if (!response.ok) throw new Error('Manager unavailable');
      const state = await response.json();
      if (state.app !== 'ClaudeSkin' || typeof state.revision !== 'string') throw new Error('Unknown service');
      if (revision !== state.revision || !cache) {
        const skin = await fetch(manager + '/skin', {signal: AbortSignal.timeout(5000), cache:'no-store'});
        if (!skin.ok) throw new Error('Theme unavailable');
        cache = await skin.json(); revision = state.revision;
      }
      return {...cache, revision, connected:true};
    } catch {
      cache = null; revision = null;
      return {enabled:false, connected:false};
    } finally { pending = null; }
  })();
  return pending;
}
async function updateTabs() {
  const payload = await refresh();
  const tabs = await chrome.tabs.query({url:'https://claude.ai/*'});
  await Promise.allSettled(tabs.map(tab => chrome.tabs.sendMessage(tab.id, {type:'claudeskin:apply', payload})));
}
chrome.runtime.onMessage.addListener((message, sender, respond) => {
  const content = sender.id === chrome.runtime.id && sender.url?.startsWith('https://claude.ai/');
  const popup = sender.id === chrome.runtime.id && sender.url === chrome.runtime.getURL('popup.html');
  if (message?.type !== 'claudeskin:get' || (!content && !popup)) return false;
  refresh().then(respond, () => respond({enabled:false,connected:false}));
  return true;
});
chrome.alarms.onAlarm.addListener(alarm => { if (alarm.name === 'claudeskin-sync') updateTabs(); });
chrome.runtime.onInstalled.addListener(() => updateTabs());
chrome.runtime.onStartup.addListener(() => updateTabs());
chrome.alarms.get('claudeskin-sync').then(alarm => {
  if (!alarm) chrome.alarms.create('claudeskin-sync', {periodInMinutes:.5});
});
