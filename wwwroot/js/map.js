/* Vấn Đạo · js/map.js: tab Bản đồ. Tách từ index.html (tools/split_index.py). */
// =====================================================================
// BẢN ĐỒ: 10 chương × (15 ải + ải Boss). S.map.realm = chương đang đánh (0-based); thắng Boss chương cao nhất mở chương kế
// =====================================================================
function renderMap(){
  const m = S.map, c = DATA.chapters[m.realm], R = m.realm + 1, keyN = S.inventory[m.keyItem] || 0, back = m.realm < m.chapterMax;
  const N = DATA.areas, B = N + 1, inBoss = m.area > N;
  const bossOpen = m.areaMax >= N, canBoss = bossOpen && (keyN > 0 || adminBoss());
  // thanh chọn chương 1–10: đã qua ✓ · đang mở sáng · chưa mở / tạm khóa có ổ khóa
  const chapterBar = `<nav class="mpx-chs" aria-label="Chọn chương">${DATA.chapters.map(x => {
    const open = x.enabled && x.index <= m.chapterMax, done = x.index < m.chapterMax, cur = x.index === m.realm;
    const tip = `Chương ${x.index + 1} · ${x.name}&#10;Boss: ${open ? x.boss + ' · ' + x.bossLair : '???'}`
      + (!x.enabled ? '&#10;Tạm khóa, sẽ mở ở bản cập nhật sau' : !open ? `&#10;Đánh bại Boss chương ${x.index} để mở` : done ? '&#10;Đã qua: mọi ải đã mở' : '');
    return `<button class="${cur ? 'on' : ''}${done ? ' done' : ''}${open ? '' : ' lk'}" data-mch="${x.index}" aria-pressed="${cur}" ${open ? '' : 'aria-disabled="true"'} data-tip="${tip}">`
      + `<b>${x.index + 1}</b>${open ? (done ? '<i>✓</i>' : '') : `<span class="lkc">${LOCK_SVG}</span>`}</button>`; }).join('')}</nav>`;
  const pass = a => { const st = mapStateOf(a);
    return mapFilter === 'all' || (mapFilter === 'open' && st !== 'locked') || (mapFilter === 'win' && st === 'win')
      || (mapFilter === 'lose' && st === 'lose') || (mapFilter === 'boss' && a.index === B); };
  const FILTERS = [['all', 'Tất cả'], ['open', 'Đã mở'], ['win', 'Vượt qua'], ['lose', 'Nguy hiểm'], ['boss', 'Boss']];

  // ---- đường ải ngang, xếp zigzag 2 tầng cho vừa 16 ải không cần cuộn (tọa độ %, co theo khung) ----
  const pts = m.areas.map(a => mapPos(a.index));
  const seg = (from, to) => pts.slice(from, to).map((p, k) => `${k ? 'L' : 'M'}${p.x} ${p.y}`).join(' ');
  const reach = Math.min(m.areaMax + (bossOpen ? 1 : 0), B);                   // đoạn đã đi qua
  const hero = realm().hero.replace('.png', '_48.png');
  const fr = mapFrontier();
  const node = a => {
    const st = mapStateOf(a), p = mapPos(a.index), boss = a.index === B;
    const go = st !== 'locked' && st !== 'cur' && (!boss || canBoss);
    const why = st === 'locked' ? (boss ? `Mở ải ${R}-${N} trước` : `Thắng ải ${R}-${a.index - 1} để mở`) : boss && !canBoss && st !== 'cur' ? `Cần ${ITEM[m.keyItem].name}` : boss ? `Boss ${m.boss} · mỗi lần khiêu chiến tốn 1 ${ITEM[m.keyItem].name}` : '';
    const lab = boss ? 'BOSS' : `${a.elite ? '❖ ' : ''}${R}-${a.index}${st === 'win' ? ' ✓' : st === 'lose' ? ' nguy hiểm' : ''}`;   // ❖ = có quái tinh anh
    const isTop = !boss && a.index === fr.top;                          // ⚑ ải cao nhất đã mở
    return `<button class="mpx-n ${st}${boss ? ' bs' : ''}${a.elite ? ' el' : ''}${isTop ? ' top' + (fr.knocked ? ' knock' : '') : ''}" style="left:${p.x}%;top:${p.y}%" data-mx="${a.index}" ${go ? '' : 'aria-disabled="true"'}
      aria-label="Ải ${R}-${a.index}${boss ? ' Boss' : ''}" ${why ? `data-tip="${why}"` : ''}>
      <span class="o">${boss ? `<img src="${m.bossImage.replace('.png', '_48.png')}" alt="">` : st === 'locked' ? LOCK_SVG : st === 'cur' ? `<img class="me" src="${hero}" alt="">` : ''}</span>
      ${st === 'cur' ? '<span class="now">đang đánh</span>' : ''}${isTop && st !== 'cur' ? `<span class="fl" data-tip="Ải cao nhất đã mở${fr.knocked ? '&#10;Thua ở đây nên bị lùi về ' + R + '-' + m.area : ''}">⚑</span>` : ''}<span class="lb">${lab}</span></button>`;
  };
  // vạch ngăn giữa các vùng (nét đứt), đặt giữa ải cuối vùng trước và ải đầu vùng sau
  const seps = m.areas.filter((a, i) => i > 0 && a.index <= N && m.areas[i - 1].place !== a.place).map(a => (mapPos(a.index - 1).x + mapPos(a.index).x) / 2);
  const note = `${back ? `Chương ${R} đã qua, mọi ải đã mở. ` : 'Thắng ải cao nhất sẽ mở ải kế tiếp, thua thì lùi một ải. Hạ Boss chương để mở chương kế. '}${ITEM[m.keyItem].name} rơi ở ải ${R}-1 đến ${R}-${N}, mỗi lần khiêu chiến Boss tốn 1 tấm.&#10;Bấm vào ải để xuất chinh; rê chuột để xem thông tin.`;
  const old = document.querySelector('#pane-map .mpx-track'), keepX = old && mapScrollRealm === m.realm ? old.scrollLeft : null;
  $('pane-map').innerHTML = `<div class="mpx">
    ${chapterBar}
    <header class="mp-hd mpx-hd"><div><b>Chương ${R} · ${c.name} <small class="mp-back">${back ? 'đã qua' : 'gợi ý ' + c.realm}</small> <i class="mpx-q" data-tip="${note}">?</i></b>
      <span>${inBoss ? `Đang khiêu chiến Boss · ${m.bossLair}` : `Đang đánh ải ${R}-${m.area} · ${m.areas[m.area - 1].place}`}${!inBoss && m.area < fr.top ? ` · <em class="mpx-top ${fr.knocked ? 'down' : ''}">⚑ Ải cao nhất ${R}-${fr.top}${fr.knocked ? ' (bị lùi do thua)' : ''}</em>` : ''}</span></div></header>
    <div class="mpx-wrap"><button class="mpx-nav l" data-mnav="-1" aria-label="Cuộn trái">‹</button><div class="mpx-track"><div class="mpx-lane">
      <svg class="mpx-path" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">
        <path d="${seg(0, B)}" class="ln"/>${reach > 1 ? `<path d="${seg(0, reach)}" class="dn"/>` : ''}</svg>
      ${seps.map(x => `<i class="mpx-sep" style="left:${x}%"></i>`).join('')}
      ${m.areas.map(node).join('')}
    </div></div><button class="mpx-nav r" data-mnav="1" aria-label="Cuộn phải">›</button></div>
    <div class="mpx-info" id="mpxInfo"></div>
  </div>`;
  renderMapInfo();
  // cuộn ngang: giữ vị trí cũ giữa các lần vẽ lại; mới mở / đổi bản đồ thì đưa ải đang đánh vào giữa
  const tr = document.querySelector('#pane-map .mpx-track');
  if (keepX != null && !mapNeedCenter) tr.scrollLeft = keepX; else mapCenter();
  mapScrollRealm = m.realm;
}
/** Đưa ải đang đánh vào giữa khung (tab đang ẩn thì để dành tới lúc mở tab). */
function mapCenter(){
  const tr = document.querySelector('#pane-map .mpx-track'), lane = tr?.firstElementChild;
  if (!tr || !tr.clientWidth){ mapNeedCenter = true; return; }
  tr.scrollLeft = Math.max(0, lane.clientWidth * mapPos(S.map.area).x / 100 - tr.clientWidth / 2);
  mapNeedCenter = false;
}
/** Trạng thái ô ải: locked · win · lose · cur · boss (Boss đã mở, chưa đánh). */
function mapStateOf(a){
  const m = S.map, B = DATA.areas + 1;
  if (a.index === B) return m.area > DATA.areas ? 'cur' : a.open ? 'boss' : 'locked';
  return !a.open ? 'locked' : a.index === m.area ? 'cur' : a.win ? 'win' : 'lose';
}
/** Vị trí ô ải (%): ải 1 bên trái → Boss bên phải, hai tầng so le trên/dưới. */
function mapPos(i){
  return {x: 3.5 + (i - 1) * 91 / DATA.areas, y: i > DATA.areas ? 52 : 54 + 16 * Math.sin(i * 0.95)};
}
/** Bảng dưới: thông tin ải đang rê chuột (mặc định ải đang đánh). */
function renderMapInfo(){
  const host = $('mpxInfo'); if (!host) return;
  const m = S.map, R = m.realm + 1, N = DATA.areas, B = N + 1, keyN = S.inventory[m.keyItem] || 0;
  const a = m.areas[Math.min(mapHover || m.area, B) - 1], st = mapStateOf(a), boss = a.index === B, hidden = st === 'locked' && !boss;
  const pct = v => v >= 1 ? '100%' : (Math.round(v * 1000) / 10).toLocaleString('vi-VN') + '%';
  const cell = (k, chance, qty, name) => ITEM[k] ? `<div class="mpx-rw" data-tip="${itemTip(k)}"><span class="dd-fr r-${ITEM[k].rarity || 'common'}">${itemImg(k)}${qty ? `<b>${qty}</b>` : ''}</span><small>${name || ITEM[k].name}</small><em>${pct(chance)}</em></div>` : '';
  let rewards;
  const bw = boss && a.waves?.[0];
  if (boss) rewards = (bw?.stoneMax ? cell('linh_thach', 1, bw.stoneMin === bw.stoneMax ? bw.stoneMin : `${bw.stoneMin}-${bw.stoneMax}`) : '') + (m.bossItem ? cell(m.bossItem, 1) : '') + cell(m.bossChest || 'ruong_boss', 1) + `<div class="mpx-rw"><span class="dd-fr qi">☯</span><small>Tu vi ×5</small><em>100%</em></div>`;
  else {
    const k = a.index, drops = m.drops || [], gear = drops.filter(x => x.gear);
    rewards = drops.filter(x => !x.gear).map(x => {
      const stone = x.item === 'linh_thach', sc = stone ? (1 + (k - 1) * .05) * (a.elite ? 1.5 : 1) : 1, lo = Math.round(x.min * sc), hi = Math.round(x.max * sc);
      return cell(x.item, Math.min(1, x.chance * (stone || x.chance >= 1 ? 1 : 1 + (k - 1) * .03)), lo === hi ? (lo > 1 ? lo : '') : `${lo}-${hi}`);
    }).join('');
    if (gear.length){
      const any = 1 - gear.reduce((p, x) => p * (1 - Math.min(1, x.chance * (1 + (k - 1) * .03))), 1), v = DATA.items.find(i => i.base === gear[0].item);
      if (v) rewards += cell(v.id, any, '', 'Trang bị ' + (v.set || ''));
    }
    rewards += cell(m.keyItem, m.keyChanceMin);
    if (m.chestChance) rewards += cell('ruong_thuong', m.chestChance);
    if (m.rareChestChance && k >= m.rareChestMinArea) rewards += cell('ruong_hiem', a.elite ? .2 : m.rareChestChance);   // tinh anh: rương hiếm 20% (GameData.EliteRareChestChance)
  }
  const img = (a.image || m.bossImage).replace('.png', '_hd.png');
  const stats = hidden ? '<p class="mpx-hid">Chưa thám thính được ải này.</p>'
    : boss || adminOn ? (!boss && a.waves?.length > 1
        ? `<p class="mpx-st">Máu ${a.waves.map(w => `<b>${fmt(w.hp)}</b>`).join(' · ')} · Công ${a.waves.map(w => `<b>${fmt(w.atk)}</b>`).join(' · ')}</p>`
        : `<p class="mpx-st">Máu <b>${fmt(a.hp)}</b> · Công <b>${fmt(a.atk)}</b> · Thủ <b>${fmt(a.def)}</b></p>`) + `
      <p class="mpx-pr ${a.win ? 'ok' : 'bad'}">${a.win ? `Dự đoán: thắng sau ${a.hits} đòn` : 'Dự đoán: thua, cần mạnh hơn'}</p>`
    : `<p class="mpx-hid">${st === 'win' ? 'Đã vượt qua ải này.' : st === 'lose' ? 'Yêu khí dày đặc, cẩn thận.' : 'Đang giao chiến tại đây.'}</p>`;
  const fr = mapFrontier();
  const bossOpen = m.areaMax >= N, canBoss = bossOpen && (keyN > 0 || adminBoss());
  const foot = boss ? `<p class="mpx-key"><span class="mp-need ${keyN > 0 ? 'ok' : ''}">${itemImg(m.keyItem)} ${keyN} / 1</span>
      ${st === 'cur' ? '<i>Đang khiêu chiến · mỗi trận tốn 1 lệnh</i>' : !bossOpen ? `<i class="bad">Mở ải ${R}-${N} trước</i>` : !canBoss ? `<i class="bad">Cần ${ITEM[m.keyItem].name}</i>` : `<i>Bấm Boss để khiêu chiến${m.realm === m.chapterMax && DATA.chapters[R]?.enabled ? ` · thắng mở chương ${R + 1}` : ''}</i>`}</p>`
    : `<p class="mpx-key"><i class="${st === 'locked' ? 'bad' : ''}">${st === 'locked' ? `Thắng ải ${R}-${a.index - 1} để mở` : st === 'cur' ? 'Đang xuất chinh ở ải này' : 'Bấm vào ải để xuất chinh'}</i></p>`;
  const knock = st === 'cur' && fr.knocked ? `<p class="mpx-down">⚑ Thua ở ${R}-${fr.top}, đang luyện ở ${R}-${a.index} <button data-retry="${fr.top}">Thử lại ${R}-${fr.top}</button></p>` : '';
  const hdImg = src => `<img src="${src.replace('.png', '_hd.png')}" onerror="this.onerror=null;this.src=this.src.replace('_hd.png','_48.png')" alt="">`;
  // ải thường: 3 đợt quái (ảnh nhỏ + tên); ải Boss: 1 ảnh lớn
  const mon = !boss && a.waves?.length > 1
    ? `<section class="mpx-mon"><div class="tx"><p class="ey">Ải ${R}-${a.index} · ${a.place} · ${a.waves.length} đợt</p>${knock}
        <div class="mpx-wv">${a.waves.map((w, i) => `<div class="w ${w.tier}"><span class="pic ${hidden ? 'unk' : ''}">${hdImg(w.image)}</span>
          <small><i>Đợt ${i + 1}${w.tier === 'elite' ? ' · tinh anh' : ''}</i>${hidden ? '???' : w.name}</small></div>`).join('')}</div>
        ${stats}${foot}</div></section>`
    : `<section class="mpx-mon ${boss ? 'bs' : ''}">
      <div class="pic ${hidden ? 'unk' : ''}">${hdImg(img.replace('_hd.png', '.png'))}</div>
      <div class="tx"><p class="ey">${boss ? `Ải ${R}-${B} · Boss` : `Ải ${R}-${a.index}`} · ${boss ? m.bossLair : a.place}</p>
        <p class="nm">${hidden ? '???' : a.monster}</p>${stats}${foot}</div></section>`;
  host.innerHTML = `${mon}
    <section class="mpx-rws"><p class="dd-sec">Phần thưởng · tỉ lệ rơi mỗi trận thắng</p><div class="g">${rewards}</div></section>`;
}
let mapFilter = 'all', mapHover = 0, mapScrollRealm = -1, mapNeedCenter = true;
/** Lần cuối người chơi tự chọn ải (ms): sau đó mới coi là "tự xuống ải thấp", không phải bị lùi do thua. */
let mapPickAt = 0;
try { mapPickAt = +localStorage.getItem('van-dao-map-pick') || 0; } catch(e){}
/** Ải cao nhất đã mở của bản đồ đang xem và nhân vật có đang bị lùi xuống dưới do thua không. */
function mapFrontier(){
  const m = S.map, top = Math.min(m.areaMax, DATA.areas);
  if (m.area > DATA.areas || m.area >= top) return {top, knocked: false};
  const loss = (S.logs || []).find(l => /^Thua ở ải|^Vắng mặt .*\(thua \d+\)/.test(l.text));
  return {top, knocked: !!loss && Date.parse(loss.at) > mapPickAt};
}
function initMap(){
  const pane = $('pane-map');
  new IntersectionObserver(es => { if (mapNeedCenter && es.some(e => e.isIntersecting)) mapCenter(); }).observe(pane);
  // lăn chuột dọc trên dải đường ải = cuộn ngang
  pane.addEventListener('wheel', e => {
    const tr = e.target.closest('.mpx-track'); if (!tr || Math.abs(e.deltaX) > Math.abs(e.deltaY) || tr.scrollWidth <= tr.clientWidth) return;
    tr.scrollLeft += e.deltaY; e.preventDefault();
  }, {passive: false});
  pane.addEventListener('click', e => {
    const mc = e.target.closest('[data-mch]');
    if (mc){ if (mc.getAttribute('aria-disabled') !== 'true' && +mc.dataset.mch !== S.map.realm){ mapHover = 0; mapNeedCenter = true; act(`map/${mc.dataset.mch}`); } return; }
    const nv = e.target.closest('[data-mnav]');
    if (nv){ const tr = pane.querySelector('.mpx-track'); tr.scrollBy({left: +nv.dataset.mnav * tr.clientWidth * .7, behavior: 'smooth'}); return; }
    const f = e.target.closest('[data-mf]');
    if (f){ mapFilter = f.dataset.mf; renderMap(); return; }
    const n = e.target.closest('[data-mx]');                     // bấm ô ải = xuất chinh ngay
    const rt = e.target.closest('[data-retry]');                                       // nút "Thử lại" ải cao nhất (bảng thông tin)
    if (rt){ mapPickAt = Date.now(); try { localStorage.setItem('van-dao-map-pick', mapPickAt); } catch(e2){} act(`area/${rt.dataset.retry}`); return; }
    if (n && n.getAttribute('aria-disabled') !== 'true'){
      mapPickAt = Date.now(); try { localStorage.setItem('van-dao-map-pick', mapPickAt); } catch(e){}   // tự chọn ải: không còn tính là bị lùi
      act(`area/${n.dataset.mx}`);
    }
  });
  pane.addEventListener('mouseover', e => {
    const n = e.target.closest('[data-mx]'); if (!n || +n.dataset.mx === mapHover) return;
    mapHover = +n.dataset.mx; renderMapInfo();
  });
  pane.addEventListener('mouseleave', () => { if (mapHover){ mapHover = 0; renderMapInfo(); } });
  pane.addEventListener('mouseout', e => {
    if (e.target.closest('.mpx-track') && !e.relatedTarget?.closest?.('.mpx-track') && mapHover){ mapHover = 0; renderMapInfo(); }
  });
}
