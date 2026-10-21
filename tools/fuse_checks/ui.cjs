const {chromium}=require(process.argv[2]||'playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),fixture=JSON.parse(fs.readFileSync(path.join(root,'obj/fuse-ui.json'),'utf8'));
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:740,height:680}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.setContent('<div id="pane-fuse"></div>');
  for(const file of ['base.css','craft.css','fuse.css'])if(fs.existsSync(path.join(root,'wwwroot/css',file)))await page.addStyleTag({path:path.join(root,'wwwroot/css',file)});
  await page.evaluate(fixture=>{
   window.DATA=fixture;window.ITEM=Object.fromEntries(fixture.items.map(i=>[i.id,i]));window.S={inventory:{}};
   window.busy=false;window.$=id=>document.getElementById(id);window.fmt=window.short=n=>String(n);
   window.itemLv=it=>it.itemLevel||1;window.itemImg=()=>'<span>◆</span>';window.itemTip=(k,text)=>text;
   window.qWord=r=>r;window.openTab=()=>{};window.calls=[];window.act=async url=>calls.push(url);
  },fixture);
  await page.addScriptTag({path:path.join(root,'wwwroot/js/fuse.js')});
  for(let q=0;q<9;q++){
   const id=fixture.items[q].id;
   await page.evaluate(id=>{fuseSel=[];S.inventory={[id]:9};renderFuse();},id);
   assert.equal(await page.locator('[data-fz="auto"]').isEnabled(),true,'autofill enabled at tier '+q);
   await page.locator('[data-fz="auto"]').click();
   assert.equal(await page.locator('[data-fs]').count(),9);
   assert.match(await page.locator('.fz-rate').innerText(),new RegExp(Math.round(fixture.fuseChance[q]*100)+'%'));
   assert.ok(!(await page.locator('.fz-rate').innerText()).includes('NaN'));
   if(q===0)assert.match(await page.locator('.fz-rules').innerText(),/Chắc chắn thành công/);
   else assert.match(await page.locator('.fz-rules').innerText(),/mất 8 món/);
   const tooltip=await page.locator('.fz-out').getAttribute('data-tip');
   assert.ok(tooltip.includes('Đạo → Huyền Tiên: 25%'));
   await page.locator('[data-fz="go"]').click();
   const url=new URL(await page.evaluate(()=>calls.at(-1)),'http://test');
   assert.deepEqual(url.searchParams.get('items').split(','),Array(9).fill(id));
  }
  await page.evaluate(id=>{fuseSel=[];S.inventory={[id]:9};renderFuse();},fixture.items[9].id);
  assert.equal(await page.locator('[data-fz="auto"]').isDisabled(),true,'max rarity excluded');
  assert.equal(await page.locator('[data-fz="go"]').isDisabled(),true);
  assert.deepEqual(errors,[]);
  console.log('PASS: all nine displayed rates, high-rarity autofill, exact nine-item requests, failure rules and max rarity exclusion.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
