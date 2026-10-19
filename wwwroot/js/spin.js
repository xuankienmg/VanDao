/* Vấn Đạo · js/spin.js: Luyện chế → Tài Xỉu: đổi linh thạch lấy vé, quay vòng 10 phẩm trang bị (Phàm → Huyền Thiên). */
const SPIN_TICKET = 've_tai_xiu';
const SPIN_SEG = 36;                     // 10 ô × 36°
let spinBusy = false, spinRot = 0, spinLast = null, spinKey = '';
const spinTickets = () => S.inventory[SPIN_TICKET] || 0;
const spinPrice = () => DATA.spinPrice || 1e5;
const spinW = () => DATA.spinWeights || [35, 25, 16, 10, 6, 3.5, 2, 1.2, .6, .03];
const spinPct = w => `${w >= 1 ? +w.toFixed(1) : +w.toFixed(2)}%`.replace('.', ',');
const spinReduced = () => matchMedia('(prefers-reduced-motion: reduce)').matches;

/** Bộ trang bị vòng quay đang ra: cảnh giới cao nhất có đồ, không vượt cảnh giới hiện tại (giống GameData.SpinPool). */
function spinPoolRealm(){
  const lv = DATA.items.filter(i => i.base && i.slot).map(i => i.minRealm || 0).filter(r => r <= S.realm);
  return lv.length ? Math.max(...lv) : 0;
}
function spinPool(q){
  const r = spinPoolRealm();
  return DATA.items.filter(i => i.base && i.slot && (i.minRealm || 0) === r && i.rarity === FUSE_R[q]);
}
/** Món minh họa cho ô q: ưu tiên vũ khí. */
const spinIcon = q => { const p = spinPool(q); return (p.find(i => i.slot === 'weapon') || p[0])?.id; };

const spinPt = (r, a) => [r * Math.sin(a * Math.PI / 180), -r * Math.cos(a * Math.PI / 180)];
function spinDisc(){
  const R1 = 92, R0 = 26;
  const segs = FUSE_R.map((r, i) => {
    const a0 = -SPIN_SEG / 2, a1 = SPIN_SEG / 2;
    const [x0, y0] = spinPt(R1, a0), [x1, y1] = spinPt(R1, a1), [x2, y2] = spinPt(R0, a1), [x3, y3] = spinPt(R0, a0);
    const col = RARITY[r].c, k = spinIcon(i), nm = RARITY[r].n.split(' ');
    const name = nm.length > 1 ? `<text class="sp-nm" y="-83">${nm[0]}</text><text class="sp-nm" y="-75">${nm[1]}</text>` : `<text class="sp-nm" y="-79">${nm[0]}</text>`;
    return `<g class="sp-seg" data-q="${i}" transform="rotate(${i * SPIN_SEG})" style="--c:${col}">
      <path class="sp-bg ${i % 2 ? 'alt' : ''}" d="M${x0},${y0} A${R1},${R1} 0 0 1 ${x1},${y1} L${x2},${y2} A${R0},${R0} 0 0 0 ${x3},${y3} Z"/>
      <path class="sp-rim" d="M${x0},${y0} A${R1},${R1} 0 0 1 ${x1},${y1}"/>
      ${name}
      ${k ? `<foreignObject x="-13" y="-70" width="26" height="26"><div xmlns="http://www.w3.org/1999/xhtml" class="sp-ic">${itemImg(k)}</div></foreignObject>` : ''}
      <text class="sp-no" y="-33">${i + 1}</text></g>`;
  }).join('');
  const dots = Array.from({length: 20}, (_, i) => { const [x, y] = spinPt(96, i * 18 + 9); return `<circle class="sp-dot ${i % 2 ? 'b' : ''}" cx="${x}" cy="${y}" r="1.7"/>`; }).join('');
  return `<svg class="sp-svg" viewBox="-100 -100 200 200" aria-hidden="true">
    <circle class="sp-ring" r="99"/><g class="sp-disc" id="spDisc" style="transform:rotate(${spinRot}deg)">${segs}</g>${dots}</svg>`;
}

function renderSpin(){
  const pane = $('pane-spin'); if (!pane || !S) return;
  if (!$('spWheel')){
    pane.innerHTML = `<div class="dd sp">
      <header class="dd-top">${lcNav('spin')}<div class="dd-cult" id="spCult"></div>
        <div class="dd-stone">${itemImg('linh_thach')}<span>Linh thạch</span><b id="spStone"></b></div>
        <div class="dd-stone sp-tk" data-tip="Vé Tài Xỉu&#10;Mỗi vé quay 1 lần">${itemImg(SPIN_TICKET)}<span>Vé</span><b id="spTk"></b></div></header>
      <section class="dd-main sp-main" aria-label="Vòng quay">
        <div class="sp-stage">
          <div class="sp-wheel" id="spWheel"></div>
          <i class="sp-ptr" aria-hidden="true"></i>
          <button class="sp-hub" data-sp-go="1" id="spHub" aria-label="Quay 1 lượt">Quay</button>
        </div>
        <div class="dd-act sp-act" id="spAct"></div>
        <div class="sp-res" id="spRes" aria-live="polite"></div>
      </section>
      <aside class="dd-side sp-side">
        <div class="sp-buy" id="spBuy"></div>
        <div class="sp-odds"><p class="dd-sec">Tỉ lệ 10 ô</p><ol id="spOdds"></ol></div>
      </aside>
    </div>`;
  }
  const key = `${spinPoolRealm()}`;
  if (key !== spinKey && !spinBusy){ spinKey = key; $('spWheel').innerHTML = spinDisc(); }
  const tk = spinTickets(), st = S.inventory.linh_thach || 0, price = spinPrice(), canBuy = Math.floor(st / price);
  const set = ITEM[spinIcon(0)]?.set;
  $('spCult').innerHTML = `<span>Mỗi <b>${fmt(price)}</b> linh thạch đổi <b>1 vé</b> · mỗi vé quay 1 lần nhận 1 trang bị theo cảnh giới <b>${S.realmName}</b>${set ? ` (bộ <b>${set}</b>)` : ''} · càng lên cao càng hiếm</span>`;
  $('spStone').textContent = `× ${fmt(st)}`;
  $('spTk').textContent = `× ${fmt(tk)}`;
  const off = spinBusy || busy;
  $('spHub').disabled = off || tk < 1;
  $('spWheel').classList.toggle('spinning', spinBusy);
  $('spAct').innerHTML = `<button class="dd-synth-btn" data-sp-go="1" ${off || tk < 1 ? 'disabled' : ''}>Quay ×1</button>
    <button class="dd-all" data-sp-go="10" ${off || tk < 10 ? 'disabled' : ''} data-tip="Quay 10 lượt một lần&#10;Cần 10 vé và 10 ô trống trong hành trang">Quay ×10</button>
    <p class="dd-fee">${tk ? `Còn ${fmt(tk)} vé` : '<span class="miss">Chưa có vé</span> · đổi vé ở cột bên phải'} · trang bị nhận vào hành trang</p>`;
  const btn = (n, lab) => `<button class="dd-all" data-sp-buy="${n}" ${off || canBuy < Math.max(1, n) ? 'disabled' : ''}>${lab}</button>`;
  $('spBuy').innerHTML = `<p class="dd-sec">Đổi vé quay</p>
    <div class="sp-rate">${itemImg('linh_thach')}<b>${fmt(price)}</b><i>→</i>${itemImg(SPIN_TICKET)}<b>1 vé</b></div>
    <div class="sp-btns">${btn(1, 'Đổi 1')}${btn(10, 'Đổi 10')}${btn(0, `Tối đa${canBuy > 0 ? ` ×${fmt(canBuy)}` : ''}`)}</div>
    <p class="sp-note">${canBuy > 0 ? `Đủ linh thạch đổi ${fmt(canBuy)} vé.` : `Cần thêm <b>${fmt(price - st % price)}</b> linh thạch cho vé kế tiếp.`}</p>`;
  const w = spinW();
  $('spOdds').innerHTML = FUSE_R.map((r, i) => `<li class="${spinLast?.q === i ? 'on' : ''}"><span class="sp-n" style="--c:${RARITY[r].c}">${i + 1}</span>${qWord(r)}<b>${spinPct(w[i])}</b></li>`).join('');
  $('spRes').innerHTML = spinBusy ? '<p class="sp-wait">Vòng quay đang chuyển…</p>' : spinLast ? spinResHtml(spinLast) : '<p class="sp-wait">Bấm <b>Quay</b> để thử vận may.</p>';
}

function spinResHtml(r){
  const list = r.outs.slice().sort((a, b) => FUSE_R.indexOf(ITEM[b.item]?.rarity) - FUSE_R.indexOf(ITEM[a.item]?.rarity));
  const big = r.q >= 4;
  return `<p class="dd-sec">${big ? 'Đại vận!' : 'Kết quả'} · ô ${r.q + 1} ${qWord(FUSE_R[r.q])}</p><div class="sp-got ${big ? 'big' : ''}">${list.map(o => {
    const it = ITEM[o.item]; if (!it) return '';
    return `<span class="sp-item r-${it.rarity}" data-tip="${itemTip(o.item)}">${itemImg(o.item)}${o.qty > 1 ? `<em>×${o.qty}</em>` : ''}<b class="rc-${it.rarity}">${it.name}</b></span>`;
  }).join('')}</div>`;
}

async function spinGo(n){
  if (busy || spinBusy || spinTickets() < n) return;
  spinBusy = true; busy = true; spinLast = null; renderSpin();
  let res;
  try { res = await api('POST', `/api/players/${S.id}/spin?n=${n}`); }
  catch(e){ spinBusy = false; busy = false; localLog(e.message, 'bad'); renderSpin(); return; }
  const q = Math.max(0, +(res.craft?.recipe || 'spin:0').split(':')[1] || 0);
  const disc = $('spDisc');
  disc?.querySelectorAll('.sp-seg.on').forEach(x => x.classList.remove('on'));
  // ô q dừng dưới kim (đỉnh vòng): góc đích ≡ −q·36°, lệch ngẫu nhiên trong ô cho tự nhiên
  const jitter = (Math.random() - .5) * SPIN_SEG * .7;
  const target = ((-q * SPIN_SEG + jitter - spinRot) % 360 + 360) % 360;
  const fast = spinReduced();
  spinRot += (fast ? 0 : 360 * 6) + target;
  if (disc){ disc.style.transition = fast ? 'none' : ''; disc.style.transform = `rotate(${spinRot}deg)`; }
  setTimeout(() => {
    spinBusy = false; busy = false;
    spinLast = {q, outs: res.craft?.outputs?.length ? res.craft.outputs : [res.craft.output]};
    setState(res.state); notifyHost('changed');
    $('spDisc')?.querySelector(`.sp-seg[data-q="${q}"]`)?.classList.add('on');
    if (q >= 4 && typeof rings !== 'undefined') rings.push({t: 0, c: RARITY[FUSE_R[q]].c, big: q >= 6});
    renderSpin(); live();
  }, fast ? 250 : 4400);
}

$('pane-spin').addEventListener('click', async e => {
  const b = e.target.closest('button'); if (!b || b.disabled || spinBusy) return;
  if (b.dataset.spGo){ spinGo(+b.dataset.spGo); return; }
  if (b.dataset.spBuy !== undefined){ await act(`spin/buy?n=${b.dataset.spBuy}`); renderSpin(); }
});

/** Code quà tặng (gọi từ menu Cài đặt của cửa sổ desktop, hoặc nút cài đặt khi chơi trên trình duyệt). */
window.redeemCode = async code => {
  if (!S) return;
  const el = $('craftToast');
  const toast = (ok, txt) => {
    if (!el) return;
    el.classList.remove('brief', 'fast');
    el.innerHTML = `<span class="ct-ic">${ok ? itemImg(SPIN_TICKET) : ''}</span><span class="ct-t ${ok ? '' : 'bad'}">${ok ? 'Nhận quà' : 'Không nhận được'}</span><span class="ct-o">${txt}</span>`;
    el.hidden = true; void el.offsetWidth; el.hidden = false;
    clearTimeout(craftTimer); craftTimer = setTimeout(() => { el.hidden = true; }, 2600);
  };
  try {
    const res = await api('POST', `/api/players/${S.id}/redeem?code=${encodeURIComponent(code)}`);
    setState(res.state); notifyHost('changed');
    toast(true, res.message || 'Đã nhận quà.');
  } catch(e){ localLog(e.message, 'bad'); toast(false, e.message); }
};
