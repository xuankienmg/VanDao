/* Linh thú: server-owned state; no local simulation of pets or rewards. */
(() => {
  'use strict';
  const ui = { tab: 'stable', selected: null, element: '', rarity: '', sort: 'level', release: null, pending: false, error: '', signature: '', state: null, received: 0, timer: null };
  const esc = v => String(v ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const num = v => Number.isFinite(+v) ? +v : 0;
  const f = v => typeof fmt === 'function' ? fmt(num(v)) : String(num(v));
  const state = () => S?.pet || {owned: [], hatch: []};
  const defs = () => DATA?.petDefs || [];
  const definition = p => defs().find(d => d.id === p.defId) || {};
  const labels = {hoa:'Hỏa',thuy:'Thủy',loi:'Lôi',moc:'Mộc',tho:'Thổ',bang:'Băng',gio:'Phong',phong:'Phong',common:'Phàm',uncommon:'Linh',rare:'Huyền',epic:'Địa',legendary:'Thiên'};
  const label = v => labels[v] || v || '—';
  const quality = v => ({common:0,uncommon:1,rare:2,epic:3,legendary:4,'Phàm':0,'Linh':1,'Huyền':2,'Địa':3,'Thiên':4}[v] ?? 0);
  const paw = '<svg viewBox="0 0 64 64" aria-hidden="true"><ellipse cx="19" cy="20" rx="6" ry="9"/><ellipse cx="33" cy="14" rx="6" ry="9"/><ellipse cx="47" cy="21" rx="6" ry="9"/><ellipse cx="10" cy="34" rx="5" ry="7"/><path d="M16 46c0-9 10-19 17-19s19 13 19 22c0 11-11 7-19 7s-17 1-17-10Z"/></svg>';
  const image = (id, form, src, silhouette = false) => `<span class="pet-art${silhouette ? ' pet-unknown' : ''}">${paw}<img src="${esc(src || `img/pet/${encodeURIComponent(id)}_${num(form)}.png`)}" alt="" loading="lazy"></span>`;
  const button = (text, action, disabled = false, cls = '') => `<button type="button" class="pet-btn ${cls}" data-pet-action="${esc(action)}"${disabled || ui.pending ? ' disabled' : ''}>${text}</button>`;
  const badge = p => `<span class="pet-badge">${esc(label(p.element))}</span><span class="pet-badge pet-quality-${quality(p.rarity)}">${esc(label(p.rarity))}</span>`;
  const role = p => num(p.slot) === 1 ? 'Xuất chiến' : num(p.slot) > 1 ? `Hộ trận ${num(p.slot)-1}` : 'Đang nghỉ';
  const power = p => num(p.atk) + num(p.def) + num(p.hp);
  function remaining(h) {
    if (h.ready) return 0;
    if (h.readyInSeconds != null) return Math.max(0, Math.ceil(num(h.readyInSeconds) - (performance.now()-ui.received)/1000));
    const at = typeof h.readyAt === 'number' ? h.readyAt * (h.readyAt < 1e12 ? 1000 : 1) : Date.parse(h.readyAt);
    return Number.isFinite(at) ? Math.max(0, Math.ceil((at-Date.now())/1000)) : 0;
  }
  const clock = n => n ? `${Math.floor(n/3600) ? Math.floor(n/3600)+':' : ''}${String(Math.floor(n/60)%60).padStart(2,'0')}:${String(n%60).padStart(2,'0')}` : 'Đã nở · có thể nhận';
  function detail(p) {
    if (!p) return `<div class="pet-empty">${paw}<h3>Chưa có linh thú</h3><p>Nhận Thú Đản từ rương Boss hoặc Quay Linh Thú, rồi đặt vào ổ ấp.</p>${button('Đến ổ ấp', 'tab:hatch', false, 'pet-primary')}</div>`;
    const d = definition(p), form = num(p.form), food = num(S.inventory?.linh_thu_dan), cap = num(state().levelCap), maxed = cap > 0 && num(p.level) >= cap;
    const rail = [0,1,2].map((i) => `<div class="${i === form ? 'current' : i < form ? 'done' : ''}" title="${esc(d.forms?.[i]?.skillName || '')}"><span>${i < form ? '✓' : i+1}</span><b>${esc(d.forms?.[i]?.name || ['Ấu thể','Thành thể','Thần thú'][i])}</b></div>`).join('');
    const stats = [['Công','atk','aptAtk'],['Thủ','def','aptDef'],['Huyết','hp','aptHp']].map(([label,k,a]) => {
      const raw = num(p[a]), pct = Math.max(0, Math.min(100, raw <= 1 ? raw*100 : raw));
      return `<div class="pet-stat"><span>${label}</span><strong>${f(p[k])}</strong><small>Tư chất ${Math.round(pct)}%</small><meter min="0" max="100" value="${pct}">${pct}%</meter></div>`;
    }).join('');
    const slots = [button(num(p.slot) === 1 ? 'Đang xuất chiến' : 'Xuất chiến', 'slot:1', num(p.slot) === 1, 'pet-primary')];
    for (let i=0; i<Math.min(2,num(state().guardSlots)); i++) slots.push(button(`Hộ trận ${i+1}`, `slot:${i+2}`, num(p.slot) === i+2));
    if (num(p.slot)) slots.push(button('Nghỉ', 'slot:0'));
    return `<div class="pet-detail-head"><div><h3>${esc(p.name || d.name)}</h3><p>${esc(p.formName || d.forms?.[form]?.name)} · Cấp ${f(p.level)}${cap ? '/'+f(cap) : ''}</p></div><div>${badge(p)}</div></div>
      <div class="pet-portrait">${image(p.defId,form,p.image)}<span>${esc(role(p))}</span></div>
      <div class="pet-evolution" aria-label="Tiến trình tiến hóa">${rail}</div>
      <div class="pet-exp"><span>EXP ${f(p.exp)} / ${f(p.expNeed)}</span><progress value="${num(p.exp)}" max="${Math.max(1,num(p.expNeed))}"></progress></div>
      <div class="pet-stats">${stats}</div>
      <section class="pet-bonus"><h4>Cộng cho chủ nhân</h4><div><span>Công <b>+${f(p.shareAtk)}</b></span><span>Thủ <b>+${f(p.shareDef)}</b></span><span>Huyết <b>+${f(p.shareHp)}</b></span></div><small>${num(p.slot) ? 'Phần cộng hiện tại do máy chủ tính.' : 'Cho xuất chiến hoặc hộ trận để nhận phần cộng.'}</small></section>
      <section class="pet-lore"><h4>Thiên phú</h4><p>${esc(p.talentText || d.talentText || 'Chưa có thông tin.')}</p><h4>${esc(d.forms?.[form]?.skillName || 'Kỹ năng')} <small>mỗi ${f(p.skillEvery || d.skillEvery)} lượt</small></h4><p>${esc(p.skillText || d.skillText || 'Chưa có thông tin.')}</p></section>
      <div class="pet-actions">${slots.join('')}</div>
      <section class="pet-feed"><h4>Cho ăn <small>Linh Thú Đan: ${f(food)}${maxed ? ' · Đã đạt giới hạn cấp' : ''}</small></h4><div class="pet-actions">${button('×1','feed:1',!food||maxed)}${button('×10','feed:10',food<10||maxed)}${button('Tối đa','feed:0',!food||maxed)}</div></section>
      <section class="pet-evolve"><h4>Tiến hóa</h4><div class="pet-costs">${(p.evolveCost || []).map(c => `<span class="${num(c.have)<num(c.need)?'pet-short':''}">${esc(ITEM[c.item]?.name || c.item)} <b>${f(c.have)}/${f(c.need)}</b></span>`).join('') || `<small>${form >= 2 ? 'Đã đạt dạng Thần thú.' : 'Đạt cấp và cảnh giới yêu cầu để tiến hóa.'}</small>`}</div>${button('Tiến hóa','evolve',!p.canEvolve,'pet-primary')}</section>
      <div class="pet-release">${ui.release === String(p.id) ? `<p>Phóng sinh ${esc(p.name || d.name)}? Linh thú sẽ rời chuồng. Máy chủ tính số đan hoàn trả.</p>${button('Xác nhận phóng sinh','release-confirm',false,'pet-danger')}${button('Giữ lại','release-cancel')}` : button('Phóng sinh','release-arm',false,'pet-text')}</div>`;
  }
  function stable() {
    const owned = state().owned || [], list = owned.filter(p => (!ui.element || p.element === ui.element) && (!ui.rarity || p.rarity === ui.rarity));
    list.sort((a,b) => ui.sort === 'name' ? String(a.name).localeCompare(String(b.name),'vi') : ui.sort === 'power' ? power(b)-power(a) : ui.sort === 'rarity' ? quality(b.rarity)-quality(a.rarity) : num(b.level)-num(a.level));
    if (!list.some(p => String(p.id) === ui.selected)) { ui.selected = list.length ? String(list[0].id) : null; ui.release = null; }
    const options = (key,caption,value) => `<select data-pet-filter="${key}" aria-label="${caption}"><option value="">${caption}</option>${[...new Set(owned.map(p=>p[key]).filter(Boolean))].map(v=>`<option${v===value?' selected':''} value="${esc(v)}">${esc(label(v))}</option>`).join('')}</select>`;
    return `<div class="pet-stable"><section class="pet-collection"><div class="pet-collection-head"><h3>Linh thú của bạn</h3><span>${owned.length}/${f(state().capacity)}</span></div><div class="pet-filters">${options('element','Mọi hệ',ui.element)}${options('rarity','Mọi phẩm',ui.rarity)}<select data-pet-filter="sort" aria-label="Sắp xếp">${[['level','Cấp cao'],['power','Chỉ số cao'],['rarity','Phẩm cao'],['name','Tên']].map(([k,v])=>`<option value="${k}"${ui.sort===k?' selected':''}>${v}</option>`).join('')}</select></div><div class="pet-cards">${list.map(p=>`<button type="button" class="pet-card${String(p.id)===ui.selected?' selected':''}" data-pet-select="${esc(p.id)}" aria-pressed="${String(p.id)===ui.selected}">${image(p.defId,p.form,p.image)}<strong>${esc(p.name || definition(p).name)}</strong><small>${esc(label(p.element))} · ${esc(label(p.rarity))} · Cấp ${f(p.level)}</small><span class="pet-slot${num(p.slot)?' active':''}">${esc(role(p))}</span></button>`).join('') || `<p class="pet-list-empty">${owned.length ? 'Không có linh thú khớp bộ lọc.' : 'Chuồng đang trống. Hãy ấp Thú Đản đầu tiên.'}</p>`}</div></section><section class="pet-detail" aria-label="Chi tiết linh thú">${list.length || !owned.length ? detail(list.find(p=>String(p.id)===ui.selected)) : '<div class="pet-empty"><p>Đổi bộ lọc để chọn linh thú.</p></div>'}</section></div>`;
  }
  function hatch() {
    const p = state(), occupied = (p.hatch || []).filter(h=>h.egg), count = Math.max(num(p.hatchSlots),occupied.length), full = (p.owned || []).length >= num(p.capacity);
    const eggs = Object.entries(S.inventory || {}).filter(([id,n])=>n>0 && (ITEM[id]?.type === 'egg' || id.startsWith('thu_dan_')));
    return `<div class="pet-hatch"><h3>Ấp một mối duyên</h3><p>Thú Đản tiếp tục ấp khi bạn rời đi. Nhận linh thú khi trứng đã nở.</p><div class="pet-nests">${Array.from({length:count},(_,slot)=>{
      const h = occupied.find(x=>num(x.slot)===slot);
      return `<section class="pet-nest"><h4>Ổ ấp ${slot+1}</h4>${h ? `<div class="pet-egg-art">${ITEM[h.egg]?.icon ? `<img src="${esc(ITEM[h.egg].icon)}" alt="">` : "◈"}</div><b>${esc(ITEM[h.egg]?.name || h.egg)}</b><p data-pet-timer="${slot}"></p>${button('Nhận linh thú',`collect:${slot}`,remaining(h)>0||full,'pet-primary pet-collect')}${button(`Ấp ngay · ${f(h.instantCost)} linh thạch`,`now:${slot}`,remaining(h)===0||num(S.inventory?.linh_thach)<num(h.instantCost),'pet-now')}${full?'<small>Chuồng đã đầy. Phóng sinh để có chỗ nhận.</small>':''}` : `<p>Ổ đang trống</p>${eggs.map(([id,n])=>button(`${esc(ITEM[id]?.name || id)} ×${f(n)}`,`hatch:${slot}:${id}`,full)).join('') || '<small>Nhận Thú Đản từ rương Boss hoặc Quay Linh Thú.</small>'}`}</section>`;
    }).join('') || '<div class="pet-empty"><p>Chưa có ổ ấp khả dụng.</p></div>'}</div><section class="pet-lore"><h4>Thú Đản trong túi</h4>${eggs.map(([id,n])=>`<p>${esc(ITEM[id]?.name || id)} <b>×${f(n)}</b></p>`).join('') || '<p>Chưa có Thú Đản. Khi nhận được trứng, quay lại đây để ấp.</p>'}</section></div>`;
  }
  function codex() {
    const owned = state().owned || [], known = new Set(owned.map(p=>p.defId));
    return `<div class="pet-codex"><div class="pet-collection-head"><h3>Thú điển</h3><span>${known.size}/${defs().length} loài đang sở hữu</span></div><p>Ba hình thái của mỗi loài. Hình thái sáng lên theo linh thú đang có trong chuồng.</p>${defs().map(d=>`<section class="pet-species"><div><h4>${esc(d.name)}</h4>${badge(d)}<p>${esc(d.desc || d.role || '')}</p></div><div class="pet-forms">${[0,1,2].map(i=>{const unlocked=owned.some(p=>p.defId===d.id&&num(p.form)>=i);return `<div>${image(d.id,i,d.forms?.[i]?.image,!unlocked)}<span>${esc(d.forms?.[i]?.name || ['Ấu thể','Thành thể','Thần thú'][i])}</span><small>${unlocked?'Đang sở hữu':'Chưa sở hữu'}</small></div>`;}).join('')}</div></section>`).join('') || '<p>Thú điển sẽ xuất hiện khi máy chủ gửi dữ liệu loài.</p>'}</div>`;
  }
  function tick() {
    const root = document.getElementById('pane-pet');
    const p = state(), hatches = p.hatch || [];
    document.getElementById('btnPet')?.classList.toggle('ready',hatches.some(h=>h.egg&&remaining(h)===0)||(p.owned||[]).some(p=>p.canEvolve));
    if (!root) return;
    root.querySelectorAll('[data-pet-timer]').forEach(el=>{
      const h=hatches.find(h=>num(h.slot)===num(el.dataset.petTimer)); if (!h) return;
      const left=remaining(h); el.textContent=clock(left);
      const nest=el.closest('.pet-nest');
      nest.querySelector('.pet-collect').disabled=ui.pending||left>0||(p.owned||[]).length>=num(p.capacity);
      nest.querySelector('.pet-now').disabled=ui.pending||left===0||num(S.inventory?.linh_thach)<num(h.instantCost);
    });
  }
  async function send(path) {
    if (ui.pending || busy) return;
    const previous=new Set((state().owned||[]).map(p=>p.id));let hatched=false;
    ui.pending=true; busy=true; ui.error=''; render(true);
    try { const res=await api('POST',`/api/players/${encodeURIComponent(S.id)}/${path}`); setState(res.state); notifyHost('changed'); ui.release=null;
      if(path.startsWith('hatch/collect')){const born=(state().owned||[]).find(p=>!previous.has(p.id));if(born){ui.tab='stable';ui.selected=String(born.id);hatched=true;}}
    }
    catch(e) { ui.error=e.message || 'Không kết nối được máy chủ. Hãy thử lại.'; }
    finally { ui.pending=false; busy=false; render(true);if(hatched&&!matchMedia('(prefers-reduced-motion: reduce)').matches)document.querySelector('#pane-pet .pet-portrait')?.animate([{opacity:.2,filter:'brightness(2)',transform:'scale(.85)'},{opacity:1,filter:'brightness(1)',transform:'scale(1)'}],{duration:650,easing:'ease-out'}); }
  }
  function action(value) {
    const [kind,arg,...rest]=value.split(':'), p=(state().owned||[]).find(p=>String(p.id)===ui.selected);
    if (kind==='tab') {ui.tab=arg;ui.release=null;render(true);return;}
    if (kind==='release-arm') {ui.release=ui.selected;render(true);return;}
    if (kind==='release-cancel') {ui.release=null;render(true);return;}
    if (kind==='hatch') return send(`hatch/${encodeURIComponent(rest.join(':'))}?slot=${num(arg)}`);
    if (kind==='now'||kind==='collect') return send(`hatch/${kind}?slot=${num(arg)}`);
    if (!p) return;
    const base=`pet/${encodeURIComponent(p.id)}/`;
    if (kind==='feed') return send(base+`feed?n=${num(arg)}`);
    if (kind==='slot') return send(base+`slot/${num(arg)}`);
    if (kind==='evolve'&&p.canEvolve) return send(base+'evolve');
    if (kind==='release-confirm'&&ui.release===String(p.id)) return send(base+'release');
  }
  function init() {
    const root=document.getElementById('pane-pet');
    if (root&&!root.dataset.petBound) {
      root.dataset.petBound='1';
      root.addEventListener('click',e=>{const b=e.target.closest('[data-pet-action],[data-pet-select]');if(!b||b.disabled)return;if(b.hasAttribute('data-pet-select')){ui.selected=b.dataset.petSelect;ui.release=null;render(true);}else action(b.dataset.petAction);});
      root.addEventListener('change',e=>{const key=e.target.dataset.petFilter;if(key){ui[key]=e.target.value;ui.release=null;render(true);}});
      root.addEventListener('error',e=>{if(e.target.tagName==='IMG'){e.target.hidden=true;e.target.parentElement.classList.add('pet-missing');}},true);
    }
    if (!ui.timer) ui.timer=setInterval(tick,1000);
  }
  function render(force = false) {
    init();
    if (typeof S==='undefined'||!S) return;
    if (ui.state!==S.pet) {ui.state=S.pet;ui.received=typeof recvAt==='number'&&recvAt>0?recvAt:performance.now();}
    tick();
    const root=document.getElementById('pane-pet'); if(!root)return;
    const signature=JSON.stringify([S.pet,S.inventory,DATA?.petDefs,ui.tab,ui.selected,ui.element,ui.rarity,ui.sort,ui.release,ui.pending,ui.error]);
    if (!force&&signature===ui.signature)return;
    // A server sync must not steal focus from an open filter or keyboard navigation.
    const focused=root.contains(document.activeElement)?document.activeElement:null;
    if (!force&&focused?.matches('select')) return;
    const focusKey=focused?.dataset.petAction, focusSelect=focused?.dataset.petSelect;
    const scrolls=[...root.querySelectorAll('.pet-detail,.pet-cards,.pet-body')].map(el=>[el.className,el.scrollTop]);
    ui.signature=signature;
    root.innerHTML=`<div class="pet-ui"><header class="pet-header"><div><h2>Linh thú</h2><p>Duyên lành đồng hành</p></div><span>${f((state().owned||[]).length)} linh thú</span></header><nav class="pet-tabs" aria-label="Linh thú">${[['stable','Chuồng thú'],['hatch','Ấp trứng'],['codex','Thú điển']].map(([k,label])=>`<button type="button" data-pet-action="tab:${k}" aria-current="${ui.tab===k?'page':'false'}" class="${ui.tab===k?'active':''}">${label}</button>`).join('')}</nav><div class="pet-status" role="${ui.error?'alert':'status'}">${ui.error?esc(ui.error):ui.pending?'Đang chờ máy chủ…':''}</div><div class="pet-body">${ui.tab==='hatch'?hatch():ui.tab==='codex'?codex():stable()}</div></div>`;
    scrolls.forEach(([cls,top])=>{const el=root.getElementsByClassName(cls)[0];if(el)el.scrollTop=top;});
    if(focusKey||focusSelect) [...root.querySelectorAll('button')].find(el=>focusKey?el.dataset.petAction===focusKey:el.dataset.petSelect===focusSelect)?.focus({preventScroll:true});
    tick();
  }
  window.initPet=init;
  window.renderPet=render;
})();
