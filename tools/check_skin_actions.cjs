const fs = require('fs'), vm = require('vm'), assert = require('assert');
let hero = '', calls = [];
const sandbox = {
  skinArt: () => hero,
  Image: class { constructor(){ this.complete = true; this.naturalWidth = 1920; } }
};
vm.createContext(sandbox);
const source = fs.readFileSync('wwwroot/js/skill-preview.js', 'utf8').split('const FIRE =')[0];
vm.runInContext(source, sandbox);
const g = {globalAlpha:1, save(){}, restore(){}, drawImage(...args){calls.push(args);} };
let checks = 0;
for (const id of ['trucco','kimdan','luyenhu','luyenkhi']){
  hero = `img/hero_${id}.png`;
  for (const [action,row] of Object.entries({idle:0,run:1,slash:2,cast:3})){
    const count = id === 'luyenkhi' ? {idle:8,run:7,slash:5,cast:3}[action] : 4;
    const width = id === 'luyenkhi' ? 384 : 480;
    const height = id === 'luyenkhi' ? 320 : 440;
    for (const p of [-1,0,.25,.5,.75,1,2]){
      assert(sandbox.drawSkinActor(g,action,p,100,200,48));
      const a = calls.at(-1);
      const hover = id === 'luyenhu' && action === 'run';
      assert.equal(a[1], hover ? 0 : Math.min(count-1,Math.max(0,Math.floor(p*count))) * width);
      assert.equal(a[2], id === 'luyenkhi' || action === 'run' ? 0 : row * height);
      assert.equal(a[3],width); assert.equal(a[4],height);
      assert(a.slice(5).every(Number.isFinite));
      assert(a[0].src.includes(id));
      if (id !== 'luyenkhi' && action === 'run') assert(a[0].src.endsWith(id === 'luyenhu' ? '_actions.png' : '_flight.png'));
      checks++;
    }
  }
  const count = id === 'luyenkhi' ? 7 : 4;
  const rate = id === 'luyenkhi' ? 10 : 4;
  const sampled = new Set(Array.from({length:count},(_,i)=>Math.floor(sandbox.skinActorLoop('run',(i+.1)/rate)*count)));
  assert.equal(sampled.size,count,'every run frame has equal exposure');
}
hero = 'img/unknown.png';
assert.equal(sandbox.drawSkinActor(g,'cast',0,0,0,48),false);
const battleSource=fs.readFileSync('wwwroot/js/battle.js','utf8');
const walkStart=battleSource.indexOf('    const flying = skinActorFlight();');
const walkEnd=battleSource.indexOf("  } else if (ph.name === 'enter')",walkStart);
const walkBranch=battleSource.slice(walkStart,walkEnd);
Object.assign(sandbox,{reduce:false,sz:48,t:1,dt:1,hx:100,g:200,U:v=>v,Math:Object.assign(Object.create(Math),{random:()=>0})});
for(const id of ['trucco','kimdan','luyenhu','luyenkhi']){
  hero=`img/hero_${id}.png`;sandbox.pfx=[];sandbox.hy=0;
  vm.runInContext(`{${walkBranch}}`,sandbox);
  if(id==='luyenkhi') assert.equal(sandbox.pfx.length,1,'runner keeps foot dust');
  else {assert.equal(sandbox.pfx.length,0,'flight has no foot dust');assert(sandbox.hy < -8,'actor stays airborne');}
}
const fxSource=fs.readFileSync('wwwroot/js/battle-fx.js','utf8');
vm.runInContext(fxSource.slice(fxSource.indexOf('function drawAirFlightFx('),fxSource.indexOf('function hcBattleDraw(')),sandbox);
let fxCalls=[];
const fx={save(){},restore(){},translate(){},scale(){},beginPath(){},stroke(){},fill(){},
  createRadialGradient(){return {addColorStop(){}};},arc(...args){fxCalls.push(args);}};
sandbox.drawAirFlightFx(fx,0,0,80,0,true);const still=JSON.stringify(fxCalls);
fxCalls=[];sandbox.drawAirFlightFx(fx,0,0,80,5,true);assert.equal(JSON.stringify(fxCalls),still,'reduced-motion effect stays static');
fxCalls=[];sandbox.drawAirFlightFx(fx,0,0,80,5,false);assert.notEqual(JSON.stringify(fxCalls),still,'normal effect rotates');
assert(fxCalls.flat().every(Number.isFinite));
console.log(`PASS: ${checks} frame cases (including fixed hover feet), flight altitude, no foot dust and reduced-motion foot effects`);
