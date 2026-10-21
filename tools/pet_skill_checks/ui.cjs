const {chromium}=require(process.argv[2]||'playwright');
const fs=require('node:fs'),http=require('node:http'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),fixture=JSON.parse(fs.readFileSync(path.join(root,'obj/pet-skill-ui.json'),'utf8'));
const html=`<!doctype html><html lang="vi" class="panel"><meta charset="utf-8"><link rel="stylesheet" href="/css/base.css"><link rel="stylesheet" href="/css/window.css"><link rel="stylesheet" href="/css/concept-shell.css"><link rel="stylesheet" href="/css/pet.css"><body class="panel"><aside class="book"><div class="tabs"><button class="tab">Linh thú</button></div><div id="pane-pet"></div></aside><script>
window.fixture=${JSON.stringify(fixture)};window.DATA={petDefs:fixture.petDefs};window.S={id:'test',inventory:{},pet:fixture.pet};window.busy=false;window.recvAt=performance.now();window.fmt=n=>Math.floor(n).toLocaleString('vi-VN');window.ITEM={};window.notifyHost=()=>{};
</script><script src="/js/pet.js"></script><script src="/js/pet-battle.js"></script><script>initPet();renderPet();</script></body></html>`;
const server=http.createServer((req,res)=>{
 if(req.url==='/'){res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html);return;}
 const file=path.resolve(root,'wwwroot','.'+decodeURIComponent(req.url.split('?')[0]));
 if(!file.startsWith(path.join(root,'wwwroot')+path.sep)){res.writeHead(403);res.end();return;}
 res.setHeader('Content-Type',({'.png':'image/png','.css':'text/css','.js':'text/javascript'})[path.extname(file)]||'text/plain');
 fs.readFile(file,(error,data)=>{res.statusCode=error?404:200;res.end(error?'missing':data);});
});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1000,height:740}}),errors=[],missing=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()===404)missing.push(r.url());});
  await page.goto(`http://127.0.0.1:${server.address().port}`);
  const dragon=fixture.pet.owned.find(p=>p.defId==='thanh_long');
  await page.locator(`[data-pet-select="${dragon.id}"]`).click();
  assert.match(await page.locator('.pet-element-badge').innerText(),/Lôi/);
  assert.match(await page.locator('.pet-skill-description').innerText(),/3 tia sét/);
  assert.match(await page.locator('.pet-owner-info').getAttribute('data-tip'),/8% tấn công/);
  assert.match(await page.locator('.pet-skill-summary').innerText(),/Thanh Long Thiên Phạt/);
  await page.locator('.pet-portrait img').waitFor();
  await page.waitForFunction(()=>[...document.querySelectorAll('.pet-evolution img,.pet-portrait img')].every(i=>i.complete&&i.naturalWidth>0));
  assert.match(await page.locator('.pet-portrait img').getAttribute('src'),/thanh_long_loi_2_hd/);
  await page.screenshot({path:path.join(root,'obj/pet-lightning-ui.png'),fullPage:true});
  for(const pet of fixture.pet.owned){
   await page.locator(`[data-pet-select="${pet.id}"]`).click();
   assert.equal(await page.locator('.pet-skill-description').innerText(),pet.skillText);
   assert.ok((await page.locator('.pet-owner-info').getAttribute('data-tip')).includes(pet.talentText));
  }
  for(const sample of fixture.starSamples){
   await page.evaluate(sample=>{S.pet.owned=[sample];renderPet(true);},sample);
   await page.locator(`[data-pet-select="${sample.id}"]`).click();
   assert.equal(await page.locator('.pet-skill-description').innerText(),sample.skillText);
   assert.ok(!(await page.locator('.pet-skill-description').innerText()).includes('/'));
   await page.locator('[data-pet-action="tab:star"]').click();
   if(sample.stars<5){
    assert.ok((await page.locator('.pet-star-effects').innerText()).includes(sample.nextStarSkillText));
    assert.ok((await page.locator('.pet-star-effects').innerText()).includes(sample.nextStarTalentText));
   }else assert.equal(await page.locator('.pet-star-effects').count(),0);
   await page.locator('[data-pet-action="tab:stable"]').click();
  }
  // Exercise the actual battle renderer at three-bolt phases and reduced motion.
  await page.evaluate(()=>{const c=document.createElement('canvas');c.id='pet-test';c.width=420;c.height=200;document.body.append(c);petBattleImage('img/pet/thanh_long_loi_2.png');});
  await page.waitForFunction(()=>!!petBattleImage('img/pet/thanh_long_loi_2.png'));
  await page.evaluate(()=>{
   const c=document.getElementById('pet-test'),g=c.getContext('2d');
   const battle={pet:{name:'Thanh Long',element:'loi',image:'img/pet/thanh_long_loi_2.png',casts:[{turn:0,dmg:225,hits:[75,75,75],kind:'dragon',skill:'Thanh Long Thiên Phạt'}]}};
   for(const reduced of [false,true])for(const s of [.3,.45,.6])drawPetCompanion(g,battle,{name:'fight',i:0,s},{heroX:100,targetX:330,ground:185,heroSize:100,sx:1,sy:1,time:1,reduced,width:420,big:true});
   const events=[];petBattleFeedback(battle,{name:'fight',i:0,s:.5,trip:1},{},e=>events.push(e),0,0);
   if(events.length!==1||!events[0].text.includes('225'))throw Error('Missing lightning damage feedback');
  });
  assert.deepEqual(errors,[]);assert.deepEqual(missing,[]);
  console.log('PASS: twelve skill descriptions/talents, lightning dragon portraits and element, all three evolution images, battle bolts and reduced motion.');
 }finally{await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});
