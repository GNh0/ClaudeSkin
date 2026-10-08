import {test} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import {parseHTML} from 'linkedom';
const engine=fs.readFileSync(new URL('../shared/skin-engine.js',import.meta.url),'utf8');
function fixture(reduce=false) {
  const {document}=parseHTML('<html><head></head><body><main><textarea>original</textarea></main></body></html>');
  const events=new Set(); const query={matches:reduce,addEventListener:(_,f)=>events.add(f),removeEventListener:(_,f)=>events.delete(f)};
  const create=document.createElement.bind(document);
  document.createElement=name=> {const node=create(name);if(name==='video'){node.play=()=>{node.played=true;return Promise.resolve();};node.pause=()=>{node.paused=true;};}return node;};
  const context=vm.createContext({document,location:{href:'https://demo.test/new',origin:'https://demo.test'},URL,matchMedia:()=>query});
  vm.runInContext(engine,context);
  const payload={enabled:true,kind:'video',media:'data:video/mp4;base64,AAAA',poster:'data:image/png;base64,AAAA',css:'body{}',motion:true,dim:.4};
  return {document,query,events,api:context.ClaudeSkin,payload};
}
test('theme swaps retain the conversation and disabling fully restores original DOM',()=>{
  const f=fixture();const before=f.document.querySelector('main').outerHTML;
  f.api.apply(f.payload);f.api.apply({...f.payload,kind:'gif'});
  assert.equal(f.document.querySelectorAll('#claudeskin-background').length,1);
  assert.equal(f.document.querySelector('main').outerHTML,before);
  f.api.remove();assert.equal(f.document.querySelector('#claudeskin-background'),null);
  assert.equal(f.document.querySelector('#claudeskin-style'),null);
  assert.equal(f.document.documentElement.hasAttribute('data-claudeskin'),false);
  assert.equal(f.events.size,0);
});
test('reduced motion uses still poster and changing the OS preference rebuilds once',()=>{
  const f=fixture(true);f.api.apply(f.payload);
  assert.equal(f.document.querySelector('#claudeskin-background img').src,f.payload.poster);
  f.query.matches=false;[...f.events].forEach(listener=>listener());
  assert.ok(f.document.querySelector('video'));assert.equal(f.events.size,1);
  f.document.hidden=true;f.document.dispatchEvent(new (parseHTML('<html/>').window.Event)('visibilitychange'));
  assert.equal(f.document.querySelector('video').paused,true);
});
test('media loading errors fall back to poster without modifying existing chat',()=>{
  const f=fixture();f.api.apply(f.payload);const video=f.document.querySelector('video');
  video.dispatchEvent(new (parseHTML('<html/>').window.Event)('error'));
  assert.equal(f.document.querySelector('#claudeskin-background img').src,f.payload.poster);
});
test('rejects outside media URLs and restores after invalid payload',()=>{
  const f=fixture();f.api.apply(f.payload);
  assert.throws(()=>f.api.apply({...f.payload,media:'https://tracker.test/private'}));
  assert.equal(f.document.querySelector('#claudeskin-background'),null);
});
