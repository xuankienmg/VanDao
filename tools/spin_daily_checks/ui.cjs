const {chromium}=require(process.argv[2]||'playwright');
const path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:740,height:680},reducedMotion:'reduce'}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.setContent('<div id="pane-spin"></div>');
  for(const file of ['base.css','craft.css','spin.css'])await page.addStyleTag({path:path.join(root,'wwwroot/css',file)});
  await page.evaluate(()=>{
   window.$=id=>document.getElementById(id);window.busy=false;window.calls=[];
   window.FUSE_R=['common','uncommon','rare','epic','legendary','mythic','divine','immortal','ancient','supreme'];
   window.RARITY=Object.fromEntries(FUSE_R.map(r=>[r,{c:'#d2b676',n:r}]));
   window.ITEM={gear:{id:'gear',name:'Kiếm',base:'sword',slot:'weapon',rarity:'common',minRealm:0}};
   window.DATA={items:Object.values(ITEM)};
   window.S={id:'test',realm:0,realmName:'Luyện Khí',inventory:{ve_tai_xiu:100,ve_linh_thu:100},spinDaily:{limit:100,remaining:0}};
   window.fmt=n=>String(n);window.qWord=r=>r;window.itemImg=()=>'<span>◆</span>';window.lcNav=()=>'';
   window.notifyHost=()=>{};window.localLog=()=>{};window.live=()=>{};window.setState=s=>{S=s;};
   window.api=async(method,url)=>{calls.push(url);const next=structuredClone(S);next.spinDaily.remaining--;return {state:next,craft:{recipe:'spin:0',outputs:[{item:'gear',qty:1}]}};};
  });
  await page.addScriptTag({path:path.join(root,'wwwroot/js/spin.js')});
  await page.evaluate(()=>renderSpin());
  const one=page.locator('[data-sp-go="1"]').last(),ten=page.locator('[data-sp-go="10"]');
  assert.equal(await one.isDisabled(),true);assert.equal(await ten.isDisabled(),true);
  assert.match(await page.locator('.sp-daily').innerText(),/0\/100/);
  assert.match(await page.locator('.sp-daily').innerText(),/00:00/);
  await page.evaluate(()=>{S.spinDaily.remaining=5;renderSpin();});
  assert.equal(await one.isEnabled(),true);assert.equal(await ten.isDisabled(),true);
  await page.evaluate(()=>spinGo(10));assert.equal(await page.evaluate(()=>calls.length),0);
  await page.locator('[data-sp-mode="pet"]').click();
  assert.match(await page.locator('.sp-daily').innerText(),/5\/100/);
  assert.equal(await ten.isDisabled(),true,'mode switch cannot restore quota');
  await page.evaluate(()=>{S.spinDaily.remaining=10;renderSpin();});
  assert.equal(await ten.isEnabled(),true);
  await page.locator('[data-sp-mode="gear"]').click();
  await page.evaluate(()=>{S.spinDaily.remaining=1;renderSpin();});
  await one.click();await page.waitForFunction(()=>!busy);
  assert.equal(await page.evaluate(()=>calls.length),1);
  assert.match(await page.locator('.sp-daily').innerText(),/0\/100/);
  assert.equal(await one.isDisabled(),true);
  // A new server state after midnight re-enables both batch sizes.
  await page.evaluate(()=>{S.spinDaily.remaining=100;renderSpin();});
  assert.equal(await one.isEnabled(),true);assert.equal(await ten.isEnabled(),true);
  assert.deepEqual(errors,[]);
  console.log('PASS: shared daily display, exhausted buttons, x10 boundary, blocked requests, final turn and midnight refresh.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
