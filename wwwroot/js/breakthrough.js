/* Vấn Đạo · js/breakthrough.js: tab Đột phá. Tách từ index.html (tools/split_index.py). */
// =====================================================================
// ĐỘT PHÁ: tu sĩ ngồi thiền giữa trận pháp, điều kiện, tỉ lệ, 5 giai đoạn, màn hình kết quả
// =====================================================================
let btProc = null, btResult = null;
let btLock = false;   // khóa nút Đột phá từ lúc bấm đến khi hiện bảng kết quả (kể cả lúc chạy hoạt ảnh)
const sleep = ms => new Promise(r => setTimeout(r, ms));
const secTxt = ms => { const s = Math.ceil(ms / 1000); return s >= 60 ? `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}` : `${s}s`; };
const btLeft = ms => Math.max(0, ms - (performance.now() - recvAt));
const BT_PAL = {
  luyen_khi: {ring:'#4f7a86', rune:'#7fb8c0', glow:'#5f8e96', mote:'#9fc6cc'},     // lam đậm, xám, lam xỉn
  truc_co:   {ring:'#3f8fb0', rune:'#7fe3e0', glow:'#5fd0e0', mote:'#e6fbff'},     // lam sâu, lam ngọc, bạch ngọc
  kim_dan:   {ring:'#8a7a40', rune:'#f2d27a', glow:'#e6c46a', mote:'#fff3c8'},
  nguyen_anh:{ring:'#6a4a9a', rune:'#d4a8ff', glow:'#a77ae6', mote:'#f0e0ff'},     // tím, tử vân
  hoa_than:  {ring:'#7a8aa0', rune:'#e8f0ff', glow:'#c8d8f0', mote:'#ffffff'},     // bạch kim
};
/** Trạng thái điều kiện: tu vi, vật phẩm, viên mãn, thể trạng. */
function btReqs(){
  const q = qiNow(), rows = [];
  rows.push({ok: q >= S.qiReq, name: 'Tu vi', val: `<span class="btq">${fmt(q)} / ${fmt(S.qiReq)}</span>`});
  for (const c of S.targetCost){
    const it = ITEM[c.item], ok = c.have >= c.need;
    rows.push({ok, ic: c.item, name: it.name, val: `${fmt(c.have)} / ${fmt(c.need)}`, boss: it.type === 'boss',
      hint: ok ? '' : it.type === 'boss' ? `<a href="#" data-goboss>Đánh Boss ${DATA.chapters?.find(x => x.bossItem === realm().bossItem)?.boss || realm().boss}${bossChapterTag(realm().bossItem)}</a>` : it.type === 'pill' ? '<a href="#" data-gotab="craft">Hợp thành đan</a>' : ''});
  }
  if (S.isPeak) rows.push({ok: true, name: 'Tu luyện viên mãn', val: S.stageName});
  const debuff = btLeft(S.break.debuffMs), cd = btLeft(S.break.cooldownMs);
  rows.push({ok: !debuff && !cd, name: 'Thể trạng', val: `<span class="btbody">${debuff ? 'Tẩu hỏa · ' + secTxt(debuff) : cd ? 'Điều tức · ' + secTxt(cd) : 'Ổn định'}</span>`});
  return rows;
}
function btReady(){ return !(S.isPeak && !S.hasNext) && btReqs().every(r => r.ok); }

function renderReq(){
  const pane = $('pane-req');
  if (!$('btCanvas')){
    pane.innerHTML = `<div class="bt">
      <section class="bt-cur" id="btCur"></section>
      <section class="bt-mid"><div class="bt-stage"><canvas id="btCanvas" width="96" height="84" aria-label="Tu sĩ ngồi thiền giữa trận pháp"></canvas></div></section>
      <section class="bt-req" id="btReq"></section>
      <div class="bt-foot" id="btFoot"></div>
      <div class="bt-result" id="btResult" hidden></div>
    </div>`;
  }
  const top = S.isPeak && !S.hasNext, peak = S.isPeak && S.hasNext, pv = S.break.preview;
  const nextName = peak ? DATA.realms[S.realm + 1].name : top ? DATA.lockedRealms[0] : S.targetTitle.replace(' · ', ' ');
  const nextDesc = peak ? DATA.realms[S.realm + 1].desc.split('.')[0] + '.' : top ? 'Cảnh giới chưa khai mở.' : 'Đột phá tầng kế tiếp trong cùng cảnh giới.';
  const gains = [pv.hp && `Máu +${fmt(pv.hp)}`, pv.atk && `Tấn công +${fmt(pv.atk)}`, pv.def && `Phòng thủ +${fmt(pv.def)}`, pv.ratePct && `Tốc độ tu luyện +${pv.ratePct}%`,
    pv.bagSlots && `Hành trang +${pv.bagSlots} ô`, pv.newSlots.length && `Ô trang bị: ${pv.newSlots.join(', ')}`, pv.newStats.length && `Chỉ số mới: ${pv.newStats.join(', ')}`,
    pv.skillPoints ? `Điểm kỹ năng +${pv.skillPoints}` : `<span class="dim">Điểm kỹ năng +0</span>`].filter(Boolean);
  $('btCur').innerHTML = `<p class="bt-sec">Cảnh giới hiện tại</p>
    <p class="bt-realm">${S.realmName}</p><p class="bt-stage-n">${S.realmName} ${S.stageName}</p>
    <div class="bt-qi"><span>Tu vi</span><b class="btq"></b></div>
    <div class="bt-bar"><i id="btBar"></i><em id="btPct"></em></div>
    <div class="bt-down">↓</div>
    <div class="bt-next ${peak ? 'major' : ''} ${top ? 'locked' : ''}"><p class="bt-sec">${peak ? 'Đại cảnh giới kế tiếp' : 'Kế tiếp'}</p><p class="bt-realm2">${nextName}</p><p class="bt-desc">${nextDesc}</p></div>
    ${gains.length && !top ? `<ul class="bt-gain">${gains.map(g => g.startsWith('<span class="dim">') ? `<li class="dim">${g.slice(18, -7)}</li>` : `<li>${g}</li>`).join('')}</ul>` : ''}`;

  const reqs = btReqs();
  const chance = peak ? S.chance : 1, crit = chance * .2, fail = 1 - chance, major = fail * .25;
  const outs = peak ? [
    ['ok', 'Thành công', chance - crit, `Bước vào ${nextName}`],
    ['crit', 'Đại thành công', crit, 'Đạo cơ vững chắc · +10% máu, thủ vĩnh viễn'],
    ['bad', 'Thất bại', fail - major, 'Linh khí phản phệ · −10% tu vi · điều tức 1 phút'],
    ['major', 'Tẩu hỏa nhập ma', major, '−25% tu vi · suy yếu 5 phút'],
  ] : [['ok', 'Thành công', 1, 'Đột phá chắc chắn']];
  $('btReq').innerHTML = top ? `<p class="bt-sec">Điều kiện đột phá</p><p class="bt-note">Đã đạt đỉnh phong ${S.realmName}. Cảnh giới ${DATA.lockedRealms[0]} chưa khai mở.</p>` :
    `<p class="bt-sec">Điều kiện đột phá</p>
    <ul class="bt-list">${reqs.map(r => `<li class="${r.ok ? 'ok' : 'no'} ${r.boss ? 'boss' : ''}"><i>${r.ok ? '✓' : '⚠'}</i>${r.ic ? itemImg(r.ic) : '<span class="nic"></span>'}<span class="nm">${r.name}${r.hint ? `<small>${r.hint}</small>` : ''}</span><b>${r.val}</b></li>`).join('')}</ul>
    <p class="bt-sec">Tỉ lệ thành công</p>
    <div class="bt-rate" data-tip="Tỉ lệ gốc ${Math.round((peak ? S.break.baseChance : 1) * 100)}%${S.bonus && peak ? `&#10;+${Math.round(S.bonus * 100)}% kinh nghiệm từ các lần thất bại` : ''}&#10;Tối đa 95%"><b>${Math.round(chance * 100)}%</b>${S.bonus && peak ? `<small>gốc ${Math.round(S.break.baseChance * 100)}% · +${Math.round(S.bonus * 100)}% kinh nghiệm</small>` : `<small>${peak ? 'đại cảnh giới' : 'đột phá tầng'}</small>`}</div>
    <ul class="bt-outs">${outs.map(([k, n, p, d]) => `<li class="${k}"><span>${n}</span><b>${Math.round(p * 100)}%</b><small>→ ${d}</small></li>`).join('')}</ul>`;

  const ready = btReady(), running = !!btProc || btLock;
  const why = top ? '' : reqs.find(r => !r.ok);
  $('btFoot').innerHTML = top ? '' : `<button class="bt-go ${ready ? 'ready' : ''}" id="btGo" ${ready && !running ? '' : 'disabled'}>
      <b>${running ? 'Đang đột phá…' : 'Đột phá'}</b><small>${peak ? `Thử đột phá ${nextName}` : `${S.realmName} ${S.stageName} → ${nextName}`}</small></button>
    <p class="bt-why ${ready ? '' : 'bad'}" id="btWhy">${running ? '' : ready ? 'Đủ điều kiện' : `Chưa đủ: ${why ? why.name : ''}`}</p>`;
  btShowResult();
  liveBreak();
}

function liveBreak(){
  if (!S || !$('btCur')) return;
  const q = qiNow(), pct = Math.min(1, q / S.qiReq);
  document.querySelectorAll('#pane-req .btq').forEach(el => el.textContent = `${fmt(q)} / ${fmt(S.qiReq)}`);
  const bar = $('btBar'); if (bar){ bar.style.width = (pct * 100).toFixed(1) + '%'; bar.classList.toggle('full', pct >= 1); $('btPct').textContent = Math.floor(pct * 100) + '%'; }
  const body = document.querySelector('#pane-req .btbody');
  if (body){ const d = btLeft(S.break.debuffMs), c = btLeft(S.break.cooldownMs); body.textContent = d ? 'Tẩu hỏa · ' + secTxt(d) : c ? 'Điều tức · ' + secTxt(c) : 'Ổn định'; }
  // nút đột phá bật lại khi đủ tu vi / hết thời gian điều tức
  const go = $('btGo'); if (go && !btProc && !btLock){ const ok = btReady(); if (go.disabled === ok) renderReq(); }
}

function btShowResult(){
  const el = $('btResult'); if (!el) return;
  if (!btResult){ el.hidden = true; el._html = ''; return; }
  const {kind, before, after, msg} = btResult;
  const win = kind === 'gold' || kind === 'crit' || kind === 'good', major = before.peak;
  const d = (a, b) => b - a;
  const st = after.stats, bs = before.stats;
  let html;
  if (win){
    const rows = [[`Máu`, d(bs.hp, st.hp)], [`Tấn công`, d(bs.atk, st.atk)], [`Phòng thủ`, d(bs.def, st.def)]].filter(r => r[1])
      .map(([n, v]) => `<li>${n} <b>+${fmt(v)}</b></li>`).join('')
      + (after.rate > before.rate ? `<li>Tốc độ tu luyện <b>+${Math.round((after.rate / before.rate - 1) * 100)}%</b></li>` : '')
      + (after.bagCap > before.bagCap ? `<li>Hành trang <b>+${after.bagCap - before.bagCap} ô</b></li>` : '')
      + (after.skillPointsTotal > before.skillPts ? `<li>Điểm kỹ năng <b>+${after.skillPointsTotal - before.skillPts}</b></li>` : '');
    const pv = before.preview;
    const unl = major ? [pv.newSlots.length && `Ô trang bị: ${pv.newSlots.join(', ')}`, pv.newStats.length && `Chỉ số: ${pv.newStats.join(', ')}`,
      bossChapterHint(after.realm)].filter(Boolean) : [];
    html = `<div class="bt-card win ${kind}">
      <p class="t">${major ? (kind === 'crit' ? 'Đại thành công' : 'Đột phá thành công') : 'Đột phá thành công'}</p>
      ${kind === 'crit' ? '<p class="crit">Đạo cơ vững chắc</p>' : ''}
      <p class="lab">Cảnh giới hiện tại</p><p class="realm">${after.realmName}</p><p class="stg">${after.realmName} ${after.stageName}</p>
      <p class="lab">Phần thưởng</p><ul class="rw">${rows}</ul>
      ${kind === 'crit' ? `<p class="note">${msg.split(': ').slice(1).join(': ')}</p>` : ''}
      ${unl.length ? `<p class="lab">Mở khóa</p><ul class="unl">${unl.map(u => `<li>${u}</li>`).join('')}</ul>` : ''}
      <button class="bt-ok" data-btok>Tiếp tục</button></div>`;
  } else {
    const isMajor = kind === 'major';
    html = `<div class="bt-card lose ${kind}">
      <p class="t">${isMajor ? 'Tẩu hỏa nhập ma' : 'Đột phá thất bại'}</p>
      <p class="lab">Hậu quả</p><ul class="rw">
        <li>Tổn <b>${isMajor ? 25 : 10}%</b> tu vi</li>
        <li>${isMajor ? 'Kinh mạch hỗn loạn' : 'Khí tức bất ổn'}: điều tức <b>${isMajor ? '2 phút' : '1 phút'}</b> trước lần thử sau</li>
        ${isMajor ? '<li>Suy yếu <b>5 phút</b>: tu vi ×0.7, công / thủ −10%</li>' : ''}
        <li>Đan dược và linh thạch đã tiêu hao, yêu hạch Boss được giữ lại</li>
        <li>Lần sau tỉ lệ thành công <b>+10%</b></li></ul>
      <button class="bt-ok" data-btok>Tiếp tục</button></div>`;
  }
  if (el._html !== html){ el.innerHTML = html; el._html = html; }   // renderReq chạy lại liên tục: chỉ thay khi nội dung đổi, tránh thẻ nháy
  el.hidden = false;
}

async function btStart(){
  if (busy || btProc || btLock || !btReady()) return;
  btLock = true; const go = $('btGo'); if (go) go.disabled = true;      // khóa ngay khi bấm, chống bấm lặp
  const peak = S.isPeak && S.hasNext;
  const before = {peak, realm: S.realm, stats: {...S.stats}, rate: S.rate, bagCap: S.bagCap, skillPts: S.skillPointsTotal, preview: S.break.preview};
  busy = true; btResult = null;
  btProc = {t0: performance.now(), dur: peak ? 3000 : 1200, peak, res: null, err: null};   // thời gian diễn hoạt (ms) trước khi hiện kết quả
  renderReq();
  try { btProc.res = await api('POST', `/api/players/${S.id}/advance`); } catch(e){ btProc.err = e.message; }
  const wait = btProc.dur - (performance.now() - btProc.t0); if (wait > 0) await sleep(wait);
  const {res, err} = btProc;
  btProc = null;
  if (err){ busy = false; btLock = false; localLog(err, 'bad'); renderReq(); $('btWhy').textContent = err; $('btWhy').className = 'bt-why bad'; return; }
  btFx = {kind: res.kind, t0: performance.now()};
  const result = {kind: res.kind, msg: res.message || '', before, after: res.state};
  // bảng kết quả hiện sau khi hoạt ảnh thành công (tia sáng ~2.6s) / thất bại (~1.4s) chạy xong.
  // Trạng thái mới (renderAll vẽ lại mọi tab — nặng) chỉ áp dụng lúc đó, không chen giữa hoạt ảnh gây giật;
  // busy giữ tới lúc ấy để sync() định kỳ cũng không vẽ lại giữa chừng.
  const fx0 = btFx, win = ['gold', 'crit', 'good'].includes(res.kind);
  setTimeout(() => {
    busy = false;
    btLock = false;                                // trạng thái mới đã áp dụng: nút tự tính lại theo điều kiện mới
    setState(res.state);
    // bảng hiện ở khung hình sau lần vẽ lại nặng, để hoạt ảnh xuất hiện chạy trọn từ đầu
    requestAnimationFrame(() => requestAnimationFrame(() => { if (btFx === fx0 && !btProc){ btResult = result; renderReq(); } }));
  }, win ? 2600 : 1400);
  notifyHost('changed'); notifyHost('break', {kind: res.kind === 'crit' ? 'gold' : res.kind === 'major' ? 'bad' : res.kind});
}

// ---- cảnh: tu sĩ ngồi thiền + trận pháp dưới chân ----
const btImgs = {};
let btFx = null, btMotes = [];
/** Vòng sáng sau lưng (RealmDef.Halo): góc xoay cộng dồn theo thời gian thực, đổi cảnh giới thì vòng mới nở ra từ tâm. */
let btHalo = {a: 0, last: 0, src: null, born: 0};
const BT_HALO_TURN = 40;                                          // giây mỗi vòng (chiều kim đồng hồ)
function btFrame(now){
  requestAnimationFrame(btFrame);
  const c = $('btCanvas');
  if (!c || !S || !c.offsetParent) return;
  if (!c._sized){ const box = c.parentElement.getBoundingClientRect(); if (box.width){ const sc = Math.max(2, Math.min(4, Math.floor(Math.min(box.width / 96, box.height / 84)))); c.style.width = 96 * sc + 'px'; c.style.height = 84 * sc + 'px'; c._sized = true; } }
  const g = c.getContext('2d');
  const BTR = 4;                                                   // canvas nét ×4 cho ảnh tu sĩ thiết kế; toạ độ vẫn theo lưới 96×84
  if (c.width !== 96 * BTR){ c.width = 96 * BTR; c.height = 84 * BTR; }
  g.setTransform(BTR, 0, 0, BTR, 0, 0); g.imageSmoothingEnabled = false;
  const t = now / 1000, id = DATA.realms[S.realm].id, pal = BT_PAL[id] || BT_PAL.luyen_khi;
  const k = btProc ? Math.min(1, (now - btProc.t0) / btProc.dur) : 0;     // tiến độ 5 giai đoạn
  const fx = btFx ? (now - btFx.t0) / 1000 : 99, win = btFx && ['gold','crit','good'].includes(btFx.kind), lose = btFx && !win;
  const fxOn = fx < 2.4;
  const ready = btReady();
  g.clearRect(0, 0, 96, 84);
  const P = (x, y, col, a = 1) => { g.globalAlpha = a; g.fillStyle = col; g.fillRect(Math.round(x), Math.round(y), 1, 1); };
  // nền: quầng sáng bậc theo năng lượng
  const energy = btProc ? .25 + k * .75 : fxOn ? (1 - fx / 2.4) : ready ? .2 : .08;
  const glowCol = lose && fxOn ? '#5a1a14' : win && fxOn ? '#2a5a66' : pal.glow;
  for (let r = 4; r >= 1; r--){ g.globalAlpha = .07 * energy * (5 - r) + .02; g.fillStyle = glowCol; g.beginPath(); g.ellipse(48, 44, 10 + r * 9, 8 + r * 8, 0, 0, 7); g.fill(); }
  g.globalAlpha = 1;
  // vòng sáng sau lưng theo cảnh giới (Trúc Cơ trở lên), xoay chậm chiều kim đồng hồ; đột phá thì xoay nhanh dần (tối đa ×4)
  const hsrc = realm().halo;
  if (hsrc){
    if (!btImgs[hsrc]){ btImgs[hsrc] = new Image(); btImgs[hsrc].src = hsrc; }
    if (btHalo.src !== hsrc){ if (btHalo.src) btHalo.born = now; btHalo.src = hsrc; }
    const dts = btHalo.last ? Math.min(.1, (now - btHalo.last) / 1000) : 0; btHalo.last = now;
    if (!reduce) btHalo.a += dts * Math.PI * 2 / BT_HALO_TURN * (btProc ? 1 + k * 3 : 1);
    const hi = btImgs[hsrc];
    if (hi.complete && hi.naturalWidth){
      const grow = btHalo.born ? Math.min(1, (now - btHalo.born) / 800) : 1, e = 1 - (1 - grow) ** 3;
      const D = 60 * (.35 + .65 * e);
      let ha = (btProc ? .7 + .3 * k : ready ? 1 : .7) * e;
      if (lose && fxOn) ha *= Math.floor(fx * 12) % 2 ? .35 : .8;          // tẩu hỏa: nhấp nháy mờ
      g.save(); g.globalAlpha = ha; g.imageSmoothingEnabled = true; g.imageSmoothingQuality = 'high';
      g.translate(48, 44); g.rotate(btHalo.a); g.drawImage(hi, -D / 2, -D / 2, D, D);
      g.restore(); g.imageSmoothingEnabled = false; g.globalAlpha = 1;
    }
  }
  // trận pháp: 2 vòng elip + 8 phù văn, sáng dần theo giai đoạn
  const cx = 48, cy = 70;
  const ringCol = lose && fxOn ? '#c0503a' : win && fxOn ? '#e6fbff' : pal.ring;
  const ringA = btProc ? .5 + k * .5 : fxOn ? 1 - fx / 2.4 * .6 : ready ? .55 : .3;
  for (const [rx, ry, dash, sp] of [[36, 10, 3, 1], [26, 7, 2, -1.6]]){
    const n = Math.round(rx * 4);
    for (let i = 0; i < n; i++){
      const a = i / n * Math.PI * 2 + t * .25 * sp * (btProc ? 1 + k * 3 : 1);
      if (Math.floor(i / dash) % 2) continue;
      P(cx + Math.cos(a) * rx, cy + Math.sin(a) * ry, ringCol, ringA);
    }
  }
  const lit = btProc ? Math.floor(k * 8.99) : fxOn && win ? 8 : ready ? 8 : 0;
  for (let i = 0; i < 8; i++){
    const a = i / 8 * Math.PI * 2 - t * .12;
    const x = Math.round(cx + Math.cos(a) * 31), y = Math.round(cy + Math.sin(a) * 8.5);
    const on = i < lit, col = lose && fxOn ? '#e06a40' : on ? pal.rune : '#3a4a50';
    const a2 = on ? .7 + .3 * Math.sin(t * 4 + i) : .5;
    // phù văn nhỏ 3x3 hình chữ thập / chéo xen kẽ
    const pts = i % 2 ? [[0,0],[1,1],[-1,-1],[1,-1],[-1,1]] : [[0,0],[1,0],[-1,0],[0,1],[0,-1]];
    for (const [dx, dy] of pts) P(x + dx, y + dy, col, a2);
  }
  // tu sĩ ngồi thiền theo cảnh giới (img/med_<cảnh giới>.png; tách lại: tools/cut_heroes.py), thiếu thì dùng ảnh chung cũ
  const src = realm().heroMed || 'img/med_ref.png';
  if (!btImgs[src]){ btImgs[src] = new Image(); btImgs[src].src = src; }
  const im = btImgs[src];
  if (im.complete && im.naturalWidth){
    const h = 54, w = h * im.naturalWidth / im.naturalHeight;
    const sx = 48 - w / 2, sy = 74 - h + (btProc ? 0 : Math.sin(t * 1.2) * .6);
    // viền linh khí quanh người (silhouette lệch nhẹ, sáng dần)
    const aur = btProc ? .25 + k * .5 : fxOn && win ? .7 * (1 - fx / 2.4) : .12;
    if (!im._sil){ const o = document.createElement('canvas'); o.width = im.naturalWidth; o.height = im.naturalHeight; const og = o.getContext('2d'); og.drawImage(im, 0, 0); og.globalCompositeOperation = 'source-in'; og.fillStyle = '#e6fbff'; og.fillRect(0, 0, o.width, o.height); im._sil = o; }
    g.imageSmoothingEnabled = true; g.imageSmoothingQuality = 'high';
    g.globalAlpha = aur;
    for (const [dx, dy] of [[-.75,0],[.75,0],[0,-.75],[0,.75]]) g.drawImage(im._sil, sx + dx, sy + dy, w, h);
    g.globalAlpha = 1;
    if (lose && fxOn && Math.floor(fx * 12) % 2 === 0) g.globalAlpha = .75;
    g.drawImage(im, sx, sy, w, h);
    g.globalAlpha = 1; g.imageSmoothingEnabled = false;
  }
  // đột phá thành công: từng tia sáng vàng bắn vút lên quanh tu sĩ (đại cảnh giới nhiều tia hơn)
  if (win && fx < 2.6){
    const n = btFx.kind === 'good' ? 16 : 28, h = i => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
    if (fx < .35){ g.globalAlpha = .55 * (1 - fx / .35); g.fillStyle = '#fff3c4'; g.fillRect(44, 0, 8, 74); g.globalAlpha = .25 * (1 - fx / .35); g.fillRect(38, 0, 20, 74); }   // cột sáng lóe lúc đầu
    for (let i = 0; i < n; i++){
      const delay = h(i) * 1.1, life = .7 + h(i + 50) * .5, p = (fx - delay) / life;
      if (p <= 0 || p >= 1) continue;
      const x0 = 48 + (h(i + 100) - .5) * 46, y = 74 - p * (60 + h(i + 150) * 20), len = 6 + h(i + 200) * 10, w = h(i + 250) < .3 ? 1 : .5;
      const x = x0 + (x0 - 48) * p * .25, a = p < .15 ? p / .15 : 1 - (p - .15) / .85;
      const gr = g.createLinearGradient(0, y, 0, y + len);
      gr.addColorStop(0, '#fffbe6'); gr.addColorStop(.25, '#f8d25a'); gr.addColorStop(1, 'rgba(242,201,76,0)');
      g.globalAlpha = a; g.fillStyle = gr; g.fillRect(x - w / 2, y, w, len);
      g.fillStyle = '#ffffff'; g.fillRect(x - w / 2, y, w, .75);             // đầu tia sáng trắng
    }
    g.globalAlpha = 1;
  }
  // hạt linh khí: lúc thường bay lên lững lờ, khi đột phá xoáy vào người, thất bại thì văng ra
  const want = btProc ? 20 + k * 50 : ready ? 14 : 8;
  while (btMotes.length < want) btMotes.push({a: Math.random() * Math.PI * 2, r: 30 + Math.random() * 16, y: 20 + Math.random() * 50, s: .5 + Math.random(), life: Math.random()});
  if (btMotes.length > want + 4) btMotes.length = Math.round(want);
  for (const m of btMotes){
    if (btProc){ m.r -= (8 + k * 30) * m.s / 60; m.a += (1 + k * 3) * m.s / 60; if (m.r < 4){ m.r = 30 + Math.random() * 16; m.y = 20 + Math.random() * 50; } }
    else if (lose && fxOn){ m.r += 40 / 60; }
    else { m.y -= m.s * 6 / 60; if (m.y < 4){ m.y = 74; m.r = 18 + Math.random() * 26; } m.a += .2 / 60; }
    const x = 48 + Math.cos(m.a) * m.r, y = btProc ? 46 + Math.sin(m.a) * m.r * .45 : m.y;
    const col = lose && fxOn ? (m.s > 1 ? '#e06a40' : '#8a2a1a') : (btProc && k > .8) || (win && fxOn) ? (m.s > 1.2 ? '#f2d27a' : '#ffffff') : pal.mote;
    P(x, y, col, btProc ? .5 + k * .5 : .5);
  }
  // giai đoạn cuối: cột linh quang mảnh, không quá chói
  if ((btProc && k > .8) || (win && fx < 1.2)){
    const a = btProc ? (k - .8) * 4 : 1 - fx / 1.2;
    for (let y = 0; y < 70; y++) { P(47, y, '#e6fbff', a * .5 * (y / 70)); P(48, y, '#ffffff', a * .7 * (y / 70)); P(49, y, '#e6fbff', a * .5 * (y / 70)); }
    if (win) for (let i = 0; i < 6; i++){ const ang = t * 2 + i; P(48 + Math.cos(ang) * (6 + fx * 30), 44 + Math.sin(ang) * (4 + fx * 14), '#f2c94c', a); }
  }
  g.globalAlpha = 1;
}
requestAnimationFrame(btFrame);
addEventListener('resize', () => { const c = $('btCanvas'); if (c) c._sized = false; });

function initBreak(){
  $('pane-req').addEventListener('click', e => {
    if (e.target.closest('#btGo')){ btStart(); return; }
    if (e.target.closest('[data-btok]')){ btResult = null; renderReq(); return; }
    if (e.target.closest('[data-goboss]')){ e.preventDefault(); ddGoBoss(); return; }
    const gt = e.target.closest('[data-gotab]'); if (gt){ e.preventDefault(); openTab(gt.dataset.gotab, true); }
  });
}

const pct = v => `+${Math.round(v)}%`;
// hành trang chia trang: 6 cột × 7 hàng mỗi trang, thanh "‹ 1 2 3 ›" dưới lưới
const BAG_PER_PAGE = 48;                                        // 8 cột × 6 hàng
let bagPage = 0;
/** Thanh phân trang: ‹ trước · số trang (rút gọn bằng … khi nhiều) · sau ›. cur bắt đầu từ 0. */
function pagerHtml(cur, pages, attr){
  const nums = [];
  for (let p = 0; p < pages; p++){
    if (pages <= 7 || p === 0 || p === pages - 1 || Math.abs(p - cur) <= 1) nums.push(p);
    else if (nums[nums.length - 1] !== '…') nums.push('…');
  }
  const btn = (p, label, dis, aria) => `<button type="button" ${attr}="${p}" ${dis ? 'disabled' : ''} aria-label="${aria}">${label}</button>`;
  return btn(cur - 1, '‹', cur <= 0, 'Trang trước')
    + nums.map(p => p === '…' ? '<span class="pg-gap">…</span>'
      : `<button type="button" ${attr}="${p}" ${p === cur ? 'aria-current="page"' : ''} aria-label="Trang ${p + 1}">${p + 1}</button>`).join('')
    + btn(cur + 1, '›', cur >= pages - 1, 'Trang sau');
}
// sắp xếp trang bị trong túi (nhớ lựa chọn trên máy này)
const GEAR_SORTS = {
  slot: {name:'Loại', key: it => it.slot ? DATA.slots.findIndex(s => s.id === it.slot) : DATA.items.indexOf(it)},
  grade:{name:'Phẩm cấp', key: it => -(RAR_KEYS.indexOf(it.rarity || 'common') * 1000 + (it.itemLevel || 0) * 10) + (it.slot ? DATA.slots.findIndex(s => s.id === it.slot) : 0) / 100},   // Thiên → Phàm, cùng phẩm thì cấp cao trước
  atk:  {name:'Tấn công', key: it => it.slot ? -(it.atk||0) : DATA.items.indexOf(it)},
  hp:   {name:'Máu', key: it => it.slot ? -(it.hp||0) : DATA.items.indexOf(it)},
  def:  {name:'Phòng thủ', key: it => it.slot ? -(it.def||0) : DATA.items.indexOf(it)},
  name: {name:'Tên A–Z', key: it => it.name},
};
let gearSort = 'slot', sortOpen = false;
try { const v = localStorage.getItem('van-dao-gear-sort'); if (v && GEAR_SORTS[v]) gearSort = v; } catch(e){}
