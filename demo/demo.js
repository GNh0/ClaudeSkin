'use strict';
const $ = id => document.getElementById(id);
const themes={shorekeeper:{name:'Shorekeeper · 명조',kind:'gif',media:'shorekeeper.gif',poster:'shorekeeper-poster.png'},firefly:{name:'Firefly · 붕괴: 스타레일',kind:'video',media:'firefly.mp4',poster:'firefly-poster.png'},jinhsi:{name:'Jinhsi · 명조',kind:'video',media:'jinhsi.mp4',poster:'jinhsi-poster.png'}};
let css='';
async function apply(){
  if(!css)css=await fetch('skin.css').then(r=>{if(!r.ok)throw Error('Style unavailable');return r.text();});
  const theme=themes[$('theme').value];
  $('dim-value').textContent=$('dim').value+'%';$('theme-name').textContent=theme.name;
  globalThis.ClaudeSkin.apply({...theme,enabled:$('enabled').checked,motion:$('motion').checked,dim:Number($('dim').value)/100,media:new URL('media/'+theme.media,location.href).href,poster:new URL('media/'+theme.poster,location.href).href,css});
}
function surface(desktop){document.body.dataset.surface=desktop?'desktop':'web';$('desktop').classList.toggle('selected',desktop);$('web').classList.toggle('selected',!desktop);$('desktop').setAttribute('aria-pressed',String(desktop));$('web').setAttribute('aria-pressed',String(!desktop));$('surface').textContent=desktop?'Desktop adapter · sample UI':'Web companion · sample UI';}
$('desktop').addEventListener('click',()=>surface(true));$('web').addEventListener('click',()=>surface(false));
for(const id of ['theme','dim','motion','enabled'])$(id).addEventListener(id==='dim'?'input':'change',()=>apply().catch(()=>{}));
$('restore').addEventListener('click',()=>{$('enabled').checked=false;globalThis.ClaudeSkin.remove();});
apply().catch(()=>{$('theme-name').textContent='미디어를 불러오지 못했습니다.';});
