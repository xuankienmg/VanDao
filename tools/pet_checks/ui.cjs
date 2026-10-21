// Isolated UI contract test. Fixtures exist only here; production uses server state.
const {chromium} = require(process.argv[2] || 'playwright');
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
(async () => {
  const browser = await chromium.launch({channel:'msedge',headless:true});
  try {
    const page = await browser.newPage({viewport:{width:740,height:680}}), errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    await page.setContent('<style>body{margin:0;padding:8px;background:#111}*{box-sizing:border-box}</style><button id="btnPet">Pets</button><div id="pane-pet"></div>');
    await page.addStyleTag({path:path.join(root,'wwwroot/css/pet.css')});
    await page.evaluate(() => {
      window.busy=false; window.recvAt=performance.now(); window.calls=[];
      window.fmt=n=>Math.floor(n).toLocaleString('vi-VN'); window.notifyHost=()=>{};
      window.ITEM={linh_thu_dan:{name:'Linh Thú Đan'},thu_dan_linh:{name:'Thú Đản Linh',type:'egg'}};
      window.DATA={petDefs:Array.from({length:12},(_,i)=>({id:'species'+i,name:'Linh thú '+i,element:i%2?'Băng':'Hỏa',rarity:'Linh',forms:[0,1,2].map(j=>({name:['Ấu thể','Thành thể','Thần thú'][j]})),skillEvery:4}))};
      window.S={id:7,inventory:{linh_thu_dan:20,thu_dan_linh:2,linh_thach:100},pet:{owned:[],hatch:[],hatchSlots:2,guardSlots:2,capacity:20,levelCap:40}};
      window.api=async(method,url)=>{calls.push(url);if(window.fail)throw Error('Thiếu nguyên liệu');return {state:S};};
      window.setState=s=>{S={...s,pet:{...s.pet}};recvAt=performance.now();renderPet();};
      window.makePet=(i)=>({id:i+1,defId:'species'+i,name:'Linh thú '+i,element:i%2?'Băng':'Hỏa',rarity:'Linh',level:i+1,exp:10,expNeed:100,form:0,slot:0,atk:30,def:20,hp:100,aptAtk:.92,aptDef:80,aptHp:100,shareAtk:3,shareDef:2,shareHp:10,talentText:'Thiên phú thật',skillText:'Kỹ năng thật',skillEvery:4,canEvolve:true,evolveCost:[{item:'linh_thu_dan',have:20,need:2}]});
    });
    await page.addScriptTag({path:path.join(root,'wwwroot/js/pet.js')});
    await page.evaluate(()=>{initPet();initPet();renderPet();});
    assert.equal(await page.locator('.pet-card').count(),0);
    await page.getByRole('button',{name:'Đến ổ ấp'}).click();
    assert.equal(await page.locator('.pet-nest').count(),2);
    await page.locator('[data-pet-action="hatch:1:thu_dan_linh"]').click();
    await page.waitForFunction(()=>!busy);
    assert.equal(await page.evaluate(()=>calls.at(-1)),'/api/players/7/hatch/thu_dan_linh?slot=1');
    await page.evaluate(()=>{S.pet.owned=Array.from({length:12},(_,i)=>makePet(i));S.pet={...S.pet};renderPet();});
    await page.getByRole('button',{name:'Chuồng thú',exact:true}).click();
    assert.equal(await page.locator('.pet-card').count(),12);
    await page.locator('[data-pet-select="1"]').click();
    await page.locator('[data-pet-action="feed:10"]').click(); await page.waitForFunction(()=>!busy);
    assert.match(await page.evaluate(()=>calls.at(-1)),/pet\/1\/feed\?n=10$/);
    await page.locator('[data-pet-action="slot:3"]').click(); await page.waitForFunction(()=>!busy);
    assert.match(await page.evaluate(()=>calls.at(-1)),/pet\/1\/slot\/3$/);
    await page.locator('[data-pet-action="evolve"]').click(); await page.waitForFunction(()=>!busy);
    assert.match(await page.evaluate(()=>calls.at(-1)),/pet\/1\/evolve$/);
    const before=await page.evaluate(()=>calls.length);
    await page.locator('[data-pet-action="release-arm"]').click();
    assert.equal(await page.evaluate(()=>calls.length),before);
    await page.locator('[data-pet-action="release-cancel"]').click();
    await page.locator('[data-pet-action="release-arm"]').click();
    await page.locator('[data-pet-action="release-confirm"]').click(); await page.waitForFunction(()=>!busy);
    assert.match(await page.evaluate(()=>calls.at(-1)),/pet\/1\/release$/);
    await page.evaluate(()=>{window.fail=true;});
    await page.locator('[data-pet-action="feed:1"]').click(); await page.waitForFunction(()=>!busy);
    assert.equal(await page.locator('[role="alert"]').textContent(),'Thiếu nguyên liệu');
    await page.evaluate(()=>{window.fail=false;});
    await page.locator('[data-pet-filter="element"]').selectOption('Băng');
    assert.equal(await page.locator('.pet-card').count(),6);
    await page.locator('[data-pet-filter="element"]').focus();
    await page.evaluate(()=>{window.focusedFilter=document.activeElement; S.pet={...S.pet};renderPet();});
    assert.equal(await page.evaluate(()=>document.activeElement===focusedFilter),true);
    await page.locator('[data-pet-filter="element"]').selectOption('');
    await page.evaluate(()=>{S.pet.owned[0].element='hoa';S.pet.owned[0].rarity='legendary';S.pet.owned[1].rarity='uncommon';renderPet(true);});
    await page.locator('[data-pet-filter="sort"]').selectOption('rarity');
    assert.equal(await page.locator('.pet-card').first().getAttribute('data-pet-select'),'1');
    assert.match(await page.locator('.pet-card').first().innerText(),/Hỏa · Thiên/);
    for(const width of [740,375]){
      await page.setViewportSize({width,height:680});
      assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'horizontal page overflow');
      assert.ok(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'vertical page overflow');
      if(width===740)assert.ok(await page.locator('.pet-detail').evaluate(e=>e.scrollHeight>e.clientHeight),'detail should scroll internally');
    }
    await page.getByRole('button',{name:'Thú điển',exact:true}).click();
    assert.equal(await page.locator('.pet-species').count(),12);
    assert.equal(await page.locator('.pet-forms .pet-art').count(),36);
    await page.evaluate(()=>{S.pet={...S.pet,hatch:[{slot:0,egg:'thu_dan_linh',readyInSeconds:1,instantCost:10}]};recvAt=performance.now();renderPet();});
    await page.getByRole('button',{name:'Ấp trứng',exact:true}).click();
    await page.waitForFunction(()=>document.querySelector('.pet-collect').disabled===false,{},{timeout:4000});
    assert.ok(await page.locator('#btnPet').evaluate(e=>e.classList.contains('ready')));
    await page.locator('.pet-collect').click();await page.waitForFunction(()=>!busy);
    assert.match(await page.evaluate(()=>calls.at(-1)),/hatch\/collect\?slot=0$/);
    // Check the real parent layout styles as well as the isolated component.
    for(const file of ['base.css','window.css','concept-shell.css'])await page.addStyleTag({path:path.join(root,'wwwroot/css',file)});
    await page.evaluate(()=>{
      document.documentElement.classList.add('panel');document.body.classList.add('panel');
      const pane=document.getElementById('pane-pet'),book=document.createElement('aside');book.className='book';
      book.innerHTML='<div class="game-brand"><b>Vấn Đạo</b></div><div class="tabs"><button class="tab">Linh thú</button></div>';
      document.body.append(book);book.append(pane);document.getElementById('btnPet').hidden=true;
    });
    for(const width of [740,375]){
      await page.setViewportSize({width,height:680});
      assert.ok(await page.evaluate(()=>document.querySelector('.pet-ui').getBoundingClientRect().bottom<=innerHeight),'parent panel fit');
      assert.ok(await page.evaluate(()=>document.querySelector('.pet-ui').getBoundingClientRect().height>200),'parent panel usable height');
    }
    assert.deepEqual(errors,[]);
    console.log('PASS: empty state, 12 species/36 forms, feed/slot/evolve routes, two-click release/cancel, error display, filters/focus, hatch countdown/collect, 740px and 375px overflow.');
  } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
