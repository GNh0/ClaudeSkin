'use strict';
let revision = null;
function apply(payload) {
  if (payload?.enabled && payload.revision === revision) return;
  try { globalThis.ClaudeSkin.apply(payload); revision = payload?.enabled ? payload.revision : null; }
  catch { globalThis.ClaudeSkin.remove(); revision = null; }
}
chrome.runtime.onMessage.addListener((message, sender) => {
  if (sender.id === chrome.runtime.id && message?.type === 'claudeskin:apply') apply(message.payload);
});
chrome.runtime.sendMessage({type:'claudeskin:get'}).then(apply).catch(() => {});
setInterval(() => { chrome.runtime.sendMessage({type:'claudeskin:get'}).then(apply).catch(() => {globalThis.ClaudeSkin.remove();revision=null;}); }, 5000);