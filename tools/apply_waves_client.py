#!/usr/bin/env python3
"""Client cho quái 3 đợt: diễn trận theo đợt (đổi quái, máu mang qua, tinh anh), kiểu đánh 16 quái mới.
Chạy từ thư mục gốc dự án: python3 tools/apply_waves_client.py   (chạy lại không lỗi)"""
import sys
P = 'wwwroot/index.html'
s = open(P, encoding='utf-8', newline='').read()
nl = '\r\n' if '\r\n' in s else '\n'
def rep(a, b):
    global s
    a, b = a.replace('\n', nl), b.replace('\n', nl)
    if b in s and a not in s: return
    n = s.count(a)
    if n != 1: sys.exit(f'tìm thấy {n} chỗ cho:\n{a[:200]}')
    s = s.replace(a, b)

# kiểu đòn phản công của từng loài (dùng lại 7 kiểu sẵn có)
rep("""const MON_STYLE = {quai_yeu_lang:'pounce', boss_huyet_giao:'pounce', quai_doc_xa:'spit', quai_thach_quy:'slam',
  quai_hoa_ho:'foxfire', boss_kim_o:'foxfire', quai_bang_lang:'frost', quai_thiet_trung:'charge', quai_cot_khuyen:'blink', boss_hac_lan:'foxfire'};""",
"""const MON_STYLE = {quai_yeu_lang:'pounce', quai_doc_xa:'spit', quai_thach_tru:'charge', quai_bang_ho:'frost', quai_pha_son_vien:'slam',
  quai_tinh_thach_tich:'frost', quai_huyen_o:'charge', quai_ma_dang:'spit', quai_cot_khuyen:'blink', quai_loi_ung:'charge',
  tinh_thiet_giap_ho:'pounce', tinh_cot_tuong:'charge', tinh_hac_mang:'spit',
  boss_tu_ty_ma_vien:'slam', boss_hac_giao:'foxfire', boss_bang_phuong:'frost'};""")

# vẽ trận: lấy quái / máu / sát thương theo đợt đang đánh
rep("""  const L = pixLayers, g = L.g, b = S.battle;
  const hero = sprite48(realm().hero), mon = sprite48(b.image), monHi = monHD(b.image);""",
"""  const L = pixLayers, g = L.g, B0 = S.battle, wvI = battleWaveIdx(B0, ph), wv = wvI >= 0 ? B0.waves[wvI] : null;
  // ải 3 đợt: quái, máu, sát thương hai bên theo đợt đang đánh (mảng theo lượt vẫn dùng chung chỉ số lượt)
  const b = wv ? Object.assign({}, B0, {monster: wv.name, image: wv.image, monsterHp: wv.hp, heroDmg: wv.heroDmg, monsterDmg: wv.monsterDmg}) : B0;
  const wEnd = wv ? (B0.waves[wvI + 1]?.start ?? B0.hits) : B0.hits, lastWave = !wv || wvI === B0.waves.length - 1, elite = wv?.tier === 'elite';
  const hero = sprite48(realm().hero), mon = sprite48(b.image), monHi = monHD(b.image);""")
rep("const isBoss = /\\/boss_/.test(b.image), ms = isBoss ? 1.5 : 1,",
    "const isBoss = /\\/boss_/.test(b.image), ms = isBoss ? 1.5 : elite ? 1.25 : 1,")
rep("const mLeft = n => n <= 0 ? b.monsterHp :", "const mLeft = n => n <= (wv ? wv.start : 0) ? b.monsterHp :")
rep("killing = b.win && i === b.hits - 1,", "killing = (lastWave ? b.win : true) && i === wEnd - 1,")
rep("""    wy = reduce ? 0 : -(Math.floor(ph.k*10) % 2);
    sword = 'ready';
  } else if (ph.name === 'fight'){""",
"""    wy = reduce ? 0 : -(Math.floor(ph.k*10) % 2);
    sword = 'ready';
    if (wv && wvI > 0) hHp = Math.max(0, hLeft(wv.start) / heroMax);                  // đợt sau: máu tu sĩ giữ từ đợt trước
  } else if (ph.name === 'fight'){""")
rep("  if (Object.keys(battleDone).length > 60) battleDone = {};",
"""  if (elite && wx !== null && wAlpha > .5) fxAfter.push(() => pring(wx, g - msz * .45, msz * .5 + (Math.floor(t * 6) % 2), msz * .5, '#a070e0', true));   // tinh anh: hào quang tím
  if (wv && ph.name === 'enter' && wvI > 0){                                         // quái đợt sau bước vào
    const ek = ph.trip + ':wave' + wvI;
    if (!battleDone[ek]){ battleDone[ek] = 1; if (MODE !== 'strip') pfx.push({type:'label', x: wolfX, y: g - msz - 20, text: elite ? 'Tinh anh xuất hiện!' : 'Đợt ' + (wvI + 1), col: elite ? '#c08aff' : '#f0e2bc', life: 0, max: 1.1}); }
  }
  if (Object.keys(battleDone).length > 60) battleDone = {};""")
rep("""    ctx.fillStyle = 'rgba(235,228,211,.9)'; ctx.fillText(b.monster, wx*sx, (g - msz - 7)*sy);
  }""",
"""    ctx.fillStyle = elite ? '#d4a8ff' : 'rgba(235,228,211,.9)'; ctx.fillText(b.monster, wx*sx, (g - msz - 7)*sy);
  }
  if (wv && ph.name !== 'walk'){                                                      // ải 3 đợt: đợt đang đánh, góc phải trên
    const total = S.map.areas[Math.min(S.map.area, S.map.areas.length) - 1]?.waves?.length || B0.waves.length;
    if (total > 1){
      ctx.font = `700 ${big ? 13 : 10}px "Be Vietnam Pro", system-ui, sans-serif`; ctx.textAlign = 'right';
      const tx = `Đợt ${wvI + 1}/${total}`, ty = big ? 18 : 11;
      ctx.fillStyle = 'rgba(0,0,0,.6)'; ctx.fillText(tx, W - 7, ty + 1); ctx.fillStyle = elite ? '#d4a8ff' : '#f0e2bc'; ctx.fillText(tx, W - 8, ty);
      ctx.textAlign = 'center';
    }
  }""")
# dòng thời gian: đi đường → (quái bước vào → các lượt) × số đợt → kết thúc
rep("""function battlePhase(){
  const b = S.battle, hits = Math.max(1, b.hits);
  const cd = (S.exploreCooldownMs || 8000) / 1000, left = exploreLeft() / 1000;
  const raw = 2.5 + hits * 1 + 1, f = cd / raw;                       // thời gian thực → thời gian kịch bản
  const e = Math.max(0, Math.min(raw, (cd - left) / f));
  const trip = Math.round((recvAt + S.exploreReadyInMs) / 250);
  if (e < 2.0) return {name:'walk', k:e/2, trip};
  if (e < 2.5) return {name:'enter', k:(e-2)/.5, trip};
  if (e < 2.5 + hits){ const x = e - 2.5; return {name:'fight', i:Math.floor(x), s:x % 1, trip}; }
  return {name: b.win ? 'win' : 'lose', k:Math.min(1, (e - 2.5 - hits)), trip};
}""",
"""function battlePhase(){
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
  if (!b?.waves?.length) return -1;
  if (ph.wave != null) return Math.min(ph.wave, b.waves.length - 1);
  return ph.name === 'walk' ? 0 : b.waves.length - 1;
}""")
# thanh thám hiểm: nêu các đợt
rep("${bt.win ? `hạ ${bt.monster} sau ${bt.hits} đòn` : `thua ${bt.monster}, cần mạnh hơn`}",
    "${bt.win ? `hạ ${(bt.waves || []).map(w => w.name + (w.tier === 'elite' ? ' (tinh anh)' : '')).join(', ') || bt.monster}` : `thua ${(bt.waves || []).at(-1)?.name || bt.monster}, cần mạnh hơn`}")
open(P, 'w', encoding='utf-8', newline='').write(s)
print('ok', P)
