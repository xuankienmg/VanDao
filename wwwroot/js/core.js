/* Vấn Đạo · js/core.js: kết nối server, trạng thái, act(), tiện ích giao diện. Tách từ index.html (tools/split_index.py). */
/* ============================================================
   KẾT NỐI SERVER (ASP.NET Core + SQLite)
   Toàn bộ luật chơi nằm ở server, trình duyệt chỉ hiển thị.
   ============================================================ */
const KEY = 'van-dao-player-id';
const Q = new URLSearchParams(location.search);
const MODE = Q.get('mode') || 'full';          // full | strip | panel
document.documentElement.classList.add(MODE);
document.body.classList.add(MODE);
if (Q.get('opaque') === '1') document.body.classList.add('opaque');
// Gửi tin nhắn cho cửa sổ desktop (WebView2). Trên trình duyệt thường thì bỏ qua.
const notifyHost = (type, extra = {}) => { try { window.chrome?.webview?.postMessage({type, ...extra}); } catch(e){} };
let DATA, ITEM = {}, S, recvAt = 0, busy = false;

let postSeq = 0;           // tăng mỗi lần gửi lệnh POST: sync() bỏ kết quả GET đã cũ (gửi trước lệnh) để không ghi đè trạng thái mới
async function api(method, url){
  if (method !== 'GET') postSeq++;
  const r = await fetch(url, {method, headers:{'Content-Type':'application/json'}, body: method === 'POST' ? '{}' : undefined});
  const j = await r.json().catch(() => ({}));
  if (r.status === 426 && j.update) window.showUpdateGate?.(j.update);    // bản game đã cũ: server từ chối, hiện màn hình cập nhật
  if (!r.ok){ const e = new Error(j.error || 'Không kết nối được máy chủ.'); e.status = r.status; throw e; }
  return j;
}
const realm = () => DATA.realms[S.realm];
/** Trang phục đang mặc (S.skin, rỗng = theo cảnh giới) → bộ ảnh nhân vật: 'hero' (sprite chiến đấu) · 'heroStand' · 'heroMed' (ngồi thiền) · 'heroCast' (tung chiêu). */
const curSkin = () => (S && S.skin && DATA.skins?.find(x => x.id === S.skin && S.realm >= x.minRealm)) || null;
const skinArt = k => curSkin()?.[k] || realm()[k];
/** Chương có Boss rơi nguyên liệu đột phá item: " (chương N)" để ghi kèm tên Boss. */
const bossChapterTag = item => { const c = (DATA.chapters || []).find(x => x.bossItem === item); return c ? ` · chương ${c.index + 1}` : ''; };
/** Dòng gợi ý sau khi lên đại cảnh giới ri: Boss giữ nguyên liệu cho lần đột phá kế. */
const bossChapterHint = ri => { const r = DATA.realms[ri], c = r && (DATA.chapters || []).find(x => x.bossItem === r.bossItem);
  return c ? `Đột phá kế cần ${ITEM[r.bossItem].name}: Boss ${c.boss} · chương ${c.index + 1}` : ''; };
// Tu vi dự đoán phía client giữa hai lần đồng bộ; server mới là nguồn chính xác
const qiNow = () => Math.min(S.qiReq, S.qi + S.rate * (performance.now() - recvAt) / 1000);
const exploreLeft = () => Math.max(0, S.exploreReadyInMs - (performance.now() - recvAt));
const hasAll = cost => cost.every(c => c.have >= c.need);
const fmt = n => Math.floor(n).toLocaleString('vi-VN');
const $ = id => document.getElementById(id);

let lastPull = 0;
/** Các loại rương, quý → thường (thứ tự hiện trong thông báo nhận rương). */
const CHEST_IDS = ['ruong_boss', 'ruong_hiem', 'ruong_thuong'];
let lastLogKey = '';
function setState(st){
  const prevKey = lastLogKey, prev = S;
  S = st; recvAt = performance.now();
  // rương rơi từ quái: số rương trong túi tăng so với lần trước → thông báo
  if (prev && prev.id === S.id){
    const got = CHEST_IDS.map(k => [k, (S.inventory[k] || 0) - (prev.inventory?.[k] || 0)]).filter(([, n]) => n > 0);
    if (got.length && typeof showChestDrop === 'function') showChestDrop(got);
    const pm = prev.map, nm = S.map;
    if (pm && nm && pm.realm === nm.realm && pm.area <= DATA.areas && nm.area < pm.area){
      const loss = (S.logs || []).find(l => /^Thua ở ải/.test(l.text));
      if (loss && Date.parse(loss.at) > mapPickAt) showKnockback(pm.area, nm.area);
    }
  }
  const top = S.logs[0]; lastLogKey = top ? top.at + top.text : '';
  if (prevKey && top && lastLogKey !== prevKey && (top.kind === 'loot' || top.kind === 'gold') && typeof floats !== 'undefined'){
    const loot = (top.text.split(': ')[1] || '').replace(/\.$/, '').split(', ').filter(x => x.startsWith('+')).slice(0, 3);
    loot.forEach((txt, i) => floats.push({x:0, row:i, text:txt, t:-i*.15, c: i === 0 ? null : '#e9dfc4'}));
    if (top.kind === 'gold') rings.push({t:0, c:'#e6c46a', big:false});
  }
  renderAll();
}
function localLog(text, kind){ S.logs.unshift({at:new Date().toISOString(), text, kind}); renderLog(); }

async function act(path){
  if (busy) return; busy = true;
  const prevRealm = S.realm;
  try {
    const res = await api('POST', `/api/players/${S.id}/${path}`);
    setState(res.state);
    notifyHost('changed');
    if (path === 'advance'){                     // hiệu ứng chạy sau khi lần vẽ lại của setState đã xong, tránh khựng khung đầu
      const k = res.kind || 'good';
      requestAnimationFrame(() => requestAnimationFrame(() => playBreak(k)));
      notifyHost('break', {kind: k});
    }
    if (res.craft) showCraft(res.craft);
  } catch(e){ localLog(e.message, 'bad'); ddErr = {msg: e.message, at: performance.now()}; }
  finally { busy = false; live(); }
}
async function channel(){
  // phản hồi ngay trên màn hình, server xác nhận sau
  const g = S.channelGain;
  S.qi = Math.min(S.qiReq, qiNow() + g); recvAt = performance.now();
  floats.push({x:(Math.random()-.5)*80, y:-170, text:'+' + fmt(g), t:0}); live();
  try { const res = await api('POST', `/api/players/${S.id}/channel`); if (!busy) setState(res.state); notifyHost('changed'); }
  catch(e){ localLog(e.message, 'bad'); }
}
async function sync(){
  if (busy || !S) return;
  const seq = postSeq;
  try { const r = await api('GET', `/api/players/${S.id}`); if (!busy && seq === postSeq) setState(r.state ?? r); } catch(e){}
}

/* ============================================================
   GIAO DIỆN
   ============================================================ */
function icon(k){
  const it = ITEM[k], c = it.color;
  if (it.icon) return `<img class="ic" src="${it.icon}" alt="">`;
  const shapes = {
    stone:`<path d="M12 2.5 20 8.5 16.8 20.5H7.2L4 8.5Z" fill="${c}" opacity=".9"/><path d="M12 2.5 9.5 8.5 12 20.5 14.5 8.5Z M4 8.5H20" fill="none" stroke="#0e1416" stroke-opacity=".35" stroke-width="1"/>`,
    pill:`<circle cx="12" cy="12" r="8" fill="${c}"/><ellipse cx="9.3" cy="9" rx="3" ry="2" fill="#fff" opacity=".55"/>`,
    herb:`<path d="M12 21.5c0-3 .5-6 2-8.5" stroke="#5f8a52" stroke-width="1.6" fill="none"/><path d="M14 13c-5 1-9-2-10-8 6-1 10 2 10 8Z" fill="${c}"/><path d="M14 13c3-1 6-4 6-8-4 0-7 3-6 8Z" fill="${c}" opacity=".75"/>`,
    artifact:`<path d="M12 3c-3 0-5 2-5 5 0 2 1 3 2 4-2 1-3 3-3 5 0 2.5 2.7 4 6 4s6-1.5 6-4c0-2-1-4-3-5 1-1 2-2 2-4 0-3-2-5-5-5Z" fill="${c}"/><path d="M9 12h6" stroke="#0e1416" stroke-opacity=".4" stroke-width="1.4"/><circle cx="10" cy="7" r="1.4" fill="#fff" opacity=".5"/>`,
    quest:`<rect x="5" y="3" width="14" height="18" rx="2" fill="${c}"/><path d="M8 8h8M8 12h8M8 16h5" stroke="#0e1416" stroke-opacity=".45" stroke-width="1.4"/>`,
    core:`<circle cx="12" cy="12" r="10" fill="none" stroke="${c}" stroke-opacity=".5" stroke-width="1.4"/><circle cx="12" cy="12" r="6.5" fill="${c}"/><circle cx="10" cy="10" r="2" fill="#fff" opacity=".5"/>`,
    ore:`<path d="M12 2 21 12 12 22 3 12Z" fill="${c}"/><path d="M12 2 12 22 M3 12H21" stroke="#0e1416" stroke-opacity=".3"/>`,
  };
  return `<svg class="ic" viewBox="0 0 24 24" aria-hidden="true">${shapes[it.type]}</svg>`;
}

/* ---- Giữ nhịp hiệu ứng lặp khi vẽ lại DOM ----
   Các bảng (Hành trang, Lò ghép…) vẽ lại bằng innerHTML nên phần tử mới khiến animation lặp
   (viền đồ Đỏ, lấp lánh đồ Thiên…) chạy lại từ đầu → giật. Neo mọi animation vô hạn vào gốc
   document.timeline: phần tử mới tiếp tục đúng pha cũ (animation-delay âm vẫn giữ độ lệch). */
(function(){
  if (!document.getAnimations) return;
  let queued = false;
  const align = () => {
    queued = false;
    for (const a of document.getAnimations()){
      if (!(a instanceof CSSAnimation) || a.startTime === 0 || a.playState !== 'running') continue;
      if (a.effect?.getTiming().iterations !== Infinity) continue;
      a.startTime = 0;
    }
  };
  const kick = () => { if (!queued){ queued = true; requestAnimationFrame(align); } };
  new MutationObserver(kick).observe(document.documentElement, {childList:true, subtree:true, attributes:true, attributeFilter:['class']});
  addEventListener('DOMContentLoaded', kick); kick();
})();
