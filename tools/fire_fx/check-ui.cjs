const {chromium}=require(process.argv[2]||'playwright');
const fs=require('node:fs'),http=require('node:http'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),types={'.html':'text/html; charset=utf-8','.js':'text/javascript','.png':'image/png','.css':'text/css'};
const fixture=JSON.parse(fs.readFileSync(path.join(root,'obj/skill-ui-data.json'),'utf8'));
const integration=`<!doctype html><html><head><base href="/wwwroot/"></head><body><canvas id="stage" width="420" height="300"></canvas><script>const DATA=${JSON.stringify(fixture)},S={realm:4,skills:{},skillSlots:['hoa_cau','than_hoa']},ctx=document.getElementById('stage').getContext('2d'),reduce=false,MODE='strip';const $=id=>document.getElementById(id);</script><script src="js/fire-fx-data.js"></script><script src="js/fire-fx.js"></script><script src="js/skills.js"></script><script src="js/skill-preview.js"></script><script src="js/battle-fx.js"></script></body></html>`;
const server=http.createServer((req,res)=>{const pathname=new URL(req.url,'http://localhost').pathname;if(pathname==='/integration'){res.setHeader('Content-Type','text/html; charset=utf-8');res.end(integration);return;}const file=path.resolve(root,'.'+decodeURIComponent(pathname));if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}res.setHeader('Content-Type',types[path.extname(file)]||'application/octet-stream');fs.readFile(file,(err,data)=>{res.statusCode=err?404:200;res.end(err?'Not found':data)})});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1100,height:850},deviceScaleFactor:1.5}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.goto(`http://127.0.0.1:${server.address().port}/tools/fire_fx/preview.html`);
  await page.evaluate(()=>fireQa.stop());
  for(const realm of [0,1,2,5]){
   await page.selectOption('#realm',String(realm));
   for(const id of ['hoa_cau','liet_diem','hoa_long','phan_thien','xich_diem_thien','than_hoa','hu_khong_hoa']){
    await page.selectOption('#skill',id);
    await page.evaluate(async({id,realm})=>{await fireQa.waitReady(id,realm);fireQa.stop();for(const s of [0,.25,.45,.60,.749,.75])fireQa.render(s)},{id,realm});
    assert.ok(await page.evaluate(()=>fireQa.cache().size<=2),'Cache over budget');
   }
  }
  await page.selectOption('#skill','than_hoa');
  await page.evaluate(async()=>{await fireQa.waitReady('than_hoa',5);fireQa.stop();fireQa.render(.5)});
  await page.screenshot({path:path.join(root,'obj/fire-than-hoa.png'),fullPage:true});
  await page.selectOption('#skill','hu_khong_hoa');
  await page.evaluate(async()=>{await fireQa.waitReady('hu_khong_hoa',5);fireQa.stop();fireQa.render(.5)});
  await page.screenshot({path:path.join(root,'obj/fire-hu-khong.png'),fullPage:true});
  await page.check('#reduced');await page.selectOption('#monster','1.5');await page.selectOption('#size','840');
  await page.evaluate(()=>fireQa.render(.45));
  await page.click('#benchmark');
  await page.waitForFunction(()=>document.getElementById('metrics').textContent.includes('1600 frame'));
  console.log(await page.locator('#metrics').innerText());
  // Chặn một sprite trong context sạch để kiểm tra tải lỗi không văng trang.
  const failed=await browser.newPage();await failed.route('**/fx/fire/than_hoa.png',route=>route.abort());
  await failed.goto(`http://127.0.0.1:${server.address().port}/tools/fire_fx/preview.html`);
  await failed.waitForTimeout(150);assert.equal(await failed.evaluate(()=>fireFxAvailable('than_hoa',4)),false);
  const battle=await browser.newPage();battle.on('pageerror',e=>errors.push(e.message));
  await battle.goto(`http://127.0.0.1:${server.address().port}/integration`);
  const actual=await battle.evaluate(async()=>{
    const wait=async id=>{const start=performance.now();while(!fireFxAvailable(id,4)){if(performance.now()-start>5000)throw Error('asset timeout');await new Promise(r=>setTimeout(r,20))}};
    const ref=new Image();ref.src='img/hoa_cau_ref_kimdan.png';await ref.decode();await wait('than_hoa');
    let fresh=0,old=0;const originalNew=drawFireFx,originalOld=skBattleDraw;
    drawFireFx=(...args)=>{fresh++;return originalNew(...args)};skBattleDraw=(...args)=>{old++;return originalOld(...args)};
    const o={ph:{name:'fight',s:.5},sx:1,sy:1,g:240,sz:100,t:0,hx:80,hy:0,hAlpha:1,hFlash:false,wolfX:310,casts:[{skill:'than_hoa',dmg:100,anim:'pillar'}],basic:'hoa_cau',cast:true};
    hcBattleDraw(ref,o);const fire={fresh,old};fresh=old=0;
    o.ph.s=.8;hcBattleDraw(ref,o);const rest={fresh,old};fresh=old=0;
    o.ph.s=.5;o.casts=[{skill:'thuy_nguyet',dmg:100,anim:'water_moon'}];o.basic='thuy_cau';hcBattleDraw(ref,o);const water={fresh,old};
    drawFireFx=originalNew;skBattleDraw=originalOld;return{fire,rest,water};
  });
  assert.deepEqual(actual,{fire:{fresh:1,old:0},rest:{fresh:0,old:0},water:{fresh:0,old:0}},'Actual combat must not double-render or leak fire into water');
  if(process.argv.includes('--record')){
    const captureDir=path.join(root,'obj/fire-frames');fs.mkdirSync(captureDir,{recursive:true});
    const videoPage=await browser.newPage({viewport:{width:1100,height:850},deviceScaleFactor:1});
    await videoPage.goto(`http://127.0.0.1:${server.address().port}/tools/fire_fx/preview.html`);
    for(const [id,realm]of [['hoa_cau',0],['liet_diem',0],['hoa_long',1],['phan_thien',1],['xich_diem_thien',2],['than_hoa',4],['hu_khong_hoa',5]]){
      await videoPage.selectOption('#realm',String(realm));await videoPage.selectOption('#skill',id);
      await videoPage.evaluate(async({id,realm})=>{await fireQa.waitReady(id,realm)},{id,realm});
      const times=[0,.0625,.125,.1875,.25,.30,.35,.40,.45,.4875,.525,.5625,.60,.6375,.675,.7125,.9];
      for(let i=0;i<times.length;i++){
        await videoPage.evaluate(s=>{fireQa.stop();fireQa.render(s)},times[i]);
        await videoPage.locator('#stage').screenshot({path:path.join(captureDir,`${id}-${String(i).padStart(2,'0')}.png`)});
      }
    }
    await videoPage.close();console.log('Saved animation frames to obj/fire-frames/');
  }
  assert.deepEqual(errors,[]);
  console.log('PASS: seven skills, 3 variants, phases, cache, DPR1.5, boss, reduced motion, x2, 100 turns, load failure and actual hcBattleDraw integration (no old/new overlap, no Fire on Water turns).');
 }finally{await browser.close();server.close()}
})().catch(e=>{console.error(e);server.close();process.exitCode=1});
