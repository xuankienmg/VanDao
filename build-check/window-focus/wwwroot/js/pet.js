/* Linh thú: server-owned state; no local simulation of pets or rewards. */
(() => {
  'use strict';
  const ui = { tab: 'stable', selected: null, element: '', rarity: '', sort: 'level', egg: '', codexElement: '', codexRarity: '', codexOwned: '', release: null, multi: false, picked: new Set(), multiArm: false, merge: null, star: null, starAll: false, pending: false, error: '', signature: '', state: null, received: 0, timer: null };
  let notice=null,noticeTimer=null;
  function noticeMarkup(){return `<div id="petNotice" class="pet-notice ${notice?.kind==='bad'?'is-error':'is-success'}" role="${notice?.kind==='bad'?'alert':'status'}" aria-live="polite"${notice?'':' hidden'}>${notice?`<button type="button" class="pet-notice-close" data-pet-action="notice-close" aria-label="Đóng thông báo">×</button><b>${notice.kind==='bad'?'Chưa thực hiện được':'Thành công'}</b><span>${esc(notice.message)}</span>`:''}</div>`;}
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
  const glyphs={atk:'<path d="m5 4 12 12m-1-7 5-6-6 5m-5 7-6 6m-1-6 6 6"/>',def:'<path d="m12 3 8 3v6c0 5-8 9-8 9s-8-4-8-9V6z"/>',hp:'<path d="M12 21 4 13C-3 5 8-1 12 7c4-8 15-2 8 6z"/>',hoa:'<path d="M12 2c3 7-1 8 3 11l3-6c8 15-16 19-14 5l4-6c-1 5 1 7 2 7 2-3 0-6 2-11Z"/>',bang:'<path d="M12 2v20M3 7l18 10M3 17 21 7M8 4l4 3 4-3M8 20l4-3 4 3"/>',moc:'<path d="M4 20C0 4 12 2 21 3c1 12-6 19-17 17Zm0 0L17 7"/>',tho:'<path d="m12 2 8 10-8 10-8-10zM8 12l4-5 4 5-4 5z"/>',loi:'<path d="M13 2 4 14h7l-1 8 10-13h-8z"/>',thuy:'<path d="M12 2C9 8 4 12 4 16c0 10 16 10 16 0 0-4-5-8-8-14Z"/>',gio:'<path d="M2 8h14c8 0 4-9 0-4M2 12h17c8 0 1 10-2 5M2 16h8"/>'};
  const petIcon=key=>`<svg class="pet-symbol" viewBox="0 0 24 24" aria-hidden="true">${glyphs[key]||glyphs.tho}</svg>`;
  const image = (id, form, src, silhouette = false) => `<span class="pet-art${silhouette ? ' pet-unknown' : ''}">${paw}<img src="${esc((src || `img/pet/${encodeURIComponent(id)}_${num(form)}.png`).replace(/(img\/pet\/[^/]+_[0-2])\.png$/,'$1_hd.png'))}" alt="" loading="lazy"></span>`;
  const button = (text, action, disabled = false, cls = '') => `<button type="button" class="pet-btn ${cls}" data-pet-action="${esc(action)}"${disabled || ui.pending ? ' disabled' : ''}>${text}</button>`;
  const badge = p => `<span class="pet-badge pet-element-badge" data-element="${esc(p.element)}">${petIcon(p.element)}${esc(label(p.element))}</span><span class="pet-badge pet-quality-${quality(p.rarity)}">${petIcon('tho')}${esc(label(p.rarity))}</span>`;
  const role = p => num(p.slot) === 1 ? 'Xuất chiến' : 'Đang nghỉ';
  const power = p => num(p.atk) + num(p.def) + num(p.hp);
  const stars = p => num(p.stars) ? `<span class="pet-stars" title="${num(p.stars)}/5 sao · +${num(p.stars)*6}% chỉ số">${'★'.repeat(num(p.stars))}<i>${'★'.repeat(5-num(p.stars))}</i></span>` : '';
  const QN = ['Phàm','Linh','Huyền','Địa','Thiên'];
  /** Thanh phóng sinh hàng loạt: chọn nhanh theo phẩm (bỏ qua con xuất chiến và con đã có sao), tổng Linh Thú Đan nhận được. */
  function multiBar(owned){
    if(!ui.multi)return '';
    const pick=owned.filter(p=>ui.picked.has(String(p.id))&&!num(p.slot)), gain=pick.reduce((a,p)=>a+num(p.releaseValue),0);
    const byQ=QN.map((n,q)=>[n,q,owned.filter(p=>quality(p.rarity)===q&&!num(p.slot)&&!num(p.stars)).length]).filter(x=>x[2]);
    return `<div class="pet-multi"><div class="pet-multi-pick"><small>Chọn nhanh</small>${byQ.map(([n,q,c])=>button(`${n} ×${c}`,`pick:${q}`)).join('')}${button('Bỏ chọn','pick-clear',!pick.length)}</div>
      <div class="pet-multi-sum"><span>Đã chọn <b>${pick.length}</b> · nhận <b>${f(gain)}</b> Linh Thú Đan</span>${ui.multiArm?`${button(`Xác nhận phóng sinh ${pick.length} con`,'multi-confirm',!pick.length,'pet-danger')}${button('Hủy','multi-cancel')}`:button('Phóng sinh đã chọn','multi-arm',!pick.length,'pet-danger')}</div>
      <small class="pet-multi-note">Nhấp thẻ để chọn / bỏ. Chọn nhanh không lấy con đang xuất chiến và con đã có sao.</small></div>`;
  }
  // ---- điểm ghép: mỗi sao cần StarCost[s] con (server gửi trong S.pet.starCost); con hiến mang theo điểm của nó ----
  const starCost = () => state().starCost || [2,3,4,5,6];
  const starAt = s => starCost().slice(0, Math.max(0, Math.min(5, s))).reduce((a,b)=>a+b, 0);
  const starsFrom = pts => { let s = 0; while (s < 5 && pts >= starAt(s+1)) s++; return s; };
  const pts = p => num(p.starPts);
  const progress = p => { const s = starsFrom(pts(p)); return s >= 5 ? 'Tối đa 5★' : `${pts(p) - starAt(s)}/${starCost()[s]} con tới ${s+1}★`; };
  const progressBar = p => { const s = starsFrom(pts(p)); if (s >= 5) return ''; return `<div class="pet-star-progress"><progress value="${pts(p)-starAt(s)}" max="${starCost()[s]}"></progress><small>${progress(p)} · mỗi con hiến tính 1 + điểm ghép của nó</small></div>`; };
  /** Ghép sao: con cùng loài trong chuồng (trừ con đang xem và con xuất chiến), yếu nhất trước. */
  function mergeBox(p){
    const st=num(p.stars), list=(state().owned||[]).filter(x=>x.defId===p.defId&&String(x.id)!==String(p.id)&&!num(x.slot))
      .sort((a,b)=>num(a.stars)-num(b.stars)||power(a)-power(b));
    const head=`<h4>Ghép sao <small>${st}/5 ★ · ${progress(p)} · mỗi sao +6% công / thủ / huyết</small></h4>`;
    if(st>=5)return `<section class="pet-merge">${head}<p class="pet-merge-hint">Đã đạt tối đa 5 sao.</p></section>`;
    if(!list.length)return `<section class="pet-merge">${head}<p class="pet-merge-hint">Cần thêm một ${esc(p.name||definition(p).name)} khác trong chuồng để ghép.</p></section>`;
    const food=list.find(x=>String(x.id)===ui.merge);
    return `<section class="pet-merge">${head}${food?`<p class="pet-merge-hint">Hiến <b>${esc(food.name)} Lv.${f(food.level)}${num(food.stars)?' '+num(food.stars)+'★':''}</b> (${esc(label(food.rarity))}) → <b>${(()=>{const np=Math.min(starAt(5),pts(p)+1+pts(food)),ns=starsFrom(np);return ns>st?`lên ${ns}★`:`${st}★ (${np-starAt(st)}/${starCost()[st]} con tới ${st+1}★)`;})()}</b>? Con bị hiến biến mất, hoàn nửa số Linh Thú Đan đã cho nó ăn.</p><div class="pet-actions">${button('Xác nhận ghép','merge-confirm',false,'pet-primary')}${button('Hủy','merge-cancel')}</div>`
      :`<div class="pet-merge-list">${list.map(x=>`<button type="button" class="pet-merge-pick" data-pet-action="merge-arm:${esc(x.id)}"${ui.pending?' disabled':''}>${image(x.defId,x.form,x.image)}<span><b>Lv.${f(x.level)} ${esc(label(x.rarity))}</b><small>${pts(x)?`+${1+pts(x)} điểm · `:''}tư chất ${Math.round((num(x.aptAtk)+num(x.aptDef)+num(x.aptHp))/3)}</small></span></button>`).join('')}</div>`}</section>`;
  }
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
    const appearance=p.appearanceForm>=0?num(p.appearanceForm):form;
    const rail = [0,1,2].map((i) => `<div class="${i === appearance ? 'current' : i <= form ? 'done' : ''}" title="${esc(d.forms?.[i]?.skillName || '')}"><button type="button" class="pet-form-choice" data-pet-action="appearance:${i}" aria-label="Ngoại hình ${esc(d.forms?.[i]?.name||['Ấu thể','Thành thể','Thần thú'][i])}" aria-pressed="${i===appearance}"${i>form||i===appearance||ui.pending?' disabled':''}>${image(p.defId,i,d.forms?.[i]?.image,i>form)}</button><span>${i <= form ? '✓' : '🔒'}</span><b>${esc(d.forms?.[i]?.name || ['Ấu thể','Thành thể','Thần thú'][i])}</b></div>`).join('');
    const stats = [['Công','atk','aptAtk'],['Thủ','def','aptDef'],['Huyết','hp','aptHp']].map(([label,k,a]) => {
      const raw = num(p[a]), pct = Math.max(0, Math.min(100, raw <= 1 ? raw*100 : raw));
      return `<div class="pet-stat"><span>${petIcon(k)}${label}</span><strong>${f(p[k])}</strong><small>Tư chất ${Math.round(pct)}%</small><meter min="0" max="100" value="${pct}">${pct}%</meter></div>`;
    }).join('');
    const slots = [button(num(p.slot) === 1 ? 'Đang xuất chiến' : 'Xuất chiến', 'slot:1', num(p.slot) === 1, 'pet-primary')];
    if (num(p.slot)) slots.push(button('Nghỉ', 'slot:0'));
    return `<div class="pet-detail-head"><div><h3>${esc(p.name || d.name)} ${stars(p)}</h3><p>${esc(p.formName || d.forms?.[form]?.name)} · Cấp ${f(p.level)}${cap ? '/'+f(cap) : ''}</p></div><div>${badge(p)}</div></div>
      <div class="pet-portrait">${image(p.defId,form,p.image)}${num(p.slot)===1?`<div class="pet-active-emblem" role="img" aria-label="Linh thú đang xuất chiến" title="Đang xuất chiến">${petIcon('atk')}<b>Xuất chiến</b></div>`:''}<span>${esc(role(p))}</span></div>
      <div class="pet-growth"><div class="pet-exp"><h4>Cấp ${f(p.level)} <small>/ ${f(cap)}</small></h4><span>${f(p.exp)} / ${f(p.expNeed)}</span><progress value="${num(p.exp)}" max="${Math.max(1,num(p.expNeed))}"></progress></div><h4>Tiến hóa</h4><div class="pet-evolution" aria-label="Tiến trình tiến hóa">${rail}</div></div>
      <div class="pet-stats">${stats}</div>
      <section class="pet-bonus"><h4>Cộng cho chủ nhân</h4><div><span>${petIcon("atk")}Công <b>+${f(p.shareAtk)}</b></span><span>${petIcon("def")}Thủ <b>+${f(p.shareDef)}</b></span><span>${petIcon("hp")}Huyết <b>+${f(p.shareHp)}</b></span></div><small>${num(p.slot) ? 'Phần cộng hiện tại do máy chủ tính.' : 'Cho xuất chiến hoặc hộ trận để nhận phần cộng.'}</small></section>
      <section class="pet-lore"><h4>Thiên phú</h4><p>${esc(p.talentText || d.talentText || 'Chưa có thông tin.')}</p><h4 class="pet-skill-summary"><span>${esc(d.forms?.[form]?.skillName || 'Kỹ năng')}</span><small>Thi triển mỗi ${f(p.skillEvery || d.skillEvery)} lượt</small></h4><p class="pet-skill-description">${esc(p.skillText || d.skillText || 'Chưa có thông tin kỹ năng.')}</p></section>
      <div class="pet-actions">${slots.join('')}</div>
      <section class="pet-feed"><h4>Cho ăn <small>Linh Thú Đan: ${f(food)}${maxed ? ' · Đã đạt giới hạn cấp' : ''}</small></h4><div class="pet-actions">${button('×1','feed:1',!food||maxed)}${button('×10','feed:10',food<10||maxed)}${button('Tối đa','feed:0',!food||maxed)}</div></section>
      <section class="pet-evolve"><h4>Tiến hóa</h4><p class="pet-evolve-hint">${esc(p.evolveHint || (form>=2?'Đã mở hình thái cuối.':form===0?'Yêu cầu: linh thú cấp 30 và đủ nguyên liệu.':'Yêu cầu: linh thú cấp 60, nhân vật Nguyên Anh và đủ nguyên liệu.'))}</p><div class="pet-costs">${(p.evolveCost || []).map(c => `<span class="${num(c.have)<num(c.need)?'pet-short':''}">${esc(ITEM[c.item]?.name || c.item)} <b>${f(c.have)}/${f(c.need)}</b></span>`).join('')}</div>${form>0?'<small>Chọn hình thái đã mở ở trên để đổi ngoại hình, giữ nguyên sức mạnh.</small>':''}${button(form>=2?'Đã tiến hóa tối đa':'Tiến hóa','evolve',form>=2,'pet-primary')}</section>
      <section class="pet-merge"><h4>Ghép sao <small>${num(p.stars)}/5 ★ · ${(state().owned||[]).filter(x=>x.defId===p.defId&&String(x.id)!==String(p.id)&&!num(x.slot)).length} con cùng loài có thể hiến</small></h4>${button('Mở Ghép sao',`star-open:${esc(p.id)}`)}</section>
      <div class="pet-release">${ui.release === String(p.id) ? `<p>Phóng sinh ${esc(p.name || d.name)}? Linh thú sẽ rời chuồng, nhận <b>${f(p.releaseValue)}</b> Linh Thú Đan.</p>${button('Xác nhận phóng sinh','release-confirm',false,'pet-danger')}${button('Giữ lại','release-cancel')}` : button(`Phóng sinh · +${f(p.releaseValue)} Linh Thú Đan`,'release-arm',false,'pet-text')}</div>`;
  }
  function layoutStable(root) {
    const collection=root.querySelector('.pet-collection'), detail=root.querySelector('.pet-detail');
    if(!collection||!detail)return;
    collection.querySelector('h3').textContent='Chuồng thú';
    collection.querySelectorAll('.pet-card').forEach(card=>{const p=(state().owned||[]).find(p=>String(p.id)===card.dataset.petSelect);if(p)card.dataset.element=p.element;});
    const stats=detail.querySelector('.pet-stats');
    if(!stats)return;
    const aptitude=document.createElement('section');aptitude.className='pet-aptitude';aptitude.innerHTML='<h4>Tư chất</h4>';
    const attributes=document.createElement('section');attributes.className='pet-attributes';attributes.innerHTML='<h4>Thuộc tính</h4>';
    stats.querySelectorAll('.pet-stat').forEach(stat=>{
      const name=stat.querySelector('span'),value=stat.querySelector('strong'),pct=stat.querySelector('small'),meter=stat.querySelector('meter');
      const row=document.createElement('div');row.className='pet-attribute-row';row.append(name,value);attributes.append(row);
      pct.textContent=Math.round(num(meter.value))+'%';
      const apt=document.createElement('div');apt.className='pet-aptitude-row';apt.append(name.cloneNode(true),meter,pct);aptitude.append(apt);
    });
    stats.replaceChildren(attributes,aptitude);
    const actions=detail.querySelector('.pet-actions'),feed=detail.querySelector('.pet-feed .pet-actions'),evolve=detail.querySelector('.pet-evolve');
    feed?.querySelectorAll('button').forEach(b=>{b.textContent='Cho ăn '+b.textContent;actions.append(b);});
    const evolveButton=evolve?.querySelector('button');if(evolveButton){evolveButton.title=evolve.querySelector('.pet-evolve-hint')?.textContent||'';actions.append(evolveButton);}
    const lore=detail.querySelector('.pet-lore');
    if(lore){
      const talent=lore.querySelector('p'),bonus=detail.querySelector('.pet-bonus');
      if(talent&&bonus){const label=document.createElement('span');label.className='pet-talent';label.textContent='✦ Thiên phú: '+talent.textContent;bonus.querySelector('div').append(label);talent.remove();lore.querySelector('h4')?.remove();bonus.querySelector('small')?.remove();}
      lore.insertAdjacentHTML('afterbegin','<h4 class="pet-skill-title">Kỹ năng linh thú</h4>');
    }
    const selected=(state().owned||[]).find(p=>String(p.id)===ui.selected);
    detail.querySelector('.pet-detail-head>div:last-child')?.insertAdjacentHTML('beforeend',`<span class="pet-badge pet-form-badge" data-form="${num(selected?.form)}">${paw}${esc(selected?.formName||['Ấu thể','Thành thể','Thần thú'][num(selected?.form)])}</span>`);
  }
  /** Tab Ghép sao: trái = các loài (loài có nhiều con trước), chọn con chính; phải = sao, chỉ số trước/sau, con hiến, ghép lẻ hoặc ghép hết. */
  function starTab(){
    const owned=state().owned||[], groups={};
    owned.forEach(p=>(groups[p.defId]=groups[p.defId]||[]).push(p));
    const species=Object.values(groups).map(g=>g.sort((a,b)=>num(b.stars)-num(a.stars)||num(b.level)-num(a.level)||power(b)-power(a)))
      .sort((a,b)=>b.length-a.length||quality(b[0].rarity)-quality(a[0].rarity));
    if(!owned.some(p=>String(p.id)===ui.star)){const g=species.find(g=>g.length>1&&num(g[0].stars)<5)||species[0];ui.star=g?String(g[0].id):null;ui.merge=null;ui.starAll=false;}
    const main=owned.find(p=>String(p.id)===ui.star);
    const left=`<section class="pet-star-list"><div class="pet-section-heading"><h4>Chọn linh thú chính</h4><small>Loài có nhiều con nằm trên</small></div>${species.map(g=>`<div class="pet-star-group"><p><b>${esc(g[0].name)}</b><small>${g.length} con${g.length>1?' · có thể ghép':''}</small></p><div class="pet-star-row">${g.map(p=>`<button type="button" class="pet-star-pick${String(p.id)===ui.star?' on':''}" data-pet-action="star-main:${esc(p.id)}" aria-pressed="${String(p.id)===ui.star}"${ui.pending?' disabled':''}>${image(p.defId,p.form,p.image)}<span><b>Lv.${f(p.level)} ${esc(label(p.rarity))}</b>${num(p.stars)?stars(p):'<small>0★</small>'}${num(p.slot)?'<small class="pet-star-active">Xuất chiến</small>':''}</span></button>`).join('')}</div></div>`).join('')||'<p class="pet-merge-hint">Chuồng đang trống.</p>'}</section>`;
    if(!main)return `<div class="pet-star">${left}<section class="pet-star-main"><div class="pet-empty">${paw}<h3>Chưa có linh thú</h3></div></section></div>`;
    const st=num(main.stars), next=Math.min(5,st+1), k=(1+next*.06)/(1+st*.06);
    const foods=owned.filter(x=>x.defId===main.defId&&String(x.id)!==String(main.id)&&!num(x.slot));
    // giống server (MergeAll): hiến con ít sao, cấp thấp, tư chất thấp trước, dừng khi đủ 5★
    const order=foods.slice().sort((a,b)=>pts(a)-pts(b)||num(a.level)-num(b.level)||(num(a.aptAtk)+num(a.aptDef)+num(a.aptHp))-(num(b.aptAtk)+num(b.aptDef)+num(b.aptHp)));
    let np=pts(main),need=0;for(const x of order){if(np>=starAt(5))break;np=Math.min(starAt(5),np+1+pts(x));need++;}
    const reach=starsFrom(np);
    const row=(n,key)=>`<div class="pet-star-stat"><span>${petIcon(key)}${n}</span><b>${f(main[key])}</b>${st<5?`<i>→</i><b class="up">${f(Math.round(num(main[key])*k))}</b>`:''}</div>`;
    const right=`<section class="pet-star-main"><div class="pet-star-head">${image(main.defId,main.form,main.image)}<div><h3>${esc(main.name)}</h3><p>${esc(label(main.rarity))} · ${esc(main.formName||'')} · Cấp ${f(main.level)}</p><div class="pet-star-big">${'★'.repeat(st)}<i>${'★'.repeat(5-st)}</i></div><small>Mỗi sao +6% công / thủ / huyết · ${st}/5 sao (+${st*6}%) · cần ${starCost().join(' / ')} con cho từng sao</small>${progressBar(main)}</div></div>
      <div class="pet-star-stats"><h4>${st<5?`Chỉ số khi lên ${next}★`:'Chỉ số (đã tối đa 5★)'}</h4>${row('Công','atk')}${row('Thủ','def')}${row('Huyết','hp')}</div>
      ${mergeBox(main)}
      ${st<5&&foods.length?`<div class="pet-star-all">${ui.starAll?`<p class="pet-merge-hint">Hiến <b>${need}</b> con ${esc(main.name)} yếu nhất (không lấy con xuất chiến) để đạt <b>${reach}★</b>${reach<5?` (${np-starAt(reach)}/${starCost()[reach]} con tới ${reach+1}★)`:''}?</p>${button('Xác nhận ghép hết','star-all-confirm',false,'pet-primary')}${button('Hủy','star-all-cancel')}`:button(`Ghép hết ${need} con · ${reach>st?`lên ${reach}★`:`${st}★ (${np-starAt(st)}/${starCost()[st]})`}`,'star-all-arm',false,'pet-primary')}</div>`:''}
    </section>`;
    return `<div class="pet-star">${left}${right}</div>`;
  }
  function stable() {
    const owned = state().owned || [], list = owned.filter(p => (!ui.element || p.element === ui.element) && (!ui.rarity || p.rarity === ui.rarity));
    list.sort((a,b) => ui.sort === 'name' ? String(a.name).localeCompare(String(b.name),'vi') : ui.sort === 'power' ? power(b)-power(a) : ui.sort === 'rarity' ? quality(b.rarity)-quality(a.rarity) : num(b.level)-num(a.level));
    if (!list.some(p => String(p.id) === ui.selected)) { ui.selected = list.length ? String(list[0].id) : null; ui.release = null; }
    const options = (key,caption,value) => `<select data-pet-filter="${key}" aria-label="${caption}"><option value="">${caption}</option>${[...new Set(owned.map(p=>p[key]).filter(Boolean))].map(v=>`<option${v===value?' selected':''} value="${esc(v)}">${esc(label(v))}</option>`).join('')}</select>`;
    return `<div class="pet-stable"><section class="pet-collection"><div class="pet-collection-head"><h3>Linh thú của bạn</h3><span>${owned.length}/${f(state().capacity)}</span>${button(ui.multi?'Xong':'Phóng sinh hàng loạt','multi',false,'pet-text pet-multi-toggle')}</div>${multiBar(owned)}<div class="pet-filters">${options('element','Mọi hệ',ui.element)}${options('rarity','Mọi phẩm',ui.rarity)}<select data-pet-filter="sort" aria-label="Sắp xếp">${[['level','Cấp cao'],['power','Chỉ số cao'],['rarity','Phẩm cao'],['name','Tên']].map(([k,v])=>`<option value="${k}"${ui.sort===k?' selected':''}>${v}</option>`).join('')}</select></div><div class="pet-cards">${list.map(p=>`<button type="button" class="pet-card${String(p.id)===ui.selected&&!ui.multi?' selected':''}${ui.multi&&ui.picked.has(String(p.id))?' picked':''}${ui.multi&&num(p.slot)?' unpickable':''}" data-pet-select="${esc(p.id)}" aria-pressed="${String(p.id)===ui.selected}"><span class="pet-element-mark" title="${esc(label(p.element))}">${petIcon(p.element)}</span><span class="pet-card-level">Lv.${f(p.level)}</span>${stars(p)}${image(p.defId,p.form,p.image)}<strong>${esc(p.name || definition(p).name)}</strong><small>${esc(label(p.element))} · ${esc(label(p.rarity))} · Cấp ${f(p.level)}</small><span class="pet-slot${num(p.slot)?' active':''}">${esc(role(p))}</span></button>`).join('') || `<p class="pet-list-empty">${owned.length ? 'Không có linh thú khớp bộ lọc.' : 'Chuồng đang trống. Hãy ấp Thú Đản đầu tiên.'}</p>`}</div></section><section class="pet-detail" aria-label="Chi tiết linh thú">${list.length || !owned.length ? detail(list.find(p=>String(p.id)===ui.selected)) : '<div class="pet-empty"><p>Đổi bộ lọc để chọn linh thú.</p></div>'}</section></div>`;
  }
  function hatch() {
    const p = state(), occupied = (p.hatch || []).filter(h=>h.egg), count = Math.max(num(p.hatchSlots),occupied.length), full = (p.owned || []).length >= num(p.capacity);
    const eggs = Object.entries(S.inventory || {}).filter(([id,n])=>n>0 && (ITEM[id]?.type === 'egg' || id.startsWith('thu_dan_')));
    if(!eggs.some(([id])=>id===ui.egg))ui.egg=eggs[0]?.[0]||'';
    const eggArt=id=>`<img src="${esc(ITEM[id]?.icon||`img/pet/egg_${id.replace('thu_dan_','')}.png`)}" alt="">`;
    const ready=occupied.filter(h=>remaining(h)===0).length;
    return `<div class="pet-hatch"><header class="pet-page-heading"><div><span class="pet-eyebrow">DƯỠNG DỤC LINH THÚ</span><h3>Ấp trứng</h3><p>Gieo một mối duyên, đón bạn đồng hành.</p></div><div class="pet-summary"><span><b>${occupied.length}/${count}</b> ổ đang ấp</span><span><b>${ready}</b> có thể nhận</span><span><b>${(p.owned||[]).length}/${f(p.capacity)}</b> chuồng thú</span></div></header><div class="pet-hatch-layout"><section class="pet-incubator"><div class="pet-section-heading"><h4>Đài ấp linh thú</h4><small>Tiếp tục ấp khi bạn rời đi</small></div><div class="pet-nests">${Array.from({length:Math.max(2,count)},(_,slot)=>{
      const h = occupied.find(x=>num(x.slot)===slot);
      const locked=slot>=count;
      return `<section class="pet-nest ${locked?'is-locked':h?'is-incubating':'is-empty'}"><div class="pet-nest-heading"><h4>Ổ ấp ${slot+1}</h4><span>${locked?'Chưa mở':h?(remaining(h)===0?'Đã nở':'Đang ấp'):'Sẵn sàng'}</span></div>${locked?`<div class="pet-nest-stage"><span class="pet-nest-lock">♙</span></div><b>Ổ ấp chưa mở</b><small>Tăng cảnh giới để mở thêm ổ ấp.</small>`:h ? `<div class="pet-nest-stage"><div class="pet-egg-art">${eggArt(h.egg)}</div></div><b>${esc(ITEM[h.egg]?.name || h.egg)}</b><p class="pet-countdown" data-pet-timer="${slot}"></p><div class="pet-nest-actions">${button('Nhận linh thú',`collect:${slot}`,remaining(h)>0||full,'pet-primary pet-collect')}${button(`Ấp ngay · ${f(h.instantCost)} linh thạch`,`now:${slot}`,remaining(h)===0||num(S.inventory?.linh_thach)<num(h.instantCost),'pet-now')}</div>${full?'<small class="pet-short">Chuồng đã đầy. Phóng sinh để có chỗ nhận.</small>':''}` : `<div class="pet-nest-stage">${ui.egg?`<div class="pet-egg-art pet-egg-preview">${eggArt(ui.egg)}</div>`:paw}</div><b>${ui.egg?esc(ITEM[ui.egg]?.name||ui.egg):'Chờ một mối duyên'}</b><small>${ui.egg?'Trứng đã chọn từ kho bên cạnh':'Nhận Thú Đản từ rương Boss hoặc Quay Linh Thú.'}</small>${button('Bắt đầu ấp',`hatch:${slot}:${ui.egg}`,full||!ui.egg,'pet-primary')}`}</section>`;
    }).join('')}</div><p class="pet-hatch-note">Chọn trứng trong kho, sau đó chọn ổ trống để bắt đầu ấp. Linh thú mới nhận sẽ xuất hiện trong Chuồng thú.</p></section><aside class="pet-egg-vault"><div class="pet-section-heading"><h4>Thú Đản trong túi</h4><span>${f(eggs.reduce((sum,[,n])=>sum+num(n),0))} trứng</span></div><div class="pet-egg-list">${eggs.map(([id,n])=>`<button type="button" class="pet-egg-choice ${ui.egg===id?'selected':''}" data-pet-action="egg:${esc(id)}" aria-pressed="${ui.egg===id}">${eggArt(id)}<span><b>${esc(ITEM[id]?.name||id)}</b><small>${ui.egg===id?'Đang chọn':'Chọn để đặt vào ổ'}</small></span><strong>×${f(n)}</strong></button>`).join('')||'<div class="pet-vault-empty"><span>◇</span><b>Kho trứng đang trống</b><p>Nhận Thú Đản từ rương Boss hoặc Quay Linh Thú.</p></div>'}</div><div class="pet-vault-help"><h4>Một hành trình mới</h4><p>Mỗi Thú Đản mang đến một linh thú. Giữ chỗ trống trong chuồng để nhận khi trứng nở.</p></div></aside></div></div>`;
  }
  function codex() {
    const owned = state().owned || [], known = new Set(owned.map(p=>p.defId));
    const list=defs().filter(d=>(!ui.codexElement||d.element===ui.codexElement)&&(!ui.codexRarity||d.rarity===ui.codexRarity)&&(!ui.codexOwned||(ui.codexOwned==='owned')===known.has(d.id)));
    const filter=(key,caption,values)=>`<select data-pet-filter="${key}" aria-label="${caption}"><option value="">${caption}</option>${values.map(v=>`<option value="${esc(v)}"${ui[key]===v?' selected':''}>${esc(label(v))}</option>`).join('')}</select>`;
    return `<div class="pet-codex"><header class="pet-page-heading"><div><span class="pet-eyebrow">VẠN LINH ĐỒ GIÁM</span><h3>Thú điển</h3><p>Khám phá các loài linh thú và ba hình thái trưởng thành.</p></div><div class="pet-codex-progress"><b>${known.size}<small> / ${defs().length}</small></b><span>loài đang sở hữu</span><progress value="${known.size}" max="${Math.max(1,defs().length)}"></progress></div></header><div class="pet-codex-toolbar">${filter('codexElement','Tất cả hệ',[...new Set(defs().map(d=>d.element))])}${filter('codexRarity','Tất cả phẩm',[...new Set(defs().map(d=>d.rarity))])}<select data-pet-filter="codexOwned" aria-label="Trạng thái sở hữu"><option value="">Tất cả trạng thái</option><option value="owned"${ui.codexOwned==='owned'?' selected':''}>Đang sở hữu</option><option value="missing"${ui.codexOwned==='missing'?' selected':''}>Chưa sở hữu</option></select><span>${list.length} loài</span></div><div class="pet-species-grid">${list.map(d=>`<section class="pet-species ${known.has(d.id)?'is-known':'is-undiscovered'}"><header><div><h4>${esc(d.name)}</h4><div class="pet-species-badges">${badge(d)}</div></div><span class="pet-discovery">${known.has(d.id)?'✓ Đang sở hữu':'Chưa sở hữu'}</span></header><div class="pet-forms">${[0,1,2].map(i=>{const unlocked=owned.some(p=>p.defId===d.id&&num(p.form)>=i);return `<div class="${unlocked?'unlocked':'locked'}">${image(d.id,i,d.forms?.[i]?.image,!unlocked)}<span>${esc(d.forms?.[i]?.name || ['Ấu thể','Thành thể','Thần thú'][i])}</span><small>${unlocked?'✓ Đã đạt':'Chưa đạt'}</small></div>`;}).join('')}</div><footer><p>${esc(d.desc || d.role || 'Ấp Thú Đản để kết duyên cùng linh thú này.')}</p>${d.talentText?`<span class="pet-species-talent">✦ ${esc(d.talentText)}</span>`:''}</footer></section>`).join('')||'<div class="pet-vault-empty"><b>Không có loài phù hợp</b><p>Thử thay đổi bộ lọc để xem thêm linh thú.</p></div>'}</div></div>`;
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
    const previous=new Set((state().owned||[]).map(p=>p.id));let hatched=false,message='';
    clearTimeout(noticeTimer);notice=null;
    ui.pending=true; busy=true; ui.error=''; render(true);
    try { const res=await api('POST',`/api/players/${encodeURIComponent(S.id)}/${path}`); setState(res.state); notifyHost('changed'); ui.release=null;message=res.message||'Đã cập nhật linh thú.';
      if(path.startsWith('hatch/collect')){const born=(state().owned||[]).find(p=>!previous.has(p.id));if(born){ui.tab='stable';ui.selected=String(born.id);hatched=true;}}
    }
    catch(e) { ui.error=e.message || 'Không kết nối được máy chủ. Hãy thử lại.'; }
    finally { ui.pending=false; busy=false;notice={message:ui.error||message,kind:ui.error?'bad':'good'}; render(true);noticeTimer=setTimeout(()=>{notice=null;const el=document.getElementById('petNotice');if(el)el.hidden=true;},ui.error?12000:6000);if(hatched&&!matchMedia('(prefers-reduced-motion: reduce)').matches)document.querySelector('#pane-pet .pet-portrait')?.animate([{opacity:.2,filter:'brightness(2)',transform:'scale(.85)'},{opacity:1,filter:'brightness(1)',transform:'scale(1)'}],{duration:650,easing:'ease-out'}); }
  }
  // Reuse existing images, controls and scroll containers when server values change.
  // Keys also keep a pet's card intact when feeding changes the level sort order.
  function reconcile(target, source) {
    const key=node=>node.nodeType===1?(node.id||['petSelect','petAction','petFilter'].map(k=>node.dataset[k]?k+':'+node.dataset[k]:'').find(Boolean)||''):'';
    [...source.childNodes].forEach((next,index)=>{
      const nextKey=key(next),at=target.childNodes[index];
      let current=nextKey?[...target.childNodes].find(n=>key(n)===nextKey):at;
      if(!current||current.nodeType!==next.nodeType||current.nodeName!==next.nodeName||(!nextKey&&key(current))){
        target.insertBefore(next.cloneNode(true),at||null);return;
      }
      if(current!==at)target.insertBefore(current,at||null);
      if(next.nodeType!==1){if(current.nodeValue!==next.nodeValue)current.nodeValue=next.nodeValue;return;}
      [...current.attributes].forEach(a=>{if(!next.hasAttribute(a.name))current.removeAttribute(a.name);});
      [...next.attributes].forEach(a=>{if(current.getAttribute(a.name)!==a.value)current.setAttribute(a.name,a.value);});
      reconcile(current,next);
    });
    while(target.childNodes.length>source.childNodes.length)target.lastChild.remove();
  }
  function action(value) {
    if(value==='notice-close'){
      clearTimeout(noticeTimer);notice=null;
      const el=document.getElementById('petNotice');if(el)el.hidden=true;
      document.querySelector('#pane-pet .pet-actions [data-pet-action="evolve"]:not(:disabled),#pane-pet .pet-actions button:not(:disabled)')?.focus({preventScroll:true});
      return;
    }
    const [kind,arg,...rest]=value.split(':'), p=(state().owned||[]).find(p=>String(p.id)===ui.selected);
    if (kind==='tab') {ui.tab=arg;ui.release=null;render(true);return;}
    if (kind==='egg') {ui.egg=arg;render(true);return;}
    if (kind==='release-arm') {ui.release=ui.selected;render(true);return;}
    if (kind==='release-cancel') {ui.release=null;render(true);return;}
    if (kind==='multi') {ui.multi=!ui.multi;ui.picked.clear();ui.multiArm=false;render(true);return;}
    if (kind==='pick') {(state().owned||[]).filter(x=>quality(x.rarity)===num(arg)&&!num(x.slot)&&!num(x.stars)).forEach(x=>ui.picked.add(String(x.id)));ui.multiArm=false;render(true);return;}
    if (kind==='pick-clear') {ui.picked.clear();ui.multiArm=false;render(true);return;}
    if (kind==='multi-arm') {ui.multiArm=true;render(true);return;}
    if (kind==='multi-cancel') {ui.multiArm=false;render(true);return;}
    if (kind==='multi-confirm') {const ids=(state().owned||[]).filter(x=>ui.picked.has(String(x.id))&&!num(x.slot)).map(x=>x.id);ui.picked.clear();ui.multiArm=false;if(ids.length)return send(`pet/release-many?ids=${encodeURIComponent(ids.join(','))}`);render(true);return;}
    if (kind==='merge-arm') {ui.merge=[arg,...rest].join(':');render(true);return;}
    if (kind==='merge-cancel') {ui.merge=null;render(true);return;}
    if (kind==='star-open') {ui.tab='star';ui.star=[arg,...rest].join(':');ui.merge=null;ui.starAll=false;render(true);return;}
    if (kind==='star-main') {ui.star=[arg,...rest].join(':');ui.merge=null;ui.starAll=false;render(true);return;}
    if (kind==='star-all-arm') {ui.starAll=true;ui.merge=null;render(true);return;}
    if (kind==='star-all-cancel') {ui.starAll=false;render(true);return;}
    if (kind==='star-all-confirm'&&ui.star) {ui.starAll=false;return send(`pet/${encodeURIComponent(ui.star)}/merge-all`);}
    if (kind==='hatch') return send(`hatch/${encodeURIComponent(rest.join(':'))}?slot=${num(arg)}`);
    if (kind==='now'||kind==='collect') return send(`hatch/${kind}?slot=${num(arg)}`);
    if (kind==='merge-confirm'&&ui.merge&&ui.tab==='star'&&ui.star){const food=ui.merge;ui.merge=null;return send(`pet/${encodeURIComponent(ui.star)}/merge/${encodeURIComponent(food)}`);}
    if (!p) return;
    const base=`pet/${encodeURIComponent(p.id)}/`;
    if (kind==='feed') return send(base+`feed?n=${num(arg)}`);
    if (kind==='slot') return send(base+`slot/${num(arg)}`);
    if (kind==='evolve'&&num(p.form)<2) return send(base+'evolve');
    if (kind==='appearance') return send(base+`appearance/${num(arg)}`);
    if (kind==='release-confirm'&&ui.release===String(p.id)) return send(base+'release');
    if (kind==='merge-confirm'&&ui.merge){const food=ui.merge;ui.merge=null;const mainId=ui.tab==='star'&&ui.star?ui.star:p.id;return send(`pet/${encodeURIComponent(mainId)}/merge/${encodeURIComponent(food)}`);}
  }
  function init() {
    const root=document.getElementById('pane-pet');
    if (root&&!root.dataset.petBound) {
      root.dataset.petBound='1';
      root.addEventListener('click',e=>{const b=e.target.closest('[data-pet-action],[data-pet-select]');if(!b||b.disabled)return;if(b.hasAttribute('data-pet-select')){const id=b.dataset.petSelect;if(ui.multi){const x=(state().owned||[]).find(p=>String(p.id)===id);if(x&&!num(x.slot)){ui.picked.has(id)?ui.picked.delete(id):ui.picked.add(id);ui.multiArm=false;}render(true);return;}ui.selected=id;ui.release=null;ui.merge=null;render(true);}else action(b.dataset.petAction);});
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
    root.setAttribute('aria-busy',String(ui.pending));
    if(ui.pending){
      root.querySelectorAll('.pet-btn,.pet-egg-choice').forEach(b=>{b.disabled=true;});
      return;
    }
    const signature=JSON.stringify([S.pet,S.inventory,DATA?.petDefs,ui.tab,ui.selected,ui.element,ui.rarity,ui.sort,ui.egg,ui.codexElement,ui.codexRarity,ui.codexOwned,ui.release,ui.multi,[...ui.picked],ui.multiArm,ui.merge,ui.star,ui.starAll,ui.pending,ui.error]);
    if (!force&&signature===ui.signature)return;
    // A server sync must not steal focus from an open filter or keyboard navigation.
    const focused=root.contains(document.activeElement)?document.activeElement:null;
    if (!force&&focused?.matches('select')) return;
    const focusKey=focused?.dataset.petAction, focusSelect=focused?.dataset.petSelect;
    const scrolls=[...root.querySelectorAll('.pet-detail,.pet-cards,.pet-body')].map(el=>[el.className,el.scrollTop]);
    ui.signature=signature;
    const next=document.createElement('div');
    next.innerHTML=`<div class="pet-ui"><div class="pet-top"><header class="pet-header"><span class="pet-heading-icon"><img src="img/header/pet-emblem.png" alt=""></span><div><h2>Linh thú</h2><p>Duyên lành đồng hành</p></div><span>${f((state().owned||[]).length)} linh thú</span></header><nav class="pet-tabs" aria-label="Linh thú">${[['stable','Chuồng thú'],['star','Ghép sao'],['hatch','Ấp trứng'],['codex','Thú điển']].map(([k,label])=>`<button type="button" data-pet-action="tab:${k}" aria-current="${ui.tab===k?'page':'false'}" class="${ui.tab===k?'active':''}">${label}</button>`).join('')}</nav></div><div class="pet-status" role="${ui.error?'alert':'status'}">${ui.error?esc(ui.error):''}</div><div class="pet-body">${ui.tab==='hatch'?hatch():ui.tab==='codex'?codex():ui.tab==='star'?starTab():stable()}</div></div>`;
    if(ui.tab==='stable')layoutStable(next);
    next.firstElementChild.insertAdjacentHTML('beforeend',noticeMarkup());
    reconcile(root,next);
    scrolls.forEach(([cls,top])=>{const el=root.getElementsByClassName(cls)[0];if(el)el.scrollTop=top;});
    if(focusKey||focusSelect) [...root.querySelectorAll('button')].find(el=>focusKey?el.dataset.petAction===focusKey:el.dataset.petSelect===focusSelect)?.focus({preventScroll:true});
    tick();
  }
  window.initPet=init;
  window.renderPet=render;
})();
