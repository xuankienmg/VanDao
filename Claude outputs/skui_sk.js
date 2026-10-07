let skEl = 'hoa', skSel = 'liet_diem', skFade = 0, skPick = null, skAnimT0 = 0;
// Chiến đấu: ô 0 = ĐÒN THƯỜNG (Hỏa Cầu / Hỏa Tiễn, luôn sẵn có, không dùng điểm) · ô 1 = KỸ NĂNG chủ động (theo hồi chiêu)
const SLOT_NAMES = ['Đòn thường', 'Kỹ năng'], SLOT_IDX = [0, 1];
const fitsSlot = (s, i) => !!s && (i === 0 ? s.category === 'Đòn thường' : ['Công','Tuyệt kỹ'].includes(s.category));
const skTree = s => s.category === 'Đòn thường' ? 'basic' : (s.category === 'Căn cơ' || s.category === 'Bị động') ? 'passive' : 'active';
const skBasicId = () => (S.skillSlots && S.skillSlots[0]) || 'hoa_cau';
/** Ô chiến đấu (dùng chung cho Công pháp và Hành trang). */
function skSlotsHtml(cls = ''){
  return SLOT_IDX.map(i => {
    const id = i === 0 ? skBasicId() : (S.skillSlots || [])[1], s = id && skById(id), pick = skPick && fitsSlot(skById(skPick), i);
    const tip = s ? `${SLOT_NAMES[i]}: ${s.name}${i ? ` · Lv. ${skLv(id)} · hồi chiêu ${s.every} lượt` : ' · mỗi lượt'}&#10;${s.effect}` : `${SLOT_NAMES[i]}: trống&#10;Chọn một kỹ năng chủ động đã lĩnh ngộ (thi triển theo hồi chiêu)`;
    return `<button class="sk-slot ${i === 1 ? 'ult' : ''} ${s ? 'has' : ''} ${pick ? 'pick' : ''} ${cls}" data-slot="${i}" data-tip="${tip}" ${s && i ? `draggable="true" data-slotsk="${id}"` : ''}>
      ${s ? `<img src="${skIcon(s)}" alt="">` : `<span class="emp">★</span>`}<small>${SLOT_NAMES[i]}</small></button>`;
  }).join('');
}
try { skEl = localStorage.getItem('van-dao-sk-el') || 'hoa'; skSel = localStorage.getItem('van-dao-sk-sel') || 'liet_diem'; } catch(e){}
const SK_CAT = {'Căn cơ':'#c9a86a', 'Bị động':'#5fbf9a', 'Đòn thường':'#f8b04a', 'Công':'#f08a2a', 'Thủ':'#d8b04c', 'Khống':'#e0521e', 'Thân pháp':'#f8d25a', 'Tuyệt kỹ':'#ff5a3a'};
const SK_TYPE = {basic: 'Đánh thường', active: 'Chủ động', passive: 'Bị động'};
const skById = id => DATA.skillTree.find(s => s.id === id);
const skOfEl = el => DATA.skillTree.filter(s => s.element === el);
const skLv = id => S.skills[id] || 0;
const skCost = (s, lv) => lv === 0 ? s.cost : 1 + Math.floor(lv / 2);
const elOf = id => DATA.elements.find(e => e.id === id);
const skIcon = s => `img/sk_${s.id}.png`;
const skFactor = (s, lv) => lv > 0 ? lv + (s.lead || 0) : 0;
const realmName = i => DATA.realms[i]?.name || DATA.lockedRealms?.[i - DATA.realms.length] || '?';

/** Trạng thái một kỹ năng: locked · avail (lĩnh ngộ / nâng được) · learned · max, kèm danh sách điều kiện. */
function skState(s){
  const el = elOf(s.element), lv = skLv(s.id), max = lv >= s.maxLv, cost = skCost(s, lv);
  const reqs = [{ok: S.realm >= s.minRealm, t: `${realmName(s.minRealm)} cảnh`}]
    .concat(s.req.map(r => { const [rid, rl] = r.split(':'); return {ok: skLv(rid) >= +rl, t: `${skById(rid).name} Lv. ${rl}`}; }));
  if (!max) reqs.push({ok: S.skillPoints >= cost, t: `${cost} điểm kỹ năng`});
  const why = (el.open ? [] : ['Hệ chưa mở khóa']).concat(reqs.filter(r => !r.ok).map(r => 'Cần ' + r.t));
  const reqOk = el.open && reqs.slice(0, reqs.length - (max ? 0 : 1)).every(r => r.ok);
  const st = max ? 'max' : why.length ? (lv > 0 ? 'learned' : 'locked') : 'avail';
  return {lv, st, why, reqs, cost, max, reqOk, learned: lv > 0};
}
const pct1 = v => (Math.round(v * 10) / 10) + '%';
const SK_PASSIVE = s => skTree(s) === 'passive';
function skPassive(s, lv){
  const P = [['atkPct','Tấn công'],['defPct','Phòng thủ (kháng Hỏa)'],['hpPct','Máu'],['crit','Chí mạng Hỏa'],['speed','Thân pháp'],['move','Tốc chạy'],['qiPct','Tốc độ tu luyện'],['monAtkDown','Công của địch −'],['skillPct','Sát thương Hỏa'],['pen','Xuyên thấu Hỏa'],['qiCut','Giảm linh khí tiêu hao'],['rangePct','Tầm đánh chiêu Hỏa']];
  return P.filter(([k]) => s[k]).map(([k, n]) => [n, s[k] * skFactor(s, lv), s[k] * skFactor(s, Math.min(s.maxLv, lv + 1))]);
}
/** Tổng bị động của các kỹ năng đã học theo một chỉ số (để hiện linh khí / tầm đánh thực tế). */
const skSum = k => skOfEl(skEl).filter(SK_PASSIVE).reduce((a, s) => a + (s[k] || 0) * skFactor(s, skLv(s.id)), 0);

function renderSkill(){
  const pane = $('pane-skill'); if (!pane || !S) return;
  const el = elOf(skEl) || DATA.elements[0], list = skOfEl(el.id);
  if (!skById(skSel) || skById(skSel).element !== el.id) skSel = (list.find(s => skTree(s) === 'active') || list[0]).id;
  const root = list.find(s => s.category === 'Căn cơ'), basics = list.filter(s => skTree(s) === 'basic');
  const act = S.skillSlots?.[1] && skById(S.skillSlots[1]);
  pane.innerHTML = `<div class="sk2" style="--ec:${el.color};--ec2:${el.color2}">
    <nav class="sk-els" role="tablist" aria-label="Hệ tu luyện">${DATA.elements.map(e => `<button role="tab" class="${e.id === el.id ? 'on' : ''} ${e.open ? '' : 'lock'}" aria-selected="${e.id === el.id}" data-el="${e.id}" style="--c:${e.color}" data-tip="${e.name} hệ&#10;${e.desc}${e.open ? '' : '&#10;Chưa mở khóa'}">
      <img class="px" src="img/el_${e.id}.png" alt=""><span>${e.name.toUpperCase()}</span>${e.open ? '' : '<small>Chưa mở khóa</small>'}</button>`).join('')}</nav>
    <section class="sk2-left">
      <div class="sk2-char"><img src="img/hero_ref_stand.png" alt=""><div><b>${S.name}</b><span>${S.realmName} ${S.stageName}</span>
        <span>Thân hòa: <i style="color:${el.color}">${el.root}${root ? ` · Lv. ${skLv(root.id)}` : ''}</i></span></div></div>
      <div class="sk2-pts" data-tip="Điểm kỹ năng&#10;Chỉ dùng cho Cây Chủ động và Bị động (đánh thường không tốn điểm)&#10;Đã nhận ${S.skillPointsTotal} · đã dùng ${S.skillPointsTotal - S.skillPoints}"><span>Điểm kỹ năng</span><b>${S.skillPoints}</b></div>
      <h4>Đánh thường</h4>
      <p class="sk2-hint">Luôn sẵn có, không dùng điểm. Mạnh lên theo cảnh giới, chỉ số, trang bị và bị động.</p>
      ${basics.length ? basics.map(b => `<button class="sk2-basic ${skBasicId() === b.id ? 'on' : ''} ${skSel === b.id ? 'sel' : ''}" data-sk="${b.id}" ${el.open ? '' : 'disabled'}>
        <img src="${el.open ? skIcon(b) : `img/el_${el.id}.png`}" alt=""><span><b>${b.name}</b><small>${b.area} · ${b.dmgBase}% công${b.crit ? ` · +${b.crit}% chí mạng` : ''}</small></span>${skBasicId() === b.id && el.open ? '<em>ĐANG DÙNG</em>' : ''}</button>`).join('')
        : `<p class="sk2-hint">Đòn đánh thường ${el.name} hệ sẽ mở cùng hệ.</p>`}
      <h4>Kỹ năng trong trận</h4>
      <button class="sk2-slot ${act ? 'has' : ''}" data-slot="1" ${act ? `data-sk="${act.id}"` : ''}>${act ? `<img src="${skIcon(act)}" alt=""><span><b>${act.name}</b><small>Lv. ${skLv(act.id)} · hồi chiêu ${act.every} lượt</small></span>` : '<span class="emp">+</span><span><b>Ô trống</b><small>Chọn kỹ năng chủ động đã lĩnh ngộ</small></span>'}</button>
      ${el.id === 'hoa' ? `<div class="sk2-wx"><h4>Ngũ hành tương khắc</h4><p><b class="g">Hỏa khắc Mộc</b> +15% sát thương Hỏa</p><p><b class="r">Hỏa bị Thủy khắc</b> −15% sát thương Hỏa</p></div>` : ''}
    </section>
    <section class="sk2-tree act ${skFade ? 'fade' : ''}"><h3>Cây Chủ động <small>kỹ năng đặc biệt · có Animation</small></h3>${skTreeHtml(el, 'active', 3, 3)}</section>
    <section class="sk2-tree pas ${skFade ? 'fade' : ''}"><h3>Cây Bị động <small>tăng cường vĩnh viễn · không animation</small></h3>${skTreeHtml(el, 'passive', 5, 3)}</section>
    <aside class="sk-side" id="skSide">${skDetail(skById(skSel))}</aside>
    ${el.open ? '' : `<div class="sk-lockmsg"><b>${el.name.toUpperCase()} HỆ · CHƯA MỞ KHÓA</b><span>Cùng khung với Hỏa hệ: Đánh thường + Cây Chủ động + Cây Bị động. Sẽ mở trong bản cập nhật sau.</span></div>`}
  </div>`;
  skFade = 0;
  liveSkill();
}

/** Vẽ một cây (chủ động / bị động) trên lưới rows × cols từ Row/Col của dữ liệu; chỉ nối các tiền đề cùng cây. */
function skTreeHtml(el, tree, rows, cols){
  const list = el.open ? skOfEl(el.id).filter(s => skTree(s) === tree) : [];
  const X = c => (c + .5) / cols * 100, Y = r => (r + .5) / rows * 100;
  let lines = '';
  for (const s of list) for (const r of s.req){
    const p = skById(r.split(':')[0]); if (!p || skTree(p) !== tree) continue;
    const ps = skState(p), cs = skState(s);
    const cls = cs.learned ? 'on' : ps.learned && cs.reqOk ? 'next' : 'off';
    const x1 = X(p.col), y1 = Y(p.row), x2 = X(s.col), y2 = Y(s.row), ym = (y1 + y2) / 2;
    lines += `<path class="${cls}" d="M${x1} ${y1} V${ym} H${x2} V${y2}" />`;
  }
  const nodes = list.map(s => {
    const st = skState(s), sel = s.id === skSel, cross = s.req.filter(r => skTree(skById(r.split(':')[0])) !== tree);
    const tip = `${s.name} · ${SK_TYPE[tree]}&#10;Lv. ${st.lv} / ${s.maxLv}&#10;${st.reqs.map(r => (r.ok ? '✓ ' : '✗ ') + r.t).join('&#10;')}`;
    return `<button class="sk-n ${tree === 'active' ? 'act' : 'pas'} ${st.st} ${sel ? 'sel' : ''} ${S.skillSlots?.[1] === s.id ? 'eq' : ''}" data-sk="${s.id}" style="left:${X(s.col)}%;top:${Y(s.row)}%" data-tip="${tip}">
      <span class="sk-ic"><img src="${skIcon(s)}" alt="">${st.st === 'locked' ? '<i class="lk">🔒</i>' : ''}${st.max ? '<em>MAX</em>' : st.st === 'avail' ? '<em class="pl">+</em>' : ''}</span>
      <span class="sk-nm">${s.name}</span><span class="sk-lvt">Lv. ${st.lv} / ${s.maxLv}</span>${cross.length ? `<span class="sk-x" title="Cần thêm ở cây kia">⇄</span>` : ''}</button>`;
  }).join('');
  return `<div class="sk-grid ${tree}"><svg class="sk-lines" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">${lines}</svg>${nodes}</div>`;
}

function skDetail(s){
  if (!s) return '';
  const el = elOf(s.element), tree = skTree(s), st = skState(s), lv = st.lv, next = Math.min(s.maxLv, lv + 1);
  const dmg = l => s.dmgBase ? s.dmgBase + s.dmgPerLv * Math.max(0, l - 1) : 0;
  const qiCut = skSum('qiCut'), rng = skSum('rangePct');
  const range = s.range ? (rng && tree !== 'passive' ? `${(parseFloat(s.range) * (1 + rng / 100)).toFixed(1)}m <i>(+${pct1(rng)})</i>` : s.range) : '';
  const pass = skPassive(s, lv);
  const isBasic = tree === 'basic';
  const upBtn = isBasic ? '' : !el.open ? '<button class="sk-up" disabled>Chưa mở khóa</button>'
    : st.max ? '<button class="sk-up max" disabled>Đã đạt cấp tối đa</button>'
    : st.why.length ? `<button class="sk-up" disabled>${lv ? 'Nâng cấp' : 'Lĩnh ngộ'}</button>`
    : `<button class="sk-up go" data-up="${s.id}">${lv ? 'Nâng cấp' : 'Lĩnh ngộ'} <small>−${st.cost} điểm</small></button>`;
  const animBtn = s.animId && s.animId !== 'NONE' ? `<button class="sk-anim" data-anim>▶ Xem animation</button>` : '';
  const eqBtn = isBasic ? (skBasicId() === s.id ? '<p class="sk-note">Đang dùng làm đòn đánh thường.</p>' : `<button class="sk-eq" data-basic="${s.id}">Dùng làm đòn đánh thường</button>`)
    : tree === 'active' && lv > 0 ? (S.skillSlots?.[1] === s.id ? `<button class="sk-eq off" data-unslot="1">Tháo khỏi ô Kỹ năng</button>` : `<button class="sk-eq" data-equip-sk="${s.id}">Dùng trong trận (ô Kỹ năng)</button>`)
    : tree === 'passive' && lv > 0 ? '<p class="sk-note">Bị động: luôn có hiệu lực, không cần đặt vào ô.</p>' : '';
  return `<div class="sk-prev"><canvas id="skCanvas" width="120" height="60" aria-label="Xem trước ${s.name}"></canvas></div>
    <div class="sk-card">
      <div class="sk-hd"><img src="${el.open ? skIcon(s) : `img/el_${el.id}.png`}" alt=""><div><p class="nm">${s.name}</p><p class="sub"><span style="color:${SK_CAT[s.category]}">${SK_TYPE[tree]}${s.category === 'Tuyệt kỹ' ? ' · Tuyệt kỹ' : ''}</span> · ${el.name} hệ</p></div></div>
      <dl class="sk-dl">
        <dt>Loại</dt><dd>${SK_TYPE[tree]}${s.category === 'Tuyệt kỹ' ? ' (tối thượng)' : ''}</dd>
        <dt>Hệ</dt><dd>${el.name}</dd>
        ${s.code ? `<dt>Skill ID</dt><dd class="id">${s.code}</dd>` : ''}
        <dt>Animation ID</dt><dd class="id">${s.animId || 'NONE'}</dd>
        ${isBasic ? '' : `<dt>Cấp</dt><dd>${lv} / ${s.maxLv}</dd><dt>Cảnh giới</dt><dd class="${S.realm < s.minRealm ? 'bad' : ''}">${realmName(s.minRealm)}</dd>`}
        ${s.qiCost ? `<dt>Linh khí</dt><dd>${Math.round(s.qiCost * (1 - Math.min(60, qiCut) / 100))}${qiCut ? ` <i>(−${pct1(qiCut)})</i>` : ''}</dd>` : ''}
        ${isBasic ? '<dt>Thi triển</dt><dd>Mỗi lượt</dd>' : s.every ? `<dt>Hồi chiêu</dt><dd>${s.every} lượt</dd>` : ''}
        ${s.dmgBase ? `<dt>Sát thương</dt><dd>${isBasic ? s.dmgBase : dmg(Math.max(1, lv))}% linh lực</dd>` : ''}
        ${isBasic && s.crit ? `<dt>Chí mạng</dt><dd>+${s.crit}%</dd>` : ''}
        ${range ? `<dt>Tầm đánh</dt><dd>${range}</dd>` : ''}${s.area ? `<dt>Phạm vi</dt><dd>${s.area}</dd>` : ''}
      </dl>
      <p class="sk-eff">“${s.effect}”</p>
      ${s.id === 'hoa_cau' ? `<p class="sk-note">Biến thể theo cảnh giới: <b>${HC_TIER_NAME[Math.min(2, S.realm)]}</b></p>` : ''}
      ${pass.length ? `<p class="sk-sec">Hiệu quả bị động</p><ul class="sk-pass">${pass.map(([n, v, nx]) => `<li>${n} <b>+${pct1(v)}</b>${!st.max ? `<i>→ +${pct1(nx)}</i>` : ''}</li>`).join('')}</ul>` : ''}
      ${!isBasic && lv > 0 && tree === 'active' ? `<p class="sk-act">Trong trận: thi triển ở lượt 2, sau đó cứ ${s.every} lượt, mỗi lần ${dmg(lv)}% linh lực${!st.max ? ` (Lv. ${next}: ${dmg(next)}%)` : ''}.</p>` : ''}
      ${!isBasic && !st.max ? `<p class="sk-sec">${lv ? `Nâng cấp Lv. ${lv} → ${next}` : 'Yêu cầu lĩnh ngộ'}</p>
        <ul class="sk-reqs">${st.reqs.map(r => `<li class="${r.ok ? 'ok' : 'no'}">${r.ok ? '✓' : '✗'} ${r.t}</li>`).join('')}</ul>` : ''}
      <div class="sk-btns">${upBtn}${animBtn}</div>${eqBtn}
    </div>`;
}
