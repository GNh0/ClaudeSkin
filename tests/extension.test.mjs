import {test} from 'node:test';import assert from 'node:assert/strict';import vm from 'node:vm';import fs from 'node:fs';
test('content polling does not restart unchanged animated media and clears on disconnect',async()=>{
  let applies=0,removes=0,tick,payload={enabled:true,revision:'one'};
  const context=vm.createContext({ClaudeSkin:{apply:()=>applies++,remove:()=>removes++},chrome:{runtime:{id:'own',onMessage:{addListener(){}},sendMessage:async()=>payload}},setInterval:fn=>tick=fn});
  vm.runInContext(fs.readFileSync(new URL('../extension/content.js',import.meta.url),'utf8'),context);
  await new Promise(r=>setTimeout(r,0));assert.equal(applies,1);
  tick();await new Promise(r=>setTimeout(r,0));assert.equal(applies,1);
  payload={enabled:true,revision:'two'};tick();await new Promise(r=>setTimeout(r,0));assert.equal(applies,2);
  context.chrome.runtime.sendMessage=async()=>{throw new Error('closed');};tick();await new Promise(r=>setTimeout(r,0));assert.equal(removes,1);
});
test('extension restricts content to claude.ai and packaged engine stays identical',()=>{
  const root=new URL('../',import.meta.url);const manifest=JSON.parse(fs.readFileSync(new URL('extension/manifest.json',root)));
  assert.deepEqual(manifest.content_scripts[0].matches,['https://claude.ai/*']);
  assert.ok(!manifest.permissions.includes('cookies'));assert.ok(!manifest.permissions.includes('history'));
  assert.equal(fs.readFileSync(new URL('shared/skin-engine.js',root),'utf8'),fs.readFileSync(new URL('extension/skin-engine.js',root),'utf8'));
});
