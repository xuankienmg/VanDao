// Local responsive preview with synthetic inventory; never calls the game API.
const http = require('node:http'), fs = require('node:fs'), path = require('node:path');
const root = path.resolve(__dirname, '../..');
const fixture = {
  DATA: { realms: ['Luyện Khí','Trúc Cơ','Kim Đan','Nguyên Anh','Hóa Thần','Luyện Hư'].map(name => ({name})), equipmentSets: [] },
  ITEM: {}, S: {realm:5,inventory:{linh_thach:12057996,thuong_pham_linh_thach:0},equipment:{},gearEffects:{sets:[],effects:[]}}
};
for (const [i, rarity] of ['common','uncommon','rare','epic','legendary','mythic','divine','immortal','dao','supreme'].entries()) {
  const id = 'gear' + i;
  fixture.ITEM[id] = {id,name:'Trang bị ' + rarity,rarity,slot:'weapon',type:'equip',kind:'Kiếm',sell:3*4**i,itemLevel:10,icon:'img/lk_kiem.png'};
  fixture.S.inventory[id] = 2;
}
fixture.ITEM.pill = {id:'pill',name:'Đan dược tu luyện cao cấp tên dài',rarity:'rare',type:'pill',sell:600,tier:3,icon:'img/linh_thach.png'};
fixture.ITEM.linh_thach = {id:'linh_thach',name:'Linh thạch',rarity:'common',type:'material',sell:0,icon:'img/linh_thach.png'};
fixture.ITEM.thuong_pham_linh_thach = {...fixture.ITEM.linh_thach,id:'thuong_pham_linh_thach'};
fixture.S.inventory.pill = 1234567;
const wrapper = `<!doctype html><meta charset="utf-8"><title>Sell responsive QA</title><style>body{margin:0;background:#202328;color:white;font:14px system-ui}header{padding:10px;display:flex;gap:8px;flex-wrap:wrap}button{padding:8px}iframe{display:block;border:0;height:760px;max-width:100%}#result{padding:8px}</style><header>${[320,420,600,724,900,1280].map(w=>`<button onclick="frame.style.width='${w}px';frame.src='/preview?width=${w}'">${w}px</button>`).join('')}<button onclick="frame.contentWindow.postMessage('full','*')">Đầy quầy + đan</button><button onclick="frame.contentWindow.postMessage('large','*')">Số dư dài</button></header><div id="result">Loading</div><iframe id="frame" style="width:900px" src="/preview?width=900"></iframe><script>const frame=document.getElementById('frame');addEventListener('message',e=>{if(e.source===frame.contentWindow)document.getElementById('result').textContent=JSON.stringify(e.data)})</script>`;
const preview = `<!doctype html><html lang="vi" class="panel"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><link rel="stylesheet" href="/css/base.css"><link rel="stylesheet" href="/css/window.css"><link rel="stylesheet" href="/css/inventory.css"><link rel="stylesheet" href="/css/craft.css"><body class="panel"><div class="wrap"><main class="grid"><aside class="book"><div class="tabs">${['Đột phá','Hành trang','Luyện chế','Bản đồ','Công pháp'].map(n=>`<button class="tab">${n}</button>`).join('')}</div><div id="pane-sell" class="pane active"></div></aside></main></div><script>
const {DATA,ITEM,S}=${JSON.stringify(fixture)};let busy=false,adminOn=false;
const $=id=>document.getElementById(id),fmt=n=>Number(n).toLocaleString('vi-VN'),hideTip=()=>{},openTab=()=>{};
const act=async()=>{throw new Error('Fixture must not call sale API')};
</script><div id="pane-inv" hidden></div><div id="pane-fuse" hidden></div><script src="/js/inventory.js"></script><script src="/js/fuse.js"></script><script src="/js/sell.js"></script><script>
function inspect(){
 const pane=$('pane-sell'),header=pane.querySelector('.dd-top'),hint=pane.querySelector('.dd-cult');
 const clipped=[...pane.querySelectorAll('.sl-q,.fz-act button,.lc-tabs button')].filter(e=>e.scrollWidth>e.clientWidth+1||e.scrollHeight>e.clientHeight+1).map(e=>e.textContent.trim());
 const overflow=[...pane.querySelectorAll('*')].filter(e=>e.getBoundingClientRect().right>innerWidth+1).map(e=>e.className).slice(0,8);
 const bag=pane.querySelector('.fz-bag').getBoundingClientRect(),desk=pane.querySelector('.sl-desk').getBoundingClientRect();
 parent.postMessage({width:innerWidth,horizontalOverflow:document.documentElement.scrollWidth>innerWidth,clipped,overflow,hintWidth:Math.round(hint.getBoundingClientRect().width),headerHeight:Math.round(header.getBoundingClientRect().height),stacked:desk.top>=bag.bottom},'*');
}
renderSell();requestAnimationFrame(inspect);addEventListener('resize',()=>requestAnimationFrame(inspect));
addEventListener('message',e=>{if(e.data==='full'){sellSel=Object.keys(ITEM).filter(k=>ITEM[k].slot).slice(0,8).map(k=>({k,n:1})).concat({k:'pill',n:1234567});renderSell()}if(e.data==='large'){S.inventory.linh_thach=1234567890123456;renderSell()}requestAnimationFrame(inspect)});
new MutationObserver(()=>requestAnimationFrame(inspect)).observe($('pane-sell'),{childList:true,subtree:true});
</script></body></html>`;
http.createServer((req,res)=>{
  const url = new URL(req.url,'http://localhost');
  if(url.pathname==='/breakthrough') {res.setHeader('Content-Type','text/html; charset=utf-8');res.end(fs.readFileSync(path.join(root,'tools/breakthrough_checks/preview.html')));return;}
  if(url.pathname==='/'||url.pathname==='/preview'){res.setHeader('Content-Type','text/html; charset=utf-8');res.end(url.pathname==='/'?wrapper:preview.replace('<body class="panel">','<link rel="stylesheet" href="/css/menu-background.css"><body class="panel">'));return;}
  const file=path.resolve(root,'wwwroot','.'+decodeURIComponent(url.pathname));
  if(!file.startsWith(path.join(root,'wwwroot')+path.sep)){res.writeHead(403);res.end();return;}
  const types={'.css':'text/css','.js':'text/javascript','.png':'image/png','.webp':'image/webp'};
  if(types[path.extname(file)])res.setHeader('Content-Type',types[path.extname(file)]);
  fs.readFile(file,(err,data)=>{res.statusCode=err?404:200;res.end(err?'Not found':data)});
}).listen(4175,'127.0.0.1',()=>console.log('Sell QA: http://127.0.0.1:4175'));
