const {chromium}=require(process.argv[2]||'playwright');
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),fixture=JSON.parse(fs.readFileSync(path.join(root,'obj/brew-ui.json'),'utf8'));
const html=`<!doctype html><html lang="vi" class="panel"><meta charset="utf-8">${['base','window','concept-shell','craft'].map(f=>'<link rel="stylesheet" href="/css/'+f+'.css">').join('')}<body class="panel"><aside class="book"><div class="tabs"><button class="tab">Luyện chế</button></div><div id="pane-craft"></div></aside><script>
window.DATA=${JSON.stringify(fixture)};window.ITEM=Object.fromEntries(DATA.items.map(i=>[i.id,i]));window.S={realm:6,realmName:'Hợp Thể',stageName:'Sơ kỳ',isPeak:true,hasNext:false,inventory:{},qiReq:100};
window.$=id=>document.getElementById(id);window.busy=false;window.MODE='panel';window.calls=[];
window.fmt=n=>String(n);window.itemImg=k=>ITEM[k]?.icon?'<img src="'+ITEM[k].icon+'" alt="">':'<span>◉</span>';window.itemTip=k=>ITEM[k]?.name||k;
window.qiNow=()=>0;window.realm=()=>DATA.realms[S.realm];window.hpStoneHtml=()=>'';window.lcNav=()=>'<b>Đan dược</b>';
window.act=async url=>{calls.push(url);const u=new URL(url,'http://test'),id=u.pathname.split('/').at(-1),b=DATA.brews.find(b=>b.out===id);const max=Math.min(...Object.entries(b.in).map(([k,c])=>Math.floor((S.inventory[k]||0)/c)));const n=u.searchParams.get('n')==='0'?max:Number(u.searchParams.get('n'));for(const [k,c] of Object.entries(b.in))S.inventory[k]-=c*n;S.inventory[id]=(S.inventory[id]||0)+n;renderCraft();};
</script><script src="/js/craft.js"></script><script src="/js/notify.js"></script><script>initDan();renderCraft();requestAnimationFrame(ddFrame);</script></body></html>`;
const server=http.createServer((req,res)=>{
 if(req.url==='/'){res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html);return;}
 const file=path.resolve(root,'wwwroot','.'+decodeURIComponent(req.url.split('?')[0]));
 if(!file.startsWith(path.join(root,'wwwroot')+path.sep)){res.writeHead(403);res.end();return;}
 res.setHeader('Content-Type',({'.css':'text/css','.js':'text/javascript','.png':'image/png'})[path.extname(file)]||'text/plain');
 fs.readFile(file,(error,data)=>{res.statusCode=error?404:200;res.end(error?'missing':data);});
});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:740,height:680}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));await page.goto(`http://127.0.0.1:${server.address().port}`);
  assert.equal(await page.locator('[data-pill]').count(),fixture.brews.length);
  assert.ok(!(await page.locator('#pane-craft').innerText()).includes('Hợp thành'));
  assert.equal(await page.locator('[data-dd="synth"]').count(),0);
  for(const b of fixture.brews){
   await page.evaluate(b=>{S.inventory=Object.fromEntries(Object.entries(b.in).map(([k,c])=>[k,c*2]));renderCraft();},b);
   await page.locator(`[data-pill="${b.out}"]`).click();
   assert.equal(await page.locator('[data-dd="brew"]').isEnabled(),true);
   assert.equal(await page.locator('[data-dd="ten"]').isDisabled(),true);
   const item=fixture.items.find(i=>i.id===b.out);assert.ok((await page.locator('#ddOut').innerText()).includes(item.name));
   const before=await page.evaluate(()=>calls.length);
   await page.locator('[data-dd="brew"]').click();
   await page.waitForFunction(before=>calls.length>before,before);
   assert.equal(await page.evaluate(()=>calls.at(-1)),`brew/${b.out}?n=1`);
   assert.equal(await page.evaluate(id=>S.inventory[id],b.out),1);
   assert.equal(await page.evaluate(()=>ddAnim.item),b.out);
   await page.locator('#ddCanvas').focus();
   await page.evaluate(()=>document.activeElement.blur());
   await page.keyboard.press('Enter');
   assert.equal(await page.evaluate(()=>calls.at(-1)),`brew/${b.out}?n=1`);
   assert.equal(await page.locator('[data-dd="brew"]').isDisabled(),true);
  }
  const b=fixture.brews.find(b=>b.out==='hop_the_dan');
  await page.evaluate(b=>{S.inventory=Object.fromEntries(Object.entries(b.in).map(([k,c])=>[k,c*9]));renderCraft();},b);
  await page.locator('[data-pill="hop_the_dan"]').click();
  assert.equal(await page.locator('[data-dd="ten"]').isDisabled(),true);
  const beforeTen=await page.evaluate(()=>calls.length);
  await page.evaluate(()=>ddBrew('hop_the_dan',10));
  assert.equal(await page.evaluate(()=>calls.length),beforeTen,'insufficient x10 does not send request');
  await page.evaluate(b=>{S.inventory=Object.fromEntries(Object.entries(b.in).map(([k,c])=>[k,c*10]));renderCraft();},b);
  assert.equal(await page.locator('[data-dd="ten"]').isEnabled(),true);
  await page.locator('[data-dd="ten"]').click();
  assert.equal(await page.evaluate(()=>calls.at(-1)),'brew/hop_the_dan?n=10');
  assert.equal(await page.evaluate(()=>S.inventory.hop_the_dan),10);
  assert.ok(await page.evaluate(()=>DD_BREW_ANIM.heat+DD_BREW_ANIM.reveal<=1.1),'brew animation finishes in about one second');
  await page.evaluate(b=>{S.inventory=Object.fromEntries(Object.entries(b.in).map(([k,c])=>[k,c*3]));renderCraft();},b);
  await page.locator('[data-pill="hop_the_dan"]').click();
  await page.locator('[data-dd="all"]').click();
  assert.equal(await page.evaluate(()=>calls.at(-1)),'brew/hop_the_dan?n=0');
  assert.equal(await page.evaluate(()=>S.inventory.hop_the_dan),3);
  await page.evaluate(()=>{S.realm=2;renderCraft();});
  await page.locator('[data-pill="ngo_dao_dan"]').click();
  assert.equal(await page.locator('[data-dd="brew"]').isDisabled(),true);
  assert.match(await page.locator('#ddAct').innerText(),/Nguyên Anh/);
  for(const width of [740,420]){
   await page.setViewportSize({width,height:680});
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'no horizontal overflow at '+width);
  }
  await page.setViewportSize({width:740,height:680});
  await page.locator('[data-pill="truc_co_dan"]').click();
  await page.screenshot({path:path.join(root,'obj/brew-direct-ui.png'),fullPage:true});
  assert.deepEqual(errors,[]);
  console.log('PASS: all recipes selectable, herb inputs, one/max/keyboard crafting, animations, realm locks, no synthesis UI and responsive layout.');
 }finally{await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});
