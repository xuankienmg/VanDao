const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const context={S:{exploreCooldownMs:10000,exploreReadyInMs:10000},recvAt:0,left:10000,exploreLeft:()=>context.left};
vm.createContext(context);
vm.runInContext(fs.readFileSync('wwwroot/js/battle.js','utf8'),context);
vm.runInContext(fs.readFileSync('wwwroot/js/health-bars.js','utf8'),context);
let checks=0;
for(const waves of [[],[{start:0},{start:2}]])for(const extra of [[],[true,false,true]])for(const casts of [[],[{turn:0},{turn:0},{turn:2},{turn:-1},{turn:8}]]){
  context.S.battle={hits:3,win:true,waves,extra,pet:{casts}};
  const petTurns=new Set(casts.filter(c=>c.turn>=0&&c.turn<3).map(c=>c.turn));
  const raw=2.5+3+petTurns.size*.8+extra.filter(Boolean).length*.34+Math.max(0,waves.length-1)*.8+1;
  const at=e=>{context.left=10000-e/raw*10000;return vm.runInContext('battlePhase()',context)};
  let x=2.5;
  assert.equal(at(.5).name,'walk');assert.equal(at(2.2).name,'enter');
  for(let i=0;i<3;i++){
    if(waves.length&&i===2){assert.equal(at(x+.4).name,'enter');x+=.8;}
    if(petTurns.has(i)){
      for(const s of [.01,.3,.5,.99]){const ph=at(x+s*.8);assert.equal(ph.name,'pet');assert.equal(ph.i,i);assert.ok(Math.abs(ph.s-s)<1e-9);checks++;}
      x+=.8;
    }
    const ph=at(x+.45);assert.equal(ph.name,'fight');assert.equal(ph.i,i);assert.ok(Math.abs(ph.s-.45)<1e-9);checks++;
    if(extra[i]){const rep=at(x+.7);assert.equal(rep.name,'fight');assert.equal(rep.rep,true);checks++;}
    x+=1+(extra[i]?.34:0);
  }
  assert.equal(at(x+.3).name,'win');
}
assert.equal(vm.runInContext("battleGuardAt({guard:100,guardLeft:[80,60],guardBefore:[100,75]},{name:'pet',i:1,s:.8})",context),75);
console.log(`PASS: ${checks} timeline checks; pet casts precede actor attacks, including duplicate events, extra hits and wave transitions. Guard remains at pre-attack value.`);
