const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const handlers={},calls=[];
const pane={addEventListener:(name,fn)=>handlers[name]=fn};
const ctx=vm.createContext({
  S:{id:'player',map:{realm:1,area:4,areaMax:4,diff:0},logs:[]},DATA:{areas:15},
  localStorage:{getItem:()=>null,setItem:()=>{}},localLog:()=>{},
  $:()=>pane,IntersectionObserver:class{observe(){}},
  api:async(method,url)=>{calls.push([method,url]);return {realm:0,area:0,areaMax:15,diff:0};},
  act:path=>calls.push(['POST',path]),Date
});
vm.runInContext(fs.readFileSync('wwwroot/js/map.js','utf8'),ctx);
vm.runInContext('renderMap=()=>{};initMap();',ctx);
(async()=>{
  await vm.runInContext('previewChapter(0)',ctx);
  assert.equal(calls[0][0],'GET');
  assert.equal(ctx.S.map.realm,1);assert.equal(ctx.S.map.area,4);
  assert.equal(vm.runInContext('displayedMap().area',ctx),0);
  for(let index=1;index<=16;index++)assert.notEqual(ctx.mapStateOf({index,open:true,win:true}),'cur');
  const areaNode={dataset:{mx:'3'},getAttribute:()=>null};
  handlers.click({target:{closest:s=>s==='[data-mx]'?areaNode:null}});
  assert.deepEqual(calls.at(-1),['POST','map/0/area/3']);
  await vm.runInContext('previewChapter(1)',ctx);
  assert.equal(vm.runInContext('displayedMap().area',ctx),4);
  assert.equal(ctx.mapStateOf({index:4,open:true,win:true}),'cur');
  let resolve;
  ctx.api=()=>new Promise(r=>resolve=r);
  const pending=vm.runInContext('previewChapter(0)',ctx);
  await vm.runInContext('previewChapter(1)',ctx);
  resolve({realm:0,area:0,areaMax:15,diff:0});await pending;
  assert.equal(vm.runInContext('displayedMap().realm',ctx),1);
  console.log('PASS chapter preview, current marker, explicit travel and stale-response protection');
})().catch(e=>{console.error(e);process.exitCode=1;});
