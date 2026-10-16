/* Vấn Đạo · js/sell.js: Luyện chế · Bán — chọn món trong hành trang (cột trái) bỏ vào quầy bán (cột phải), bán một lần. */
let sellSel = [];          // id các món đã bỏ vào quầy (trang bị: mỗi phần tử 1 món)
let sellConfirm = 0;
const SELL_N = 9;            // số ô của quầy bán       // bấm Bán lần 1 khi có đồ từ Huyền cấp trở lên: chờ bấm lần 2 trong 3 giây
const SELL_GO = '<svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="8"/><path d="M12 7v10M9.5 9.5h3.8a1.7 1.7 0 0 1 0 3.4h-2.6a1.7 1.7 0 0 0 0 3.4H14.5"/></svg>';
function sellClean(){                                   // bỏ món trong quầy mà túi không còn (vừa mặc, ghép, bán…)
  const left = {...S.inventory};
  sellSel = sellSel.filter(k => ITEM[k]?.sell && left[k]-- > 0);
}
/** Các món bán được còn chưa bỏ vào quầy, theo thứ tự cột trái. */
function sellFree(){
  const left = {...S.inventory}; sellSel.forEach(k => left[k]--);
  return lcBagList().filter(c => ITEM[c.k].sell && left[c.k]-- > 0).map(c => c.k);
}
function renderSell(){
  const pane = $('pane-sell'); if (!pane || !S) return;
  sellClean();
  const bag = lcBagList(), used = {}, picked = {};
  sellSel.forEach(k => picked[k] = (picked[k] || 0) + 1);
  const cells = bag.map(({k, n}) => {
    const it = ITEM[k];
    if (!it.sell) return `<div class="iv-cell ${it.slot ? 'eq ' : ''}r-${it.rarity || 'common'} off na" aria-label="${it.name}" data-tip="${itemTip(k, 'Không bán được')}">${itemImg(k)}${lcCellTail(k, n)}</div>`;
    const j = used[k] = (used[k] || 0) + 1, on = j <= (picked[k] || 0), off = !on && sellSel.length >= SELL_N;
    return `<button class="iv-cell eq r-${it.rarity} ${on ? 'msel' : ''} ${off ? 'off' : ''}" data-sl="${k}" aria-label="${it.name}" data-tip="${itemTip(k, on ? 'Nhấp để lấy khỏi quầy' : off ? `Quầy đã đủ ${SELL_N} món: bán trước hoặc lấy bớt ra` : `Nhấp để bỏ vào quầy · ${fmt(it.sell)} linh thạch`)}">${itemImg(k)}<span class="lv">${itemLv(it)}</span></button>`;
  }).join('');
  const gain = sellSel.reduce((a, k) => a + ITEM[k].sell, 0), hi = sellSel.some(k => fuseQOf(k) >= 2);
  const armed = sellConfirm && Date.now() - sellConfirm < 3000, full = sellSel.length >= SELL_N;
  const sellable = bag.filter(c => ITEM[c.k].sell).length;
  const slots = Array.from({length: SELL_N}, (_, i) => { const k = sellSel[i];
    return k ? `<button class="iv-cell eq r-${ITEM[k].rarity}" data-ss="${i}" aria-label="${ITEM[k].name}" data-tip="${itemTip(k, 'Nhấp để lấy ra')}">${itemImg(k)}<span class="lv">${itemLv(ITEM[k])}</span></button>`
             : '<div class="iv-cell empty"></div>'; }).join('');
  pane.innerHTML = `<div class="dd fz sl">
      <header class="dd-top">${lcNav('sell')}<div class="dd-cult"><span>Bỏ tối đa <b>${SELL_N} món</b> vào quầy rồi bán một lần · đồ đang mặc không tính</span></div>
        <div class="dd-stone" data-tip="Linh thạch hiện có"><span>Linh thạch</span><b>${fmt(S.inventory.linh_thach || 0)}</b></div></header>
      <section class="dd-main fz-bag" aria-label="Hành trang"><p class="dd-sec">Hành trang · trang bị phẩm thấp → cao, rồi vật phẩm khác</p>
        <div class="fz-bar"><span>Bán được ${sellable} món · đã chọn ${sellSel.length}/${SELL_N}</span></div>
        <div class="iv-grid fz-grid">${cells || '<p class="fz-empty">Hành trang trống.</p>'}</div>
      </section>
      <aside class="fz-forge sl-desk" aria-label="Quầy bán"><p class="dd-sec">Quầy bán</p>
        <div class="fz-slots">${slots}</div>
        <div class="fz-arrow">↓</div>
        <div class="sl-sum"><span>${sellSel.length} món</span><b>+${fmt(gain)}</b>${itemImg('linh_thach')}</div>
        ${hi ? `<p class="sl-warn">Có đồ từ ${qWord('rare')} trở lên · bấm Bán 2 lần để xác nhận</p>` : ''}
        <div class="fz-act">
          <div class="fz-auto"><button class="fz-af" data-sz="auto" ${!full && sellFree().length ? '' : 'disabled'} data-tip="Tự động điền&#10;Bù đủ ${SELL_N} ô bằng món phẩm thấp nhất, bán rẻ nhất trước">${FUSE_GRID}<span>Tự động điền</span></button></div>
          <button class="dd-synth-btn fz-go ${armed ? 'sl-arm' : ''}" data-sz="go" ${sellSel.length && !busy ? '' : 'disabled'}>${SELL_GO}${armed ? 'Bấm lần nữa' : 'Bán'}</button>
        </div>
      </aside>
    </div>`;
}
$('pane-sell').addEventListener('click', async e => {
  const b = e.target.closest('button'); if (!b || b.disabled) return;
  hideTip();
  if (b.dataset.sl){
    const k = b.dataset.sl, all = [...$('pane-sell').querySelectorAll(`[data-sl="${CSS.escape(k)}"]`)], j = all.indexOf(b) + 1;
    if (j <= sellSel.filter(x => x === k).length) sellSel.splice(sellSel.lastIndexOf(k), 1); else if (sellSel.length < SELL_N) sellSel.push(k);
    sellConfirm = 0; renderSell(); return;
  }
  if (b.dataset.ss){ sellSel.splice(+b.dataset.ss, 1); sellConfirm = 0; renderSell(); return; }
  const a = b.dataset.sz;
  if (a === 'auto'){                                       // phẩm thấp nhất trước, trong cùng phẩm món bán rẻ trước
    const pick = sellFree().sort((x, y) => fuseQOf(x) - fuseQOf(y) || ITEM[x].sell - ITEM[y].sell);
    sellSel = sellSel.concat(pick.slice(0, SELL_N - sellSel.length)); sellConfirm = 0; renderSell(); return;
  }
  if (a === 'go' && sellSel.length){
    if (sellSel.some(k => fuseQOf(k) >= 2) && !(sellConfirm && Date.now() - sellConfirm < 3000)){
      sellConfirm = Date.now(); renderSell();
      setTimeout(() => { if (sellConfirm && Date.now() - sellConfirm >= 3000){ sellConfirm = 0; renderSell(); } }, 3100);
      return;
    }
    const pool = {}; sellSel.forEach(k => pool[k] = (pool[k] || 0) + 1);
    sellSel = []; sellConfirm = 0;
    await act('sell-bulk?max=9&pick=true&items=' + encodeURIComponent(Object.entries(pool).map(([k, c]) => k + ':' + c).join(',')));
    renderSell();
  }
});
