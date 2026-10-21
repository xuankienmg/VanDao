const fs=require('fs'),vm=require('vm'),path=require('path');
const {createCanvas,loadImage}=require('C:/Users/Lenovo/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
(async()=>{
 const id='hopthe_nu',images={};
 for(const suffix of ['actions','flight','idle']){const key=`img/skin/${id}_${suffix}.png`;images[key]=await loadImage(`wwwroot/${key}`);}
 const med=await loadImage(`wwwroot/img/med_${id}.png`);
 const c=vm.createContext({Math,skinArt:()=>`img/hero_${id}.png`,Image:class{}});
 vm.runInContext(fs.readFileSync('wwwroot/js/skill-preview.js','utf8').split('const FIRE =')[0],c);c.skImg=p=>images[p]||null;
 const inv=fs.readFileSync('wwwroot/js/inventory.js','utf8');
 vm.runInContext(inv.slice(inv.indexOf('function drawInventoryIdle('),inv.indexOf('function drawInventoryAvatar(')),c);
 const fx=fs.readFileSync('wwwroot/js/battle-fx.js','utf8');
 vm.runInContext(fx.slice(fx.indexOf('function drawAirFlightFx('),fx.indexOf('function hcBattleDraw(')),c);
 const meta=JSON.parse(fs.readFileSync(`wwwroot/img/skin/${id}_actions.json`,'utf8'));
 const out='docs/skin-demos/hopthe-nu';fs.mkdirSync(out,{recursive:true});
 for(let i=0;i<40;i++){
  const t=i/10,canvas=createCanvas(640,680),g=canvas.getContext('2d');
  g.fillStyle='#18222d';g.fillRect(0,0,640,680);g.fillStyle='#e0c58a';g.font='18px sans-serif';
  ['Stand','Meditate','Run','Hover'].forEach((label,j)=>g.fillText(label,j%2*320+14,Math.floor(j/2)*340+26));
  g.save();g.translate(0,20);c.drawInventoryIdle(g,images[`img/skin/${id}_idle.png`],{fw:480,fh:440,ax:240,ay:420,bodyHeight:meta.bodyHeight},t,false);g.restore();
  const q=Math.min(290/med.width,270/med.height);g.drawImage(med,480-med.width*q/2,326-med.height*q,med.width*q,med.height*q);
  g.save();g.translate(0,340);c.drawSkinActor(g,'groundRun',(t*7%4)/4,160,316,278);g.restore();
  const feet=604+Math.sin(t*3)*3;c.drawAirFlightFx(g,480,feet,278,t,false);c.drawSkinActor(g,'run',t%1,480,feet,278);
  fs.writeFileSync(path.join(out,`preview_${String(i).padStart(2,'0')}.png`),canvas.toBuffer('image/png'));
 }
 fs.copyFileSync(path.join(out,'preview_10.png'),path.join(out,'overview.png'));
 console.log('Rendered female Hợp Thể skin with real idle, run and hover drawing functions');
})();
