const {chromium}=require(process.argv[2]||'playwright');
const fs=require('node:fs'),http=require('node:http'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),fixture=JSON.parse(fs.readFileSync(path.join(root,'obj/pet-skill-ui.json'),'utf8'));
const html=`<!doctype html><meta charset="utf-8"><style>body{margin:0;padding:12px;background:#071419;color:#ead8ac;font:14px system-ui}.cards{display:grid;grid-template-columns:repeat(3,320px);gap:10px}section{border:1px solid #70633d;background:#0b2025}h3{margin:8px;font-size:14px}canvas{display:block;width:320px;height:180px}</style><div class="cards" id="cards"></div><script>window.fixture=${JSON.stringify(fixture)};</script><script src="/js/pet-battle.js"></script>`;
const server=http.createServer((req,res)=>{
 if(req.url==='/'){res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html);return;}
 const file=path.resolve(root,'wwwroot','.'+decodeURIComponent(req.url.split('?')[0]));
 if(!file.startsWith(path.join(root,'wwwroot')+path.sep)){res.writeHead(403);res.end();return;}
 res.setHeader('Content-Type',({'.png':'image/png','.js':'text/javascript'})[path.extname(file)]||'text/plain');
 fs.readFile(file,(error,data)=>{res.statusCode=error?404:200;res.end(error?'missing':data);});
});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1004,height:900}}),errors=[],missing=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()===404)missing.push(r.url());});
  await page.goto(`http://127.0.0.1:${server.address().port}`);
  await page.evaluate(()=>{
   warmPetAttackArt();for(const d of fixture.petDefs)petBattleImage(d.forms[2].image);
   petBattleImage('img/hero_luyenkhi.png');petBattleImage('img/quai_yeu_lang.png');
  });
  await page.waitForFunction(()=>[...petBattleImages.values()].every(i=>i.complete&&i.naturalWidth>0));
  const result=await page.evaluate(()=>{
   let draws=0;const original=CanvasRenderingContext2D.prototype.drawImage;
   CanvasRenderingContext2D.prototype.drawImage=function(image,...a){
    if(!a.every(Number.isFinite))throw Error('Nonfinite sprite geometry');
    if(a.length===8&&(a[0]<0||a[1]<0||a[0]+a[2]>image.naturalWidth||a[1]+a[3]>image.naturalHeight))throw Error('Atlas frame outside image');
    draws++;return original.call(this,image,...a);
   };
   const options={heroX:95,targetX:255,targetSize:100,ground:168,heroSize:100,sx:1,sy:1,time:1,reduced:false,width:320,big:true};
   const phase={name:'fight',i:0,s:.50,trip:1};let checks=0;
   const check=(v,m)=>{if(!v)throw Error(m);checks++;};
   const states=[];
   for(const def of fixture.petDefs){
    const cast={turn:0,dmg:200,heal:['deer','leech','crane'].includes(def.skill)?100:0,guard:['frost','turtle','xuanwu'].includes(def.skill)?100:0,skill:def.forms[2].skillName,kind:def.skill,hits:['dragon','eagle'].includes(def.skill)?[60,60,80]:null};
    if(['frost','turtle','xuanwu','deer','crane'].includes(def.skill))cast.dmg=0;
    states.push({def,cast});
   }
   for(const [id,kind] of [['chu_tuoc','burn'],['thanh_moc_loc','regen'],['huyen_vu','reflect']])states.push({def:fixture.petDefs.find(d=>d.id===id),cast:{turn:0,dmg:kind==='regen'?0:100,heal:kind==='regen'?100:0,guard:0,kind,skill:kind}});
   for(const {def,cast} of states){
    const card=document.createElement('section');card.innerHTML='<h3>'+def.name+' · '+cast.kind+'</h3><canvas width="320" height="180"></canvas>';document.getElementById('cards').append(card);
    const c=card.querySelector('canvas'),g=c.getContext('2d'),battle={pet:{name:def.name,element:def.element,image:def.forms[2].image,casts:[cast]}};
    const background=()=>{g.clearRect(0,0,320,180);g.fillStyle='#0b2025';g.fillRect(0,0,320,180);g.strokeStyle='#3c5144';g.beginPath();g.moveTo(0,168);g.lineTo(320,168);g.stroke();g.drawImage(petBattleImage('img/hero_luyenkhi.png'),55,63,85,105);g.drawImage(petBattleImage('img/quai_yeu_lang.png'),215,68,90,100);};
    background();let before=draws;
    drawPetCompanion(g,battle,phase,options);
    check(draws>before+1,'attack art visible: '+cast.kind);
    const pixels=c.toDataURL();background();drawPetCompanion(g,battle,phase,options);check(c.toDataURL()===pixels,'deterministic replay: '+cast.kind);
    for(const reduced of [false,true])for(const width of [160,320,840])for(const s of [.18,.3,.45,.6,.81]){
     g.globalAlpha=.7;g.filter='brightness(.95)';const filter=g.filter;
     drawPetCompanion(g,battle,{...phase,s},{...options,reduced,width});
     check(g.globalAlpha===.7&&g.filter===filter&&g.getTransform().isIdentity,'canvas state restored: '+cast.kind);
    }
    g.globalAlpha=1;g.filter='none';background();drawPetCompanion(g,battle,phase,options);
    check(petAttackPlan(battle.pet,cast,{...phase,name:'walk'})===null,'no attack while walking');
    check(petAttackPlan(battle.pet,cast,{...phase,i:1})===null,'no attack on another turn');
    const events=[],seen={};petBattleFeedback(battle,phase,seen,e=>events.push(e),0,0);petBattleFeedback(battle,phase,seen,e=>events.push(e),0,0);check(events.length===1,'feedback emitted once');
   }
   const c=document.createElement('canvas'),g=c.getContext('2d');
   check(!petAttackSprite(g,{src:null},0,0,0,10),'unavailable art fails softly');
   const pet={casts:[{turn:0,kind:'dragon',hits:[1,2,3]}]};check(petAttackPlan(pet,pet.casts[0],phase).strikes===3,'three strikes preserved');
   return {checks,variants:states.length};
  });
  await page.screenshot({path:path.join(root,'obj/pet-attack-fx.png'),fullPage:true});
  assert.deepEqual(errors,[]);assert.deepEqual(missing,[]);
  console.log(`PASS: ${result.checks} render checks across ${result.variants} pet attack/support variants, atlas bounds, replay, reduced motion, canvas state and feedback.`);
 }finally{await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});
