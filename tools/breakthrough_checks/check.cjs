const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict'),path=require('node:path');
const root=path.resolve(__dirname,'../..');
let depth=0,trace=[];
function context2d(){return new Proxy({}, {
  get(o,key){
    if(key==='createLinearGradient'||key==='createRadialGradient')return (...args)=>{assert.ok(args.every(Number.isFinite));return {addColorStop(){}}};
    return o[key] ?? ((...args)=>{
      for(const n of args.filter(v=>typeof v==='number'))assert.ok(Number.isFinite(n),'Non-finite '+key);
      if(key==='save')depth++;
      if(key==='restore'){depth--;assert.ok(depth>=0,'Unbalanced restore')}
      trace.push([key,...args.map(a=>typeof a==='object'?'object':a)]);
    });
  },
  set(o,key,value){if(key==='globalAlpha')assert.ok(value>=0&&value<=1,'Invalid alpha '+value);o[key]=value;return true;}
})}
function canvas(){return {width:0,height:0,style:{},getContext:()=>context2d()}}
const portrait={...canvas(),offsetParent:{},parentElement:{getBoundingClientRect:()=>({width:420,height:600})},closest:()=>({querySelector:()=>({offsetHeight:90})})};
const data={realms:['luyen_khi','truc_co','kim_dan','nguyen_anh','hoa_than','luyen_hu'].map(id=>({id,stages:Array(10).fill('Tầng'),heroMed:id+'.png'}))};
const sandbox=vm.createContext({console,Math,Number,Map,Image:class{complete=true;naturalWidth=96;naturalHeight=96;},
  requestAnimationFrame(){},addEventListener(){},performance:{now:()=>0},devicePixelRatio:1,
  document:{createElement:()=>canvas()},$:id=>id==='btCanvas'?portrait:id==='btPlaque'?{offsetTop:0,offsetHeight:60}:null,
  DATA:data,S:{realm:0,stage:0},reduce:false,curSkin:()=>null,setTimeout,clearTimeout});
for(const name of ['breakthrough-evolution.js','breakthrough.js'])vm.runInContext(fs.readFileSync(path.join(root,'wwwroot/js',name),'utf8'),sandbox);
vm.runInContext('btReady=()=>false;',sandbox);
vm.runInContext('const originalEvolutionLayer=btEvolutionLayer;let lastRank=-1;btEvolutionLayer=(...args)=>{lastRank=args[2].rank;return originalEvolutionLayer(...args)};',sandbox);
for(let rank=0;rank<6;rank++)for(let stage=0;stage<10;stage++)for(const phase of [0,.3,.7,1]){
 sandbox.rank=rank;sandbox.stage=stage;sandbox.phase=phase;
 const state=vm.runInContext('btEvolutionState(rank,stage,10,1,phase,null,rank)',sandbox);
 assert.equal(state.rank,rank);assert.equal(state.points,stage+1);assert.equal(state.reveal,1);
 sandbox.S={realm:rank,stage};
 vm.runInContext('btProc=phase?{t0:0,dur:1000}:null;btFx=null;btFrame(phase*1000+1)',sandbox);
 assert.equal(depth,0,'Canvas state leaked');
}
// New detail must grow in only after a successful realm transition; same-realm advance keeps all details.
assert.equal(vm.runInContext('btEvolutionState(2,0,10,0,0,0,1).reveal',sandbox),0);
assert.equal(vm.runInContext('btEvolutionState(2,0,10,0,0,2,1).reveal',sandbox),1);
assert.equal(vm.runInContext('btEvolutionState(2,5,10,0,0,0,2).reveal',sandbox),1);
sandbox.S={realm:1,stage:4};
vm.runInContext("btProc=null;btFx={kind:'bad',t0:0,fromRealm:1,target:{realm:5,stage:0}};btFrame(400)",sandbox);
assert.equal(depth,0,'Failure canvas state leaked');
assert.equal(vm.runInContext('lastRank',sandbox),1,'Failure unlocked a realm');
vm.runInContext("btFx={kind:'gold',t0:0,fromRealm:1,target:{realm:2,stage:0}};btFrame(700)",sandbox);
assert.equal(vm.runInContext('lastRank',sandbox),2,'Success did not reveal new realm');
// Reduced-motion idle must produce identical visible drawing commands over time.
sandbox.reduce=true;
vm.runInContext('btFx=null;btProc=null;btFrame(1000)',sandbox);trace=[];
vm.runInContext('btFrame(2000)',sandbox);const first=JSON.stringify(trace);trace=[];
vm.runInContext('btFrame(3000)',sandbox);assert.equal(JSON.stringify(trace),first,'Reduced motion still animates');
console.log('PASS: 240 realm/stage/phase frames; finite canvas geometry, balanced state, progressive reveal, failure and reduced-motion idle.');
