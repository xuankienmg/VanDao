const fs=require('fs'),vm=require('vm'),assert=require('assert');
let hero='img/hero_luyenkhi.png', calls=[], pending=false;
const still={hidden:false};
const g={globalAlpha:1,clearRect(){},save(){},restore(){},beginPath(){},rect(){},clip(){},drawImage(...args){calls.push(args)}};
const canvas={offsetParent:{},previousElementSibling:still,style:{},getContext(){return g}};
const c={S:{},$:()=>canvas,skinArt:()=>hero,reduce:false,
  Image:class{constructor(){this.complete=!pending;this.naturalWidth=pending?0:1920}}};
vm.createContext(c);
vm.runInContext(fs.readFileSync('wwwroot/js/skill-preview.js','utf8').split('const FIRE =')[0],c);
const src=fs.readFileSync('wwwroot/js/inventory.js','utf8');
const idleCode=src.slice(src.indexOf('function drawInventoryIdle('),src.indexOf('function renderInv(){'));
vm.runInContext(idleCode,c);
for(const id of ['luyenkhi','trucco','kimdan','luyenhu']){
  hero=`img/hero_${id}.png`;calls=[];
  c.drawInventoryAvatar(0);const first=calls.slice();calls=[];
  c.drawInventoryAvatar(.6);const next=calls.slice();
  assert(first.length>100&&first.length===next.length);assert(first[0][0].src.includes(id));
  assert(first.every(a=>a[1]===0),'source pose stays fixed');
  assert(first.some((a,i)=>a[5]!==next[i][5]||a[6]!==next[i][6]),'body moves continuously');
  assert.deepEqual(first.at(-1).slice(1),next.at(-1).slice(1),'feet remain fixed');
  assert.equal(still.hidden,true);assert.equal(canvas.style.visibility,'visible');
  calls=[];c.drawInventoryAvatar(.4-1/240);const before=calls.slice();
  calls=[];c.drawInventoryAvatar(.4+1/240);const after=calls.slice();
  assert(before.every((a,i)=>Math.abs(a[5]-after[i][5])<.1&&Math.abs(a[6]-after[i][6])<.1),'no jump at old frame boundary');
  c.reduce=true;calls=[];c.drawInventoryAvatar(10);assert.equal(calls.length,1);assert.equal(calls[0][1],0);c.reduce=false;
}
canvas.offsetParent=null;calls=[];c.drawInventoryAvatar(1);assert.equal(calls.length,0);canvas.offsetParent={};
hero='img/unknown.png';c.drawInventoryAvatar(2);assert.equal(still.hidden,false);assert.equal(canvas.style.visibility,'hidden');
// A fresh browser context loads the static portrait while the atlas is pending.
const waiting=vm.createContext({...c,Image:class{constructor(){this.complete=false;this.naturalWidth=0}}});
vm.runInContext(fs.readFileSync('wwwroot/js/skill-preview.js','utf8').split('const FIRE =')[0],waiting);
vm.runInContext(idleCode,waiting);
hero='img/hero_trucco.png';waiting.drawInventoryAvatar(0);assert.equal(still.hidden,false);
console.log('PASS: four continuous idles, stable source pose and feet, no frame-boundary jumps, reduced motion and fallback');
