/* Vấn Đạo · js/battle.js: vẽ trận đánh, pha trận, ngồi thiền. Tách từ index.html (tools/split_index.py). */
/** Viên linh thạch 4×4 (1 thân sáng · 2 điểm lóe), rơi khi hạ quái. */
const STONE_PX = ['0110', '1221', '1111', '0110'];
function drawPixelBattle(t, dt, ph, r){
  const {w, h} = pixGrid();
  if (pix.width !== w || pix.height !== h){ pix.width = w; pix.height = h; }
  const key = w + 'x' + h;
  if (pixKey !== key){ pixLayers = buildPixLayers(w, h); pixKey = key; }
  const L = pixLayers, g = L.g, B0 = S.battle, wvI = battleWaveIdx(B0, ph), wv = wvI >= 0 ? B0.waves[wvI] : null;
  // ải 3 đợt: quái, máu, sát thương hai bên theo đợt đang đánh (mảng theo lượt vẫn dùng chung chỉ số lượt)
  const b = wv ? Object.assign({}, B0, {monster: wv.name, image: wv.image, monsterHp: wv.hp, heroDmg: wv.heroDmg, monsterDmg: wv.monsterDmg}) : B0;
  const wEnd = wv ? (B0.waves[wvI + 1]?.start ?? B0.hits) : B0.hits, lastWave = !wv || wvI === B0.waves.length - 1, elite = wv?.tier === 'elite';
  const hero = sprite48(skinArt('hero')), mon = sprite48(b.image), monHi = monHD(b.image);
  if (!hero || !mon) return false;
  pctx.imageSmoothingEnabled = false;

  // ---- nền ----
  pctx.drawImage(L.bg, 0, 0);
  const off = reduce ? 0 : scrollT;
  for (const [lay, sp] of [[L.far, 6], [L.mid, 14]]) pctx.drawImage(lay.c, -Math.floor((off*sp) % lay.tile), 0);
  pctx.drawImage(L.mist, 0, 0);
  for (const m of pixMotes){                                     // bụi linh khí lơ lửng
    const y = m.y - ((t*3 + m.ph*5) % 12), x = m.x + Math.sin(m.ph + t*.7)*2;
    P(x, y, (Math.floor(m.ph*10) % 3) ? PX.cyan : PX.cyan2);
  }

  // ---- vị trí ----
  const sz = SPR(), u = sz/48, hs = sz/2, U = v => Math.max(1, Math.round(v*u));
  // ải Boss: Boss to gấp 1.5 quái thường (đã giảm 25% từ ×2; msz = ô sprite của quái, đáy vẫn chạm đất)
  const isBoss = /\/boss_/.test(b.image), ms = isBoss ? 1.5 : elite ? 1.25 : 1, msz = sz * ms, mhs = msz / 2;
  const heroX = Math.round(w*.27), wolfX = Math.round(w*(isBoss ? .72 : .70)), strikeX = wolfX - U(30) - (msz - sz) / 2;
  let hx = heroX, hy = 0, wx = null, wy = 0, wFlash = false, hFlash = false, wAlpha = 1, hAlpha = 1, sword = null;
  let hsx = 1, hsy = 1, hrot = 0;
  let monK = -1;                                                                     // pha phản đòn của quái (0→1) cho hiệu ứng nét cao                                                    // dáng tu sĩ khi tung chiêu: co giãn theo chân + nghiêng (rad)
  const fxAfter = [];                                                                 // hiệu ứng đòn quái vẽ đè lên sprite
  const dealt = n => Math.min(b.monsterHp, n * b.heroDmg);
  // máu sau mỗi lượt do server mô phỏng (đã tính chiêu thức trong ô, khiên, khống, né)
  const heroMax = b.heroHp || S.stats.hp;
  const mLeft = n => n <= (wv ? wv.start : 0) ? b.monsterHp : (b.monHpLeft ? b.monHpLeft[Math.min(n, b.monHpLeft.length) - 1] : b.monsterHp - dealt(n));
  const hLeft = n => n <= 0 ? heroMax : (b.heroHpLeft ? b.heroHpLeft[Math.min(n, b.heroHpLeft.length) - 1] : heroMax - b.monsterDmg * n);
  const castsNow = ph.name === 'fight' ? (b.casts || []).filter(c => c.turn === ph.i) : [];
  // đòn đánh mặc định là Hỏa Cầu (chưa có kiếm pháp): tu sĩ + lửa vẽ nét từ bản thiết kế ở bước sau, không vẽ kiếm pixel
  const refHero = skImg(skinArt('heroCast') || HC_REF.src);           // khung tung chiêu theo cảnh giới (cùng bố cục HC_REF)
  // nộ cuối mỗi lượt (server): lượt tung kỹ năng thì về 0
  const rageAt = n => !b.rage || n <= 0 ? 0 : b.rage[Math.min(n, b.rage.length) - 1];
  let mHp = 1, hHp = 1, rg = 0, burning = false;
  if (ph.name === 'walk'){
    const flying = skinActorFlight();
    hy = flying ? -sz * .22 + (reduce ? 0 : Math.sin(t * 3) * sz * .025)
      : reduce ? 0 : -(Math.floor(t*8) % 2);
    if (!flying && !reduce && Math.random() < dt*6) pfx.push({type:'dust', x: hx - U(4) + Math.random()*U(8), y: g - 1, vx: -12 - Math.random()*10, vy: -4, life: 0, max: .5});
  } else if (ph.name === 'enter'){
    wx = Math.round(w + mhs + 2 - (w + mhs + 2 - wolfX) * ease(ph.k));
    wy = reduce ? 0 : -(Math.floor(ph.k*10) % 2);
    sword = 'ready';
    if (wv && wvI > 0) hHp = Math.max(0, hLeft(wv.start) / heroMax);                  // đợt sau: máu tu sĩ giữ từ đợt trước
  } else if (ph.name === 'fight'){
    const i = ph.i, s = ph.s, killing = (lastWave ? b.win : true) && i === wEnd - 1, held = !killing && b.monSkip && b.monSkip[i];
    wx = wolfX;
    mHp = mLeft(i + (s >= .45 ? 1 : 0)) / b.monsterHp;
    hHp = Math.max(0, hLeft(i + (s >= .85 && !killing && !held ? 1 : 0)) / heroMax);
    rg = rageAt(i + (s >= .45 ? 1 : 0)) / RAGE_MAX;
    const skTurn = castsNow.some(c => c.dmg > 0 && c.skill !== 'hoa_cau');           // lượt tung kỹ năng: không có đòn thường
    const burnNow = b.burn > 0 && !!b.burned?.[i];                                     // thiêu đốt (Phần Cốt) kích hoạt lượt này (30%)
    burning = burnNow && s >= .45;
    if (burnNow && s >= .62){
      const bk = ph.trip + ':burn' + i;
      if (!battleDone[bk]){ battleDone[bk] = 1;
        pfx.push({type:'num', x: wolfX + U(12), y: g - msz + (MODE === 'strip' ? 2 : 6), text: '-' + b.burn, col: '#ff7a28', life: 0, max: .9});
        if (MODE !== 'strip') pfx.push({type:'label', x: wolfX + U(12), y: g - msz + 22, text: 'Thiêu đốt', col: '#ff9a50', life: 0, max: .9});
      }
    }
    // ---- hiệu ứng trang bị theo cấp (server tính sẵn theo lượt: b.bolt, b.extra, b.heal, b.reflect, b.guardLeft) ----
    const gfx = (key, at, fn) => { const k2 = ph.trip + ':' + key + i; if (s >= at && !battleDone[k2]){ battleDone[k2] = 1; fn(); } };
    const big = MODE !== 'strip';
    if (b.bolt?.[i]){                                                                  // Thiên lôi: tia sét vàng giáng xuống quái
      if (s >= .3 && s < .46) fxAfter.push(() => {
        const x0 = wolfX + U(4), top = Math.max(2, g - msz - U(60)), seg = U(8);
        let x = x0, y = top;
        while (y < g - msz * .5){ const nx = x0 + (Math.floor((y + t * 60) / seg) % 2 ? U(5) : -U(5)), ny = y + seg; pline(x, y, nx, ny, '#fff4c0', 2); pline(x + 1, y, nx + 1, ny, '#f2c94c'); x = nx; y = ny; }
      });
      gfx('bolt', .38, () => { pfx.push({type:'num', x: wolfX + U(14), y: g - msz - (big ? 18 : 6), text: '-' + b.boltDmg, col: '#f2c94c', life: 0, max: .9});
        if (big) pfx.push({type:'label', x: wolfX + U(14), y: g - msz - 30, text: 'Thiên lôi', col: '#f2c94c', life: 0, max: .9});
        for (let n = 0; n < 12; n++) pfx.push({type:'spark', x: wolfX + U(4), y: g - msz * .5, vx: (Math.random()-.5)*70*u, vy: (Math.random()-.8)*50*u, life: 0, max: .35, col: Math.random() < .5 ? '#fff4c0' : '#f2c94c'}); });
    }
    if (i === 0 && b.firstHit) gfx('dia', .47, () => { pfx.push({type:'num', x: wolfX - U(18), y: g - msz - (big ? 8 : 2), text: '-' + b.firstHit, col: '#d0a060', life: 0, max: .9});
      if (big) pfx.push({type:'label', x: wolfX - U(18), y: g - msz - 22, text: 'Địa chấn', col: '#d0a060', life: 0, max: .9}); });
    if (b.extra?.[i]) gfx('ex', .64, () => { pfx.push({type:'num', x: wolfX + U(6), y: g - msz - (big ? 34 : 12), text: '-' + (b.extraDamage?.[i] ?? b.heroDmg), critical: b.extraCritical?.[i], col: PX.dmg, life: 0, max: .8});
      if (big) pfx.push({type:'label', x: wolfX + U(6), y: g - msz - 48, text: 'Liên kích', col: PX.dmg, life: 0, max: .8}); });
    if (b.reflect?.[i]) gfx('rf', .9, () => { pfx.push({type:'num', x: wolfX - U(8), y: g - msz - (big ? 8 : 2), text: '-' + b.reflect[i], col: '#d0a060', life: 0, max: .8});
      if (big) pfx.push({type:'label', x: wolfX - U(8), y: g - msz - 22, text: 'Phản', col: '#d0a060', life: 0, max: .8}); });
    if (b.heal?.[i]) gfx('hl', .95, () => pfx.push({type:'num', x: heroX + U(10), y: g - sz - (big ? 30 : 10), text: '+' + b.heal[i], col: '#7ce08a', life: 0, max: .8}));
    const gl = !b.guard ? 0 : s >= .85 ? (b.guardLeft?.[i] ?? 0) : i > 0 ? (b.guardLeft?.[i - 1] ?? 0) : b.guard;   // Thổ thuẫn còn lại
    if (b.enrageTurn >= 0 && i >= b.enrageTurn){                                      // Boss Cuồng bạo (kỹ năng ẩn): hào quang đỏ + chữ khi vừa nổi giận
      if (i === b.enrageTurn) gfx('enr', .8, () => { if (big) pfx.push({type:'label', x: wolfX, y: g - msz - 36, text: 'Cuồng bạo!', col: '#ff5a48', life: 0, max: 1.2}); });
      fxAfter.push(() => pring(wolfX, g - msz * .45, msz * .5 + (Math.floor(t * 8) % 2), msz * .5, '#c03a30', true));
    }
    if (gl > 0) fxAfter.push(() => { const a = .5 + .5 * gl / b.guard; pring(hx, g - U(16), U(15), U(21), a > .75 ? '#d0a060' : '#8a6a3a', true); });
    castsNow.forEach((c, n) => {                                                        // tên chiêu (xếp chồng) + sát thương chiêu
      const ck = ph.trip + ':c' + i + c.skill;
      if (s >= .05 + n * .12 && !battleDone[ck]){ battleDone[ck] = 1; pfx.push({type:'label', x: heroX + (MODE === 'strip' ? U(6) : 0), y: g - sz - (MODE === 'strip' ? 2 : 14) - n * (MODE === 'strip' ? 7 : 14), text: c.name, col: '#f8d25a', life: 0, max: 1.1}); }
      const dk = ck + 'd';                                                              // Hỏa Cầu: số gộp vào đòn đánh (cùng một quả cầu)
      if (c.dmg > 0 && s >= .45 && !battleDone[dk] && !(refHero && c.skill === 'hoa_cau')){ battleDone[dk] = 1; pfx.push({type:'num', x: wolfX + U(4), y: g - msz - (MODE === 'strip' ? 16 : 40), text: '-' + c.dmg, critical: c.critical, col: '#f08a2a', life: 0, max: 1}); }
    });
    if (killing && s >= .62 && wv?.stones > 0){                                         // quái gục: linh thạch văng ra (số đúng như server cộng), rồi bay về tu sĩ
      const sk = ph.trip + ':stone' + wvI;
      if (!battleDone[sk]){ battleDone[sk] = 1;
        const nS = Math.min(6, 2 + Math.floor(Math.log10(wv.stones + 1) * 1.5)) + (wv.tier === 'boss' || isBoss ? 3 : wv.tier === 'elite' ? 1 : 0);
        for (let n = 0; n < nS; n++) pfx.push({type:'stone', x: wolfX + (Math.random() - .5) * U(10), y: g - msz * .45, vx: (Math.random() - .5) * 80 * u, vy: (-40 - Math.random() * 30) * u, g0: g, hx: heroX, u, life: 0, max: 1.25 + Math.random() * .25});
        if (MODE === 'strip') pfx.push({type:'num', x: wolfX + U(16), y: g - msz * .55, text: '+' + fmt(wv.stones), col: '#8fe6dc', life: 0, max: 1.2});
        else pfx.push({type:'label', x: wolfX + U(22), y: g - msz * .6, text: '+' + fmt(wv.stones) + ' linh thạch', col: '#8fe6dc', life: 0, max: 1.3});
      }
    }
    if (held && s >= .78){ const hk = ph.trip + ':held' + i; if (!battleDone[hk]){ battleDone[hk] = 1; pfx.push({type:'label', x: wolfX, y: g + (MODE === 'strip' ? 1 : 4), text: 'Bị khống', col: '#f08a2a', life: 0, max: .9}); } }
    if (refHero && s < .78){                                                           // thi triển Hỏa Cầu tại chỗ
      hx = heroX;
      if (!reduce){
        const e = x => x * x * (3 - 2 * x);
        if (s >= .08 && s < .3){ const k = e((s - .08) / .22);                          // lấy đà: hạ người, ngả ra sau
          hx = Math.round(heroX - U(3) * k); hy = U(1) * k; hsx = 1 + .05 * k; hsy = 1 - .07 * k; hrot = -.07 * k; }
        else if (s >= .3 && s < .4){ const k = (s - .3) / .1, p = Math.sin(k * Math.PI);  // phóng: lao người tới, duỗi thân
          hx = Math.round(heroX - U(3) * (1 - k) + U(8) * e(k)); hy = -U(2) * p; hsx = 1 - .05 * p; hsy = 1 + .07 * p; hrot = .11 * e(Math.min(1, k * 1.5)); }
        else if (s >= .4 && s < .62){ const k = e((s - .4) / .22);                     // thu thế về chỗ
          hx = Math.round(heroX + U(8) * (1 - k)); hrot = .11 * (1 - k); }
      }
      if (s >= .45 && s < .62){                                                        // cầu lửa trúng đích
        const k = (s-.45)/.17;
        wFlash = k < .4; wx = wolfX + (k < .5 ? U(3) : U(2));
        const fk = ph.trip + ':h' + i;
        if (!battleDone[fk] && !skTurn){ battleDone[fk] = 1;
          const hc = castsNow.filter(c => c.skill === 'hoa_cau' && c.dmg > 0).reduce((a, c) => a + c.dmg, 0);   // cùng một quả cầu → một con số
          pfx.push({type:'num', x: wolfX - U(6), y: g - msz - (MODE === 'strip' ? 10 : 28), text: '-' + ((b.basicDamage?.[i] ?? b.heroDmg) + hc), critical: b.basicCritical?.[i], col: hc ? '#f08a2a' : PX.dmg, life: 0, max: .9});
          const wetB = skById(b.basic)?.element === 'thuy';
          for (let n = 0; n < 16; n++) pfx.push({type:'spark', x: wolfX - U(4), y: g - U(22), vx: (Math.random()-.3)*60*u, vy: (Math.random()-.6)*50*u, life: 0, max: .35 + Math.random()*.25, col: wetB ? WP[2 + Math.floor(Math.random()*3)] : FIRE[2 + Math.floor(Math.random()*4)]});
        }
      }
    }
    else if (s < .3){ hx = heroX - 2; sword = 'raise'; }                               // lấy đà
    else if (s < .45){ hx = Math.round(heroX + (strikeX - heroX) * ease((s-.3)/.15)); sword = 'raise'; }   // lao tới
    else if (s < .62){                                                                 // chém trúng
      hx = strikeX; sword = 'slash';
      const k = (s-.45)/.17;
      pslash(wolfX - U(10), g - U(24), MODE === 'strip' ? 8 : 18, -1.9, 1.3, Math.min(1, k*2.2), k < .6 ? 1 : 1 - (k-.6)*2);
      wFlash = k < .5; wx = wolfX + (k < .5 ? U(3) : U(2));
      const fk = ph.trip + ':h' + i;
      if (!battleDone[fk] && !skTurn){ battleDone[fk] = 1;
        pfx.push({type:'num', x: wolfX - U(6), y: g - msz - (MODE === 'strip' ? 10 : 28), text: '-' + (b.basicDamage?.[i] ?? b.heroDmg), critical: b.basicCritical?.[i], col: PX.dmg, life: 0, max: .9});
        for (let n = 0; n < 16; n++) pfx.push({type:'spark', x: wolfX - U(10), y: g - U(24), vx: (Math.random()-.2)*60*u, vy: (Math.random()-.6)*50*u, life: 0, max: .35 + Math.random()*.2, col: Math.random() < .5 ? PX.cyan2 : PX.white});
      }
    }
    else if (s < .78){ hx = Math.round(strikeX + (heroX - strikeX) * ease((s-.62)/.16)); sword = 'down'; }   // lùi về
    else if (!killing && !held){                                                       // quái phản đòn: mỗi loài một kiểu (monAttack)
      const st = monAttack(monStyleOf(b.image), (s-.78)/.22, {heroX, wolfX, g, sz: msz, U, u, t, trip: ph.trip, i, fxAfter});
      monK = (s - .78) / .22;
      if (st.wx !== undefined) wx = st.wx;
      if (st.wy !== undefined) wy = st.wy;
      if (st.wAlpha !== undefined) wAlpha = st.wAlpha;
      if (s >= .85){
        hFlash = s < .93; hx = heroX - (s < .93 ? U(st.knock || 2) : 1);
        if (refHero && !reduce && s < .93){ const p = Math.sin((s - .85) / .08 * Math.PI); hsx = 1 + .05 * p; hsy = 1 - .06 * p; hrot = -.07 * p; }   // trúng đòn: co người giật lùi
        const fk = ph.trip + ':m' + i;
        if (!battleDone[fk]){ battleDone[fk] = 1;
          pfx.push({type:'num', x: heroX - U(4), y: g - sz - (MODE === 'strip' ? 8 : 26), text: '-' + (b.enrageTurn >= 0 && i >= b.enrageTurn ? b.monsterDmgEnraged : b.monsterDmg), col: st.numCol || PX.hurt, life: 0, max: .8});
          const cols = st.sparks || [PX.red];
          for (let n = 0; n < (st.nSparks || 8); n++) pfx.push({type:'spark', x: heroX + U(6), y: g - U(26), vx: (Math.random()-.5)*(st.spread || 40)*u, vy: -Math.random()*40*u, life: 0, max: .3 + Math.random()*.15, col: cols[n % cols.length]});
          if (st.label && MODE !== 'strip') pfx.push({type:'label', x: heroX, y: g - sz - 40, text: st.label, col: st.numCol || PX.hurt, life: 0, max: .9});
        }
      }
    }
  } else if (ph.name === 'win'){
    wx = wolfX; mHp = 0; wAlpha = ph.k < .33 ? .7 : ph.k < .66 ? .4 : .15;              // tan biến theo bậc
    if (refHero && !reduce && ph.k < .4){ const p = Math.sin(ph.k / .4 * Math.PI); hy = -U(3) * p; hsy = 1 + .03 * p; }   // thắng: bật nhảy nhẹ
    hHp = Math.max(0, hLeft(b.hits) / heroMax);
    const fk = ph.trip + ':win';
    if (!battleDone[fk]){ battleDone[fk] = 1;
      for (let n = 0; n < 18; n++) pfx.push({type:'spark', x: wolfX - U(14) + Math.random()*U(28), y: g - U(8) - Math.random()*U(20), vx: (Math.random()-.5)*10*u, vy: (-10 - Math.random()*18)*u, life: 0, max: 1 + Math.random()*.4, col: Math.random() < .6 ? PX.cyan : PX.cyan2}); }
  } else {                                                                             // thua: nhấp nháy, lùi lại
    wx = wolfX; mHp = mLeft(b.hits) / b.monsterHp; hHp = 0;
    hx = heroX - Math.round(U(6)*ph.k); hAlpha = Math.floor(t*10) % 2 ? .35 : .8;
    const fk = ph.trip + ':lose';
    if (!battleDone[fk]){ battleDone[fk] = 1; pfx.push({type:'label', x: heroX, y: g - sz - 8, text: 'Rút lui', col: '#e58472', life: 0, max: 1.3}); }
  }
  if (elite && wx !== null && wAlpha > .5) fxAfter.push(() => pring(wx, g - msz * .45, msz * .5 + (Math.floor(t * 6) % 2), msz * .5, '#a070e0', true));   // tinh anh: hào quang tím
  if (wv && ph.name === 'enter' && wvI > 0){                                         // quái đợt sau bước vào
    const ek = ph.trip + ':wave' + wvI;
    if (!battleDone[ek]){ battleDone[ek] = 1; if (MODE !== 'strip') pfx.push({type:'label', x: wolfX, y: g - msz - 20, text: elite ? 'Tinh anh xuất hiện!' : 'Đợt ' + (wvI + 1), col: elite ? '#c08aff' : '#f0e2bc', life: 0, max: 1.1}); }
  }
  if (Object.keys(battleDone).length > 60) battleDone = {};
  // máu "bóng": phần vừa mất hiện trắng rồi rút dần
  if (mHp > lastMHp + .01) ghostHp = mHp;
  lastMHp = mHp; ghostHp = Math.max(mHp, ghostHp - dt*.9);

  // ---- vòng mục tiêu + bóng ----
  if (wx !== null && wAlpha > .5){ pring(wx, g + 1, U(16*ms), U(3*ms), PX.red2, false); pring(wx, g + 1, U(18*ms), U(4*ms), PX.red, true); }
  pctx.fillStyle = PX.shadow;
  for (let k = -U(9); k <= U(9); k++) pctx.fillRect(hx + k, g, 1, Math.abs(k) < U(6) ? 2 : 1);
  if (wx !== null) for (let k = -U(16*ms); k <= U(16*ms); k++) pctx.fillRect(wx + k, g, 1, Math.abs(k) < U(10*ms) ? 2 : 1);

  // ---- quái ----
  if (wx !== null){
    pctx.globalAlpha = wAlpha;
    if (!monHi) pctx.drawImage(wFlash ? mon.flash : burning && mon.burn ? mon.burn : mon.im, wx - mhs, g - msz + 1 + wy, msz, msz);   // bản nét vẽ sau, ở độ phân giải thật
    pctx.globalAlpha = 1;
    if (burning) pburn(wx, g, U(15*ms), t, u);
    if (wAlpha > .5){
      const bw = Math.max(12, U(26*ms));
      pbar(wx, g - msz - 3, bw, mHp, ghostHp, PX.red, PX.red2);
      if (burning){                                                                    // biểu tượng lửa + % cạnh thanh máu quái
        const ix = Math.round(wx + bw/2) + 3, iy = g - msz - 5;
        ['010','111','121','010'].forEach((row, yy) => [...row].forEach((c, xx) => { if (c !== '0') P(ix + xx, iy + yy, c === '2' ? FIRE[4] : FIRE[3]); }));
        if (MODE !== 'strip') ptext(ix + 5, iy - 1, Math.round(b.burnPct) + '%', '#ff9a50');
      }
    }
  }
  // ---- tu sĩ + kiếm linh khí ----
  if (!refHero){
  pctx.globalAlpha = hAlpha;
  pctx.drawImage(hFlash ? hero.flash : hero.im, hx - hs, g - sz + 1 + hy);
  pctx.globalAlpha = 1;
  }
  const handX = hx + U(7), handY = g - U(22) + hy;
  if (refHero) sword = null;
  if (sword === 'raise' || sword === 'ready'){
    const n = U(sword === 'raise' ? 12 : 8), hilt = Math.max(1, U(2));
    for (let i = 0; i < n; i++){ P(handX - i, handY - i, i > hilt ? PX.cyan2 : PX.gold); if (i > hilt && u === 1) P(handX - i + 1, handY - i, PX.cyan); }
  } else if (sword === 'slash'){
    const n = U(14), hilt = Math.max(1, U(2));
    for (let i = 0; i < n; i++){ P(handX + i, handY - U(5) + Math.floor(i/3), i > hilt ? PX.white : PX.gold); if (i > hilt && u === 1) P(handX + i, handY - 6 + Math.floor(i/3), PX.cyan); }
  } else if (sword === 'down'){
    for (let i = 0; i < U(10); i++) P(handX + i, handY + Math.floor(i/2), i > Math.max(1, U(2)) ? PX.cyan2 : PX.gold);
  }
  if (ph.name !== 'walk') pbar(hx, g - sz - 3, Math.max(10, U(20)), hHp, hHp, PX.hp, PX.hp2);
  if (ph.name === 'fight' && b.rage) pbarThin(hx, g - sz, Math.max(10, U(20)), rg, rg >= 1 ? PX.dmg : PX.gold);   // thanh nộ mảnh, sát dưới thanh máu

  // ---- cột linh quang khi đột phá ----
  if (breakFx){
    breakFx.t += dt;
    const k = breakFx.t / breakFx.dur, kind = breakFx.kind;
    const col = kind === 'bad' ? PX.red : kind === 'gold' ? PX.gold : PX.cyan, core = kind === 'bad' ? '#ffb4a8' : kind === 'gold' ? '#fff3c4' : PX.cyan2;
    const cx = hx, top = 0, bot = g;
    if (kind !== 'bad'){
      // cột sáng bốc lên từ chân tu sĩ lên tới đỉnh khung rồi thu hẹp; viền dither cho cạnh pixel
      const grow = Math.min(1, k*4), fade = Math.max(0, 1 - Math.max(0, k - .55) / .45);
      const half = Math.max(1, Math.round(U(kind === 'gold' ? 11 : 7) * fade));
      const yStart = Math.round(bot - (bot - top) * ease(grow));
      // đầu cột sáng hơn khi đang bốc lên
      if (grow < 1) for (let x = -half; x <= half; x++){ P(cx + x, yStart, core); P(cx + x, yStart + 1, core); }
      for (let y = yStart; y < bot; y++) for (let x = -half - 1; x <= half + 1; x++){
        const edge = Math.abs(x) >= half;
        if (edge && (x + y + Math.floor(t*30)) % 2) continue;
        P(cx + x, y, Math.abs(x) <= Math.max(0, half - 2) ? core : col);
      }
      // vòng sóng linh khí lan trên mặt đất
      const rr = U(6) + k * w * (kind === 'gold' ? .55 : .35);
      if (k < .9) { pring(cx, g, rr, Math.max(2, rr*.18), col, false); pring(cx, g, rr*.7, Math.max(1, rr*.13), core, true); }
      if (Math.random() < dt*40) pfx.push({type:'spark', x: cx + (Math.random()-.5)*U(26), y: Math.max(yStart, g - Math.random()*(g - yStart)), vx:(Math.random()-.5)*8*u, vy:(-18 - Math.random()*22)*u, life:0, max:.9, col: Math.random() < .5 ? col : core});
    } else {
      // thất bại: tia đỏ vỡ vụn rơi xuống quanh tu sĩ
      if (k < .5 && Math.random() < dt*50) pfx.push({type:'spark', x: cx + (Math.random()-.5)*U(20), y: g - sz + Math.random()*U(8), vx:(Math.random()-.5)*30*u, vy: 10*u, life:0, max:.6, col: Math.random() < .6 ? PX.red : PX.red2});
      if (k < .25) pring(cx, g - hs, U(10) + k*U(40), U(10) + k*U(40), PX.red, true);
    }
    if (k >= 1) breakFx = null;
  }

  // ---- hiệu ứng pixel ----
  pfx = pfx.filter(f => (f.life += dt) < f.max);
  const labels = [];
  for (const f of pfx){
    const k = f.life / f.max;
    if (f.type === 'dust'){ f.x += f.vx*dt; f.y += f.vy*dt; if (k < .7) P(f.x, f.y, PX.path2); }
    else if (f.type === 'spark'){ f.x += f.vx*dt; f.y += f.vy*dt; f.vy += 60*dt; if (k < .85 || Math.floor(t*20) % 2) P(f.x, f.y, f.col); }
    else if (f.type === 'stone'){                                                    // viên linh thạch: bật lên, rơi nảy trên mặt đất, nhấp nháy rồi tan
      if (k < .62){ f.x += f.vx*dt; f.y += f.vy*dt; f.vy += 140*f.u*dt;
        if (f.y > f.g0 - 2){ f.y = f.g0 - 2; f.vy = Math.abs(f.vy) > 12*f.u ? -f.vy * .45 : 0; f.vx *= .6; } }
      else { const m = Math.min(1, (k - .62) / .3); f.x += (f.hx - f.x) * m * .35; f.y += (f.g0 - 18*f.u - f.y) * m * .35; }   // hút về tu sĩ
      if (k < .92){
        const q = Math.max(1, Math.round(f.u * (MODE === 'strip' ? 1 : 1.5))), x = Math.round(f.x), y = Math.round(f.y);
        STONE_PX.forEach((row, yy) => [...row].forEach((c, xx) => {
          if (c === '0') return;
          pctx.fillStyle = c === '2' ? '#eafffb' : yy > 1 ? '#3f9e94' : '#8fd0c9';
          pctx.fillRect(x + (xx - 2) * q, y + (yy - 3) * q, q, q);
        }));
      }
    }
    else if (f.type === 'num') labels.push(f);
    else if (f.type === 'label') labels.push(f);
  }

  // ---- chiêu thức trong ô tự thi triển (sprite sheet Hỏa Cầu 8 khung) ----
  if (castsNow.length && fxSheet.complete && fxSheet.naturalWidth){
    const s = ph.s, FC = ['#b02a1a','#e0521e','#f08a2a','#f8d25a'];
    let fxWet = false;                                             // chiêu hệ Thủy: đổi màu hiệu ứng lửa sang lam
    const FX = (f, x, y, k = 1) => { const d = Math.round(sz * k); if (fxWet) pctx.filter = WET_FILTER; pctx.drawImage(fxSheet, (f % 4) * 48, Math.floor(f / 4) * 48, 48, 48, Math.round(x - d / 2), Math.round(y - d / 2), d, d); pctx.filter = 'none'; };
    const handY = g - Math.round(sz * .55), tgtY = g - Math.round(sz * .45);
    castsNow.forEach((c, n) => {
      fxWet = skById(c.skill)?.element === 'thuy';
      if (c.anim === 'water_dragon' || c.anim === 'water_prison' || c.anim === 'water_arrows' || c.anim === 'water_moon') return;                       // Thương Hải Quyết: vẽ bằng ảnh thiết kế (tlDraw) sau khi phóng to
      if (refHero && (c.skill === 'hoa_cau' || SK_FX_ROW[c.skill] !== undefined)) return;   // vẽ nét ở skBattleDraw
      if (c.skill === 'hoa_cau' && c.dmg > 0 && hcSheet.complete && hcSheet.naturalWidth){
        // Hỏa Cầu: ánh xạ nhịp lượt đánh (tụ 0–.15 · bay .15–.45 · nổ .45–.75) sang 8 pha thiết kế
        const tier = hcTier((S.skills && S.skills.hoa_cau) || 1), sc = sz / 48, hand = heroX + U(10), y0 = handY - n * U(6);
        const seg = (a, b, e0, e1) => e0 + (s - a) / (b - a) * (e1 - e0);
        const ph = hcAt(s < .15 ? seg(0, .15, .2, .6) : s < .45 ? seg(.15, .45, .6, 1.4) : s < .75 ? seg(.45, .75, 1.4, 2.2) : 99);
        if (!ph) return;
        if (ph.name === 'ignite' || ph.name === 'form') hcDraw(pctx, hcSheet, tier, hcFrame(ph, t), hand, y0, sc);
        else if (ph.name === 'launch') hcDraw(pctx, hcSheet, tier, 6, hand + U(3) - HC_BALL_DX.launch * sc, y0, sc);
        else if (ph.name === 'fly'){ const x = hand + U(4) + (wolfX - U(8) - hand - U(4)) * ph.k; hcDraw(pctx, hcSheet, tier, hcFrame(ph, t), x - HC_BALL_DX.fly * sc, y0 + (tgtY - y0) * ph.k, sc); }
        else hcDraw(pctx, hcSheet, tier, hcFrame(ph, t), wolfX, tgtY, sc);
        return;
      }
      const big = c.anim === 'dragon' || c.anim === 'ultimate' || c.anim === 'pillar' ? 1.6 : 1;
      if (c.dmg > 0){
        if (s < .15) FX(Math.min(2, Math.floor(s / .05)), heroX + U(10), handY - n * U(6), big);
        else if (s < .45){ const k = (s - .15) / .3; FX(s < .3 ? 3 : 4, heroX + U(10) + (wolfX - heroX - U(10)) * k, handY - n * U(6), big); }
        else if (s < .75) FX(s < .55 ? 5 : s < .66 ? 6 : 7, wolfX, tgtY, big);
        if (c.anim === 'ultimate' && s >= .3 && s < .8){ pctx.globalAlpha = .18; pctx.fillStyle = '#1a4ab0'; pctx.fillRect(0, 0, w, g); pctx.globalAlpha = 1; }   // tuyệt kỹ: ánh lam
      } else if (s < .7){
        // chiêu thủ / khống / thân pháp: vòng lửa quanh người hoặc quanh quái
        const onHero = ['shield','aura','dash'].includes(c.anim), cx = onHero ? hx : wolfX, rx = U(onHero ? 14 : 18), ry = U(onHero ? 20 : 6);
        const cy = onHero ? g - U(22) : g - 1, n2 = 24;
        for (let q = 0; q < n2; q++){
          const a = q / n2 * Math.PI * 2 + t * 4;
          if (c.anim === 'shield' && Math.cos(a) < .1) continue;
          P(cx + Math.cos(a) * rx * Math.min(1, s * 4), cy + Math.sin(a) * ry * Math.min(1, s * 4), FC[(q + Math.floor(t * 10)) % 4]);
        }
      }
    });
  }
  for (const fn of fxAfter) fn();
  // ---- phóng to lên canvas chính, giữ cạnh pixel ----
  ctx.save();
  ctx.setTransform(1, 0, 0, 1, 0, 0);
  ctx.imageSmoothingEnabled = false;
  ctx.drawImage(pix, 0, 0, w, h, 0, 0, cv.width, cv.height);
  ctx.restore();

  // chữ tiếng Việt (tên quái, "Rút lui", vật phẩm nhặt được) vẽ ở độ phân giải thật cho rõ
  const sx = W / w, sy = H / h, big = MODE !== 'strip';
  if (monHi && wx !== null){                                                         // quái bản nét: phủ đúng ô sz×sz của sprite pixel
    ctx.save(); ctx.imageSmoothingEnabled = true; ctx.imageSmoothingQuality = 'high'; ctx.globalAlpha = wAlpha;
    ctx.drawImage(wFlash ? monHi.flash : burning ? monHi.burn : monHi.im, (wx - mhs) * sx, (g - msz + 1 + wy) * sy, msz * sx, msz * sy);
    ctx.restore();
  }
  if (refHero && !reduce && ph.name !== 'walk' && hsx === 1 && hsy === 1 && hrot === 0) hsy = 1 + .012 * Math.sin(t * 2.6);   // đứng chờ: thở nhẹ
  if (refHero) hcBattleDraw(refHero, {ph, hx, hy, hAlpha, hFlash, g, sz, sx, sy, wolfX, t, casts: castsNow, basic: b.basic, sqx: hsx, sqy: hsy, rot: hrot,
    cast: b.basic === 'hoa_cau'});
  if (monK >= 0 && wx !== null) monFx(monStyleOf(b.image), monK, {mx: wx * sx, my: (g - msz * .5) * sy, mw: msz * sx, mouthX: (wx - msz * .38) * sx, mouthY: (g - msz * .62) * sy,
    cx: (heroX + U(4)) * sx, cy: (g - sz * .55) * sy, gy: (g + 1) * sy, H: sz * sy, t, seed: ph.trip * 7 + ph.i});   // đè lên tu sĩ
  { const bdef = skById(b.basic);                                                 // đòn thường hệ Thủy (lượt không tung kỹ năng): nét cao
    if (ph.name === 'fight' && bdef?.element === 'thuy' && !castsNow.some(c => c.dmg > 0))
      tbDraw(ctx, bdef.anim, ph.s, (heroX + U(10)) * sx, (g - sz * .55) * sy, wolfX * sx, (g - sz * .45) * sy, sz * sy, t); }
  if (ph.name === 'fight' && castsNow.some(c => c.anim === 'water_moon' && c.dmg > 0))      // Thủy Nguyệt Trảm: trăng nước nét cao
    tngDraw(ctx, ph.s, (heroX + sz * .3) * sx, wolfX * sx, (g - sz * .55) * sy, sz * sy);
  if (ph.name === 'fight' && castsNow.some(c => c.anim === 'water_arrows' && c.dmg > 0))    // Vạn Thủy Tiễn: mưa tên nước nét cao
    ttienDraw(ctx, ph.s, (heroX + sz * .3) * sx, wolfX * sx, (g - sz * .55) * sy, sz * sy);
  if (ph.name === 'fight' && castsNow.some(c => c.anim === 'water_prison' && c.dmg > 0))    // Thủy Lao Bạo: lao cầu nét cao bao lấy quái
    tlaoDraw(ctx, ph.s, wolfX * sx, (g - sz * .45) * sy, sz * sy);
  if (ph.name === 'fight' && castsNow.some(c => c.anim === 'water_dragon' && c.dmg > 0))     // Thương Hải Quyết: thủy long nét cao
    tlDraw(ctx, ph.s, (heroX + sz * .3) * sx, wolfX * sx, (g - sz * .55) * sy, sz * sy);
  ctx.textAlign = 'center';
  if (big && wx !== null && wAlpha > .5){
    ctx.font = '600 12px "Be Vietnam Pro", system-ui, sans-serif';
    ctx.fillStyle = 'rgba(0,0,0,.6)'; ctx.fillText(b.monster, wx*sx + 1, (g - msz - 7)*sy + 1);
    ctx.fillStyle = elite ? '#d4a8ff' : 'rgba(235,228,211,.9)'; ctx.fillText(b.monster, wx*sx, (g - msz - 7)*sy);
  }
  if (wv && ph.name !== 'walk'){                                                      // ải 3 đợt: đợt đang đánh, góc phải trên
    const total = S.map.areas[Math.min(S.map.area, S.map.areas.length) - 1]?.waves?.length || B0.waves.length;
    if (total > 1){
      ctx.font = `700 ${big ? 13 : 10}px "Be Vietnam Pro", system-ui, sans-serif`; ctx.textAlign = 'right';
      const tx = `Đợt ${wvI + 1}/${total}`, ty = big ? 18 : 11;
      ctx.fillStyle = 'rgba(0,0,0,.6)'; ctx.fillText(tx, W - 7, ty + 1); ctx.fillStyle = elite ? '#d4a8ff' : '#f0e2bc'; ctx.fillText(tx, W - 8, ty);
      ctx.textAlign = 'center';
    }
  }
  if (S.map.area <= DATA.areas){                                                      // ải cao nhất: vàng ở ải cao nhất · cam khi bị lùi · xám khi tự chọn ải thấp
    const f = mapFrontier(), R = S.map.realm + 1, at = S.map.area >= f.top;
    const hasWave = wv && ph.name !== 'walk' && (S.map.areas[Math.min(S.map.area, S.map.areas.length) - 1]?.waves?.length || 0) > 1;
    const tx = at ? (big ? '⚑ Ải cao nhất' : '⚑ Cao nhất')
             : f.knocked ? (big ? `⚑ Bị lùi · cao nhất ${R}-${f.top}` : `⚑ Lùi · ${R}-${f.top}`)
             : (big ? `Cao nhất ${R}-${f.top}` : `↑ ${R}-${f.top}`);
    const ty = (big ? 18 : 11) + (hasWave ? (big ? 17 : 11) : 0);
    ctx.font = `700 ${big ? 12 : 9}px "Be Vietnam Pro", system-ui, sans-serif`; ctx.textAlign = 'right';
    ctx.fillStyle = 'rgba(0,0,0,.6)'; ctx.fillText(tx, W - 7, ty + 1);
    ctx.fillStyle = at ? '#e8c35a' : f.knocked ? '#ff9a6a' : 'rgba(235,228,211,.7)'; ctx.fillText(tx, W - 8, ty);
    ctx.textAlign = 'center';
  }
  for (const f of labels){
    const k = f.life / f.max;
    if (f.type === 'num'){
      // Số nổi gọn: chữ nét cao, tách hàng nghìn, bật nhẹ rồi bay lên và tan.
      // Chỉ đánh dấu bạo kích khi nguồn dữ liệu xác nhận, không tự suy đoán từ sát thương.
      const critical = f.critical === true;
      const size = big ? (critical ? 26 : 20) : (critical ? 18 : 14);
      const raw = String(f.text);
      const text = /^[+-]?\d+$/.test(raw)
        ? raw.replace(/\d+/, digits => Number(digits).toLocaleString('vi-VN')).replace(/^-/, '−') : raw;
      ctx.save();
      ctx.globalAlpha = reduce ? 1 : Math.min(1, k / .08) * Math.min(1, (1 - k) / .25);
      ctx.font = `${critical ? 'italic 800' : '800'} ${size}px "Be Vietnam Pro", system-ui, sans-serif`;
      ctx.textAlign = 'center'; ctx.textBaseline = 'alphabetic';
      const labelSize = big ? 11 : 9;
      const half = Math.max(ctx.measureText(text).width / 2, critical ? labelSize * 3.5 : 0) + 4;
      const x = Math.max(half, Math.min(W - half, f.x * sx));
      const y = Math.max(size + (critical ? labelSize + 5 : 0) + 3, f.y * sy - (reduce ? 0 : k * (big ? 30 : 18)));
      ctx.translate(x, y);
      const pop = reduce ? 1 : k < .12 ? .85 + k / .12 * .25 : k < .26 ? 1.1 - (k - .12) / .14 * .1 : 1;
      ctx.scale(pop, pop);
      ctx.lineJoin = 'round'; ctx.lineWidth = big ? 3 : 2;
      ctx.strokeStyle = '#141719';
      ctx.fillStyle = critical ? '#ffb33f' : /^-/.test(raw) ? '#fff0c2' : f.col;
      ctx.strokeText(text, 0, 0); ctx.fillText(text, 0, 0);
      if (critical){
        ctx.font = `800 ${labelSize}px "Be Vietnam Pro", system-ui, sans-serif`;
        ctx.strokeText('BẠO KÍCH', 0, -size - 3); ctx.fillText('BẠO KÍCH', 0, -size - 3);
      }
      ctx.restore();
      continue;
    }
    ctx.font = `700 ${big ? 15 : 11}px "Be Vietnam Pro", system-ui, sans-serif`;
    ctx.fillStyle = 'rgba(0,0,0,.6)'; ctx.fillText(f.text, f.x*sx + 1, (f.y - k*6)*sy + 1);
    ctx.fillStyle = f.col; ctx.fillText(f.text, f.x*sx, (f.y - k*6)*sy);
  }
  floats = floats.filter(f => f.t < 1.6);
  for (const f of floats){
    f.t += dt;
    ctx.font = `600 ${big ? 14 : 10}px "Be Vietnam Pro", system-ui, sans-serif`;
    const x = heroX*sx, y = (g - sz - 8)*sy + (f.row||0)*(big ? 16 : 11) - f.t*(big ? 22 : 8);
    ctx.fillStyle = 'rgba(0,0,0,.6)'; ctx.fillText(f.text, x + 1, y + 1);
    ctx.fillStyle = hexA(f.c || r.aura, Math.max(0, Math.min(1, 1 - f.t/1.6))); ctx.fillText(f.text, x, y);
  }
  rings = rings.filter(q => q.t < (q.big ? 2.2 : 1.2));
  for (const q of rings){
    q.t += dt; const k = q.t/(q.big ? 2.2 : 1.2);
    ctx.strokeStyle = hexA(q.c, 1-k); ctx.lineWidth = (q.big ? 5 : 3)*(1-k) + 1;
    ctx.beginPath(); ctx.arc(heroX*sx, (g - hs)*sy, 10 + k*Math.max(W, H)*(q.big ? .7 : .4), 0, 7); ctx.stroke();
  }
  return true;
}

function battlePhase(){
  const b = S.battle, hits = Math.max(1, b.hits), ws = b.waves || [], gap = .8;   // gap = GameData.WaveSeconds
  const cd = (S.exploreCooldownMs || 8000) / 1000, left = exploreLeft() / 1000;
  const raw = 2.5 + hits * 1 + Math.max(0, ws.length - 1) * gap + 1, f = cd / raw;   // thời gian thực → thời gian kịch bản
  const e = Math.max(0, Math.min(raw, (cd - left) / f));
  const trip = Math.round((recvAt + S.exploreReadyInMs) / 250);
  if (e < 2.0) return {name:'walk', k:e/2, trip};
  if (e < 2.5) return {name:'enter', k:(e-2)/.5, trip, wave: 0};
  let x = e - 2.5;
  for (let w = 0; w < Math.max(1, ws.length); w++){
    const s0 = ws.length ? ws[w].start : 0, n = ws.length ? (ws[w + 1]?.start ?? hits) - s0 : hits;
    if (w > 0){ if (x < gap) return {name:'enter', k: x / gap, trip, wave: w}; x -= gap; }
    if (x < n) return {name:'fight', i: s0 + Math.floor(x), s: x % 1, trip, wave: w};
    x -= n;
  }
  return {name: b.win ? 'win' : 'lose', k: Math.min(1, x), trip};
}
/** Đợt quái đang diễn (ải 3 đợt) theo pha; -1 nếu trận chỉ có 1 quái. */
function battleWaveIdx(b, ph){
  if (!(b?.waves?.length > 1)) return -1;                         // ải Boss: chỉ 1 trận đánh Boss, không chia đợt
  if (ph.wave != null) return Math.min(ph.wave, b.waves.length - 1);
  return ph.name === 'walk' ? 0 : b.waves.length - 1;
}
