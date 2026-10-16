/* Vấn Đạo · js/stage.js: canvas chính, cảnh ngồi thiền. Tách từ index.html (tools/split_index.py). */
/* ============================================================
   CANVAS — TU SĨ NGỒI XẾP BẰNG (2D)
   ============================================================ */
const cv = document.getElementById('stage'), ctx = cv.getContext('2d');
let W = 0, H = 0;
const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
function resize(){
  const r = cv.getBoundingClientRect(), d = Math.min(2, devicePixelRatio || 1);
  W = r.width; H = r.height; cv.width = W*d; cv.height = H*d; ctx.setTransform(d,0,0,d,0,0);
}
const stars = Array.from({length:46}, () => ({x:Math.random(), y:Math.random()*.5, r:Math.random()*1.2+.3, p:Math.random()*6}));
const hills = [
  {base:.60, amp:.07, f1:5.1, f2:13, p:1.3, col:'#16222a'},
  {base:.70, amp:.06, f1:3.4, f2:9,  p:4.1, col:'#121b20'},
  {base:.82, amp:.04, f1:2.2, f2:7,  p:2.2, col:'#0d1417'},
];
let parts = [], rings = [], floats = [];
function burst(n){ for (let i=0;i<n;i++) spawn(true); }
function spawn(fast){
  parts.push({a:Math.random()*Math.PI*2, r:170 + Math.random()*110, s:(fast?140:60) + Math.random()*50, w:(Math.random()<.5?-1:1)*(.6+Math.random()*.8), z:1+Math.random()*1.6});
}
function hexA(h,a){ const n=parseInt(h.slice(1),16); return `rgba(${n>>16&255},${n>>8&255},${n&255},${a})`; }

// ===== Cảnh: trận đánh pixel khi thám hiểm; cảnh ngồi thiền chỉ hiện tạm lúc ảnh chưa tải xong =====
function drawScene(t, dt){ drawSceneInner(t, dt); }

let scrollT = 0;
function drawSceneInner(t, dt){
  const r = realm();
  const fight = S.battle && sprite48(realm().hero) && sprite48(S.battle.image);
  const ph = fight ? battlePhase() : null;
  if (ph && ph.name === 'walk' && !reduce) scrollT += dt;
  if (fight && drawPixelBattle(t, dt, ph, r)) return;
  // trời
  const g = ctx.createLinearGradient(0,0,0,H); g.addColorStop(0,'#0b1216'); g.addColorStop(1,'#1a2529');
  ctx.fillStyle = g; ctx.fillRect(0,0,W,H);
  for (const s of stars){ ctx.fillStyle = `rgba(235,228,211,${.25+.35*Math.abs(Math.sin(t*.6+s.p))})`; ctx.beginPath(); ctx.arc(s.x*W, s.y*H, s.r, 0, 7); ctx.fill(); }
  // trăng
  const mx = W*.82, my = H*.2, mr = Math.min(W,H)*.06;
  const mg = ctx.createRadialGradient(mx,my,mr*.5,mx,my,mr*4); mg.addColorStop(0,'rgba(235,228,211,.18)'); mg.addColorStop(1,'rgba(235,228,211,0)');
  ctx.fillStyle = mg; ctx.fillRect(mx-mr*4,my-mr*4,mr*8,mr*8);
  ctx.fillStyle = '#e9e1cc'; ctx.beginPath(); ctx.arc(mx,my,mr,0,7); ctx.fill();
  ctx.fillStyle = '#0b1216'; ctx.globalAlpha=.12; ctx.beginPath(); ctx.arc(mx-mr*.3,my+mr*.15,mr*.25,0,7); ctx.arc(mx+mr*.3,my-mr*.3,mr*.15,0,7); ctx.fill(); ctx.globalAlpha=1;
  // núi (trôi khi nhân vật đang đi đường)
  hills.forEach((h,i) => {
    const off = -scrollT * (0.02 + i*0.03);
    ctx.fillStyle = h.col; ctx.beginPath(); ctx.moveTo(0,H);
    for (let x=0;x<=W+8;x+=8){ const u=x/W - off; ctx.lineTo(x, H*(h.base - h.amp*(Math.sin(u*h.f1+h.p)+.45*Math.sin(u*h.f2+h.p*2)))); }
    ctx.lineTo(W,H); ctx.fill();
    const my2 = H*(h.base+.02), mist = ctx.createLinearGradient(0,my2-20,0,my2+30);
    mist.addColorStop(0,'rgba(180,200,200,0)'); mist.addColorStop(.5,`rgba(180,200,200,${.05+.02*Math.sin(t*.3+i)})`); mist.addColorStop(1,'rgba(180,200,200,0)');
    ctx.fillStyle = mist; ctx.fillRect(0,my2-20,W,50);
  });


  // ---- dự phòng khi sprite chưa tải: ngồi thiền trên đài sen ----
  const u = Math.min(W/460, H/400), cx = W/2, cy = H*.6;
  ctx.save(); ctx.translate(cx,cy); ctx.scale(u,u);
  const breath = reduce ? 0 : Math.sin(t*1.1);
  const ag = ctx.createRadialGradient(0,-50,10,0,-50,210+6*breath);
  ag.addColorStop(0,hexA(r.aura,.38)); ag.addColorStop(.55,hexA(r.aura,.12)); ag.addColorStop(1,hexA(r.aura,0));
  ctx.fillStyle = ag; ctx.beginPath(); ctx.arc(0,-50,220,0,7); ctx.fill();
  drawMeditation(r, t, dt, breath);
  rings = rings.filter(g => g.t < (g.big?2.2:1.2));
  for (const g of rings){
    g.t += dt; const k = g.t/(g.big?2.2:1.2);
    ctx.strokeStyle = hexA(g.c, 1-k); ctx.lineWidth = g.big ? 6*(1-k)+1 : 3*(1-k)+1;
    ctx.beginPath(); ctx.arc(0,-40, 30 + k*(g.big?320:200), 0, 7); ctx.stroke();
  }
  floats = floats.filter(f => f.t < 1.1);
  ctx.font = '600 15px "Be Vietnam Pro", system-ui, sans-serif'; ctx.textAlign = 'center';
  for (const f of floats){ f.t += dt; ctx.fillStyle = hexA(r.aura, 1-f.t/1.1); ctx.fillText(f.text, f.x, f.y - f.t*40); }
  ctx.restore();
}
