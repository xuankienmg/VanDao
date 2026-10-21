const {chromium}=require(process.argv[2]||'playwright');
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),fixture=JSON.parse(fs.readFileSync(path.join(root,'obj/pet-star-ui.json'),'utf8'));
const html=`<!doctype html><html lang="vi" class="panel"><meta charset="utf-8"><link rel="stylesheet" href="/css/base.css"><link rel="stylesheet" href="/css/window.css"><link rel="stylesheet" href="/css/concept-shell.css"><link rel="stylesheet" href="/css/pet.css"><body class="panel"><aside class="book"><div class="tabs"><button class="tab">Linh thú</button></div><div id="pane-pet"></div></aside><script>
window.fixture=${JSON.stringify(fixture)};
window.DATA={petDefs:fixture.petDefs};window.S={id:'test',inventory:{},pet:structuredClone(fixture.pet)};
window.busy=false;window.recvAt=performance.now();window.calls=[];
window.fmt=n=>Math.floor(n).toLocaleString('vi-VN');window.ITEM={};window.notifyHost=()=>{};
window.setState=s=>{S=s;recvAt=performance.now();renderPet();};
window.api=async(method,url)=>{
 calls.push(url);if(window.fail){window.fail=false;throw Error('Nguyên liệu đã thay đổi');}
 const request=new URL(url,location.origin),ids=request.searchParams.get('foods').split(',');
 const main=S.pet.owned.find(p=>p.id===fixture.main);main.stars++;
 S.pet.owned=S.pet.owned.filter(p=>!ids.includes(p.id));
 return {state:structuredClone(S),message:'Đã ghép linh thú.'};
};
</script><script src="/js/pet.js"></script><script>initPet();renderPet();</script></body></html>`;
const server=http.createServer((req,res)=>{
 if(req.url==='/'){res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html);return;}
 const file=path.resolve(root,'wwwroot','.'+decodeURIComponent(req.url.split('?')[0]));
 if(!file.startsWith(path.join(root,'wwwroot')+path.sep)){res.writeHead(403);res.end();return;}
 res.setHeader('Content-Type',({'.css':'text/css','.js':'text/javascript','.png':'image/png'})[path.extname(file)]||'application/octet-stream');
 fs.readFile(file,(error,data)=>{res.statusCode=error?404:200;res.end(error?'missing':data);});
});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1000,height:740}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.goto(`http://127.0.0.1:${server.address().port}`);
  await page.locator('[data-pet-action="tab:star"]').click();
  for(let target=1;target<=5;target++){
   await page.evaluate(target=>{
    S.pet=structuredClone(fixture.pet);S.pet.owned.forEach(p=>{
     p.stars=p.id===fixture.main||p.id===fixture.primary?target-1:Math.max(0,target-2);
    });renderPet(true);
   },target);
   await page.locator(`[data-pet-action="star-main:${fixture.main}"]`).click();
   assert.equal(await page.locator('.pet-star-material').count(),target===1?1:2);
   assert.equal(await page.locator('[data-pet-action^="star-donor:2:"]').count(),0,'second arbitrary donor slot removed');
   assert.equal(await page.locator('[data-pet-action="merge-arm"]').isDisabled(),true);
   await page.locator(`[data-pet-action="star-donor:0:${fixture.primary}"]`).click();
   const expected=[fixture.primary];
   if(target>1){
    for(const invalid of [fixture.higher,fixture.lower,fixture.active])
     assert.equal(await page.locator(`[data-pet-action="star-donor:1:${invalid}"]`).count(),0);
    await page.locator(`[data-pet-action="star-donor:1:${fixture.extraA}"]`).click();
    expected.push(fixture.extraA);
   }
   assert.equal(await page.locator('[data-pet-action="merge-arm"]').isEnabled(),true);
   const before=await page.evaluate(()=>calls.length);
   await page.locator('[data-pet-action="merge-arm"]').click();
   assert.equal(await page.evaluate(()=>calls.length),before,'no request before confirmation');
   await page.locator('[data-pet-action="merge-cancel"]').click();
   assert.equal(await page.locator('[data-pet-action="merge-confirm"]').count(),0);
   await page.locator('[data-pet-action="merge-arm"]').click();
   if(target===3){
    await page.evaluate(()=>window.fail=true);
    await page.locator('[data-pet-action="merge-confirm"]').click();await page.waitForFunction(()=>!busy);
    assert.match(await page.locator('.pet-status').innerText(),/Nguyên liệu đã thay đổi/);
    assert.equal(await page.locator('[data-pet-action="merge-arm"]').isEnabled(),true,'retry retains selected donors');
    await page.locator('[data-pet-action="merge-arm"]').click();
   }
   if(target===5){
    for(const width of [1000,740,420]){
     await page.setViewportSize({width,height:740});
     assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'no horizontal overflow at '+width);
    }
    await page.setViewportSize({width:1000,height:740});
    await page.screenshot({path:path.join(root,'obj/pet-star-recipe.png'),fullPage:true});
   }
   await page.locator('[data-pet-action="merge-confirm"]').click();await page.waitForFunction(()=>!busy);
   const url=new URL(await page.evaluate(()=>calls.at(-1)),'http://test');
   assert.equal(url.pathname,`/api/players/test/pet/${fixture.main}/merge`);
   assert.deepEqual(url.searchParams.get('foods').split(','),expected);
   assert.equal(await page.evaluate(()=>S.pet.owned.find(p=>p.id===fixture.main).stars),target);
   for(const id of expected)assert.equal(await page.locator(`[data-pet-action="star-main:${id}"]`).count(),0);
   if(target<5)assert.equal(await page.locator('[data-pet-action="merge-arm"]').isDisabled(),true,'next tier requires a new complete recipe');
   else assert.match(await page.locator('.pet-merge').innerText(),/tối đa 5 sao/);
  }
  assert.deepEqual(errors,[]);
  console.log('PASS: all five recipe tiers, quality filters, distinct donors, confirmation/cancel, retry, request IDs, consumption, max stars and responsive layout.');
 }finally{await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});
