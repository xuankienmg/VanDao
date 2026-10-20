const{chromium}=require(process.argv[2]||'playwright');const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
const server=http.createServer((req,res)=>{const file=path.resolve(root,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}res.setHeader('Content-Type',{'.html':'text/html; charset=utf-8','.js':'text/javascript','.png':'image/png'}[path.extname(file)]||'application/octet-stream');fs.readFile(file,(e,b)=>{res.statusCode=e?404:200;res.end(e?'Not found':b)})});
(async()=>{await new Promise(r=>server.listen(0,'127.0.0.1',r));const browser=await chromium.launch({channel:'msedge',headless:true});try{
 const page=await browser.newPage({viewport:{width:740,height:650},deviceScaleFactor:1.5}),errors=[];page.on('pageerror',e=>errors.push(e.message));await page.goto(`http://127.0.0.1:${server.address().port}/tools/health_checks/preview.html`);
 await page.waitForFunction(()=>hero.complete&&hero.naturalWidth&&monster.complete&&monster.naturalWidth);await page.screenshot({path:path.join(root,'obj/hp-shield-start.png')});
 await page.click('#hit');assert.equal(await page.evaluate(()=>hp),98600);assert.equal(await page.evaluate(()=>guard),8000,'Shield must absorb damage before HP');
 await page.screenshot({path:path.join(root,'obj/hp-shield-damaged.png')});
 await page.click('#hit');assert.equal(await page.evaluate(()=>guard),0);assert.equal(await page.evaluate(()=>hp),82600);
 await page.click('#wave');assert.equal(await page.evaluate(()=>guard),0,'Next wave must not refill shield');
 for(let i=0;i<4;i++)await page.click('#hit');assert.equal(await page.evaluate(()=>hp),0);await page.waitForTimeout(1500);await page.screenshot({path:path.join(root,'obj/hp-critical.png')});
 await page.click('#heal');assert.equal(await page.evaluate(()=>hp),30000);await page.click('#wave');assert.equal(await page.evaluate(()=>mp),123000);
 await page.click('#round');assert.equal(await page.evaluate(()=>guard),32000);assert.equal(await page.evaluate(()=>hp),98600);
 await page.evaluate(()=>{const s=battleHpState('enemy','boss',123000,200000,0);drawBattleHp(ctx,{x:307,y:75,width:116,state:s,name:'Boss · Huyết Long',kind:'boss',burn:5,viewportWidth:420})});
 assert.deepEqual(errors,[]);console.log('PASS: 420px DPR1.5, shield visible from start, shield absorbs before HP, depleted shield, no wave refill, new round reset, healing and boss/burn rendering.');
}finally{await browser.close();server.close()}})().catch(e=>{console.error(e);server.close();process.exitCode=1});
