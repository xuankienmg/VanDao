// First run: dotnet run --project tools/skill_checks -- obj/skill-ui-data.json
// Then: node tools/skill_checks/ui.cjs [absolute path to playwright]
const {chromium} = require(process.argv[2] || 'playwright');
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const fixture = JSON.parse(fs.readFileSync(path.join(root, 'obj/skill-ui-data.json'), 'utf8'));
const html = `<!doctype html><html lang="vi" class="panel"><meta charset="utf-8"><link rel="stylesheet" href="/css/base.css"><link rel="stylesheet" href="/css/admin.css"><link rel="stylesheet" href="/css/window.css"><link rel="stylesheet" href="/css/skills.css"><body class="panel"><div class="wrap"><main class="grid"><aside class="book"><div class="tabs">${['Đột phá','Hành trang','Luyện chế','Bản đồ','Công pháp'].map(n=>`<button class="tab">${n}</button>`).join('')}</div><div id="pane-skill" class="pane"></div></aside></main></div><script>
const DATA=${JSON.stringify(fixture)};
let S={realm:0,skills:{hoa_linh_can:1},skillPoints:30,skillPointsTotal:30,skillSlots:['hoa_cau','']};
const $=id=>document.getElementById(id), hideTip=()=>{};let lastAction='';const act=action=>{lastAction=action};
</script><script src="/js/fire-fx-data.js"></script><script src="/js/fire-fx.js"></script><script src="/js/skills.js"></script><script src="/js/skill-preview.js"></script><script>renderSkill();initSkill();</script></body></html>`;
const server = http.createServer((req, res) => {
  if (req.url === '/') {res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html);return;}
  const file = path.resolve(root, 'wwwroot', '.' + decodeURIComponent(req.url.split('?')[0]));
  if (!file.startsWith(path.join(root, 'wwwroot') + path.sep)) {res.writeHead(403);res.end();return;}
  const type = {'.css':'text/css','.js':'text/javascript','.png':'image/png'}[path.extname(file)];
  if (type) res.setHeader('Content-Type',type);
  fs.readFile(file,(err, data)=>{res.statusCode=err?404:200;res.end(err?'Not found':data)});
});
(async()=>{
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const browser = await chromium.launch({channel:'msedge',headless:true});
  try {
    const page = await browser.newPage({viewport:{width:724,height:641}}), errors=[];
    page.on('pageerror',e=>{errors.push(e.message);console.error('Page error:',e.message)});
    await page.goto(`http://127.0.0.1:${server.address().port}`);
    await page.waitForFunction(()=>document.querySelectorAll('.sk-realm-band').length===DATA.realms.length);
    assert.equal(await page.locator('.sk-realm-band').count(),fixture.realms.length);
    assert.equal(await page.locator('.sk-detail').count(),0);
    const fits = () => page.evaluate(()=>{
      const book=document.querySelector('.book'), scroll=document.querySelector('.sk-path-scroll');
      return document.documentElement.scrollHeight<=innerHeight && book.scrollHeight<=book.clientHeight+1 && scroll.scrollHeight<=scroll.clientHeight+1 && scroll.scrollWidth<=scroll.clientWidth+1;
    });
    if (!await fits()) {
      console.log(await page.evaluate(()=>Object.fromEntries(['html','.book','#pane-skill','.sk-realms','.sk-path-scroll','.sk-realm-board'].map(s=>{const n=document.querySelector(s);return [s,{height:n.clientHeight,scroll:n.scrollHeight,width:n.clientWidth,sw:n.scrollWidth,rect:n.getBoundingClientRect().toJSON()}]}))));
      await page.screenshot({path:path.join(root,'obj/skills-default.png'),fullPage:true});
    }
    assert.ok(await fits(),'Default 740x680 window must fit without scrolling');
    await page.locator('.sk-realm-board [data-sk="hu_khong_hoa"]').click();
    assert.equal(await page.locator('[data-up="hu_khong_hoa"]').isDisabled(),true);
    assert.ok(await page.locator('[data-sk="hu_khong_hoa"]').getAttribute('data-tip'),'Hover tooltip must remain available');
    await page.locator('[data-up="liet_diem"]').click();
    assert.equal(await page.evaluate(()=>lastAction),'skill/liet_diem','Plus must act on its own skill');
    await page.evaluate(()=>{S.realm=5;S.skills.than_hoa=3;renderSkill()});
    assert.equal(await page.locator('[data-up="hu_khong_hoa"]').isEnabled(),true);
    await page.locator('[data-up="hu_khong_hoa"]').click();
    assert.equal(await page.evaluate(()=>lastAction),'skill/hu_khong_hoa');
    await page.evaluate(()=>{S.skills.hu_khong_hoa=1;renderSkill()});
    await page.locator('[data-up="hu_khong_hoa"]').click();
    assert.equal(await page.evaluate(()=>lastAction),'skill/hu_khong_hoa');
    await page.locator('[data-sk="hu_khong_hoa"]').dblclick();
    assert.equal(await page.evaluate(()=>lastAction),'skillslot/1?skill=hu_khong_hoa');
    await page.evaluate(()=>{S.skills.hu_khong_hoa=5;renderSkill()});
    assert.equal(await page.locator('[data-up="hu_khong_hoa"]').count(),0,'Maxed skills must hide plus');
    await page.evaluate(()=>{S.skills.hu_khong_hoa=1;S.skillPoints=0;renderSkill()});
    assert.equal(await page.locator('[data-up="hu_khong_hoa"]').isDisabled(),true,'No points must disable plus');
    await page.evaluate(()=>{S.skillPoints=30;renderSkill()});
    await page.evaluate(()=>{S.realm=6;S.skills.hu_khong_hoa=3;S.skills.thai_hu_hoa=3;renderSkill()});
    assert.equal(await page.locator('[data-up="hon_nguyen_hoa"]').isEnabled(),true);
    assert.equal(await page.locator('[data-up="hoa_dao_hop_nhat"]').isEnabled(),true);
    await page.locator('[data-up="hon_nguyen_hoa"]').click();
    assert.equal(await page.evaluate(()=>lastAction),'skill/hon_nguyen_hoa');
    await page.evaluate(()=>{S.skills.hon_nguyen_hoa=1;renderSkill()});
    await page.locator('[data-sk="hon_nguyen_hoa"]').dblclick();
    assert.equal(await page.evaluate(()=>lastAction),'skillslot/1?skill=hon_nguyen_hoa');
    assert.notEqual(await page.locator('[data-sk="hu_khong_hoa"] img').evaluate(i=>getComputedStyle(i).filter),await page.locator('[data-sk="xich_diem_thien"] img').evaluate(i=>getComputedStyle(i).filter));
    await page.screenshot({path:path.join(root,'obj/skills-default.png'),fullPage:true});
    await page.locator('[data-el="thuy"]').click();
    assert.equal(await page.locator('.sk-realm-board .sk-n').count(),fixture.skillTree.filter(s=>s.element==='thuy'&&s.category!=='Đòn thường').length);
    assert.ok(await fits(),'Water tree must fit default window');
    await page.setViewportSize({width:1440,height:860});
    await page.screenshot({path:path.join(root,'obj/skills-wide.png'),fullPage:true});
    for (const width of [700,420]) {
      await page.setViewportSize({width,height:641});
      assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Page overflow at '+width);
      if (width===700) assert.ok(await fits(),'700px panel must not scroll');
      await page.screenshot({path:path.join(root,`obj/skills-${width}.png`),fullPage:true});
    }
    await page.locator('[data-el="moc"]').click();
    assert.equal(await page.locator('.sk-realm-board .sk-n').count(),0);
    for(const element of ['hoa','thuy']) {
      await page.locator(`[data-el="${element}"]`).click();
      assert.ok(await page.locator('.sk-n img').evaluateAll(imgs=>imgs.every(i=>i.complete&&i.naturalWidth>0)),'Missing icons');
    }
    assert.deepEqual(errors,[]);
    console.log('PASS: default window and 700px panel fit all realm tiers; Hop The learns/equips, distinct fire colors, locks, upgrades, tooltips and images work.');
  } finally {await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1});
