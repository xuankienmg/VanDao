const fs=require('fs'),vm=require('vm'),path=require('path');
const {createCanvas,loadImage}=require('C:/Users/Lenovo/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
(async()=>{
 const source=fs.readFileSync('wwwroot/js/inventory.js','utf8');
 const c=vm.createContext({Math});
 vm.runInContext(source.slice(source.indexOf('function drawInventoryIdle('),source.indexOf('function drawInventoryAvatar(')),c);
 const specs=[['luyenkhi','Thanh Sam',384,320,192,308,210],['trucco','Trúc Cơ',480,440,240,420,336],['kimdan','Kim Đan',480,440,240,420,305],['luyenhu','Luyện Hư',480,440,240,420,360]];
 const images=await Promise.all(specs.map(s=>loadImage(`wwwroot/img/skin/${s[0]==='luyenkhi'?'luyenkhi_inventory_breeze.png':s[0]+'_actions.png'}`)));
 const out='docs/skin-demos/inventory-smooth';fs.mkdirSync(out,{recursive:true});
 for(let f=0;f<72;f++){
  const canvas=createCanvas(640,680),g=canvas.getContext('2d');
  g.fillStyle='#1c2125';g.fillRect(0,0,640,680);
  specs.forEach((s,i)=>{
   g.save();g.translate(i%2*320,Math.floor(i/2)*340);
   g.fillStyle='#d8c9a3';g.font='15px sans-serif';g.fillText(s[1],12,20);
   g.translate(0,20);c.drawInventoryIdle(g,images[i],{fw:s[2],fh:s[3],ax:s[4],ay:s[5],bodyHeight:s[6]},f/10,false);g.restore();
  });
  fs.writeFileSync(path.join(out,`frame_${String(f).padStart(2,'0')}.png`),canvas.toBuffer('image/png'));
 }
 console.log('Rendered 72 frames from the actual inventory idle function');
})();
