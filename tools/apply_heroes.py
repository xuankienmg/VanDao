#!/usr/bin/env python3
"""Hình nhân vật theo cảnh giới (đứng · ngồi thiền · khung trận đấu). Chạy từ thư mục gốc dự án:
python3 tools/apply_heroes.py   (giữ CRLF; chạy lại không lỗi)"""
import sys

def patch(path, pairs):
    s = open(path, encoding='utf-8', newline='').read()
    nl = '\r\n' if '\r\n' in s else '\n'
    for a, b in pairs:
        a, b = a.replace('\n', nl), b.replace('\n', nl)
        if b in s and a not in s: continue
        n = s.count(a)
        if n != 1: sys.exit(f'{path}: tìm thấy {n} chỗ cho:\n{a[:200]}')
        s = s.replace(a, b)
    open(path, 'w', encoding='utf-8', newline='').write(s)
    print('ok', path)

def realm_imgs(cg):
    return (f'Hero = "img/hero_{cg}.png",\n            HeroStand = "img/hero_{cg}_stand.png", HeroMed = "img/med_{cg}.png", HeroCast = "img/hoa_cau_ref_{cg}.png",')

patch('Game/GameData.cs', [
("""    /// <summary>Ảnh nhân vật khi đi thám hiểm ở cảnh giới này.</summary>
    public required string Hero { get; init; }""",
 """    /// <summary>Ảnh nhân vật khi đi thám hiểm ở cảnh giới này.</summary>
    public required string Hero { get; init; }
    /// <summary>Ảnh nét theo cảnh giới: đứng (Hành trang) · ngồi thiền (Đột phá) · bảng khung tung Hỏa Cầu (trận đấu cửa sổ lớn).
    /// Cắt lại: python tools/cut_heroes.py tools wwwroot/img</summary>
    public string? HeroStand { get; init; }
    public string? HeroMed { get; init; }
    public string? HeroCast { get; init; }"""),
('Hero = "img/hero_luyenkhi.png",', realm_imgs('luyenkhi')),
('Sash = "#3b5f9a", Hero = "img/hero_trucco.png",', 'Sash = "#3b5f9a", ' + realm_imgs('trucco')),
('Sash = "#9a6b1f", Hero = "img/hero_trucco.png",', 'Sash = "#9a6b1f", ' + realm_imgs('kimdan')),
])
patch('Game/Dtos.cs', [
("string Cn, int BagSlots, string[] EquipSlots, string Boss, string BossImage, string BossLair, string KeyItem, string BossItem);",
 "string Cn, int BagSlots, string[] EquipSlots, string Boss, string BossImage, string BossLair, string KeyItem, string BossItem,\n    string? HeroStand = null, string? HeroMed = null, string? HeroCast = null);"),
])
patch('Program.cs', [
("                r.Boss.Name, r.Boss.Image, r.BossLair, r.KeyItem, r.BossItem)),",
 "                r.Boss.Name, r.Boss.Image, r.BossLair, r.KeyItem, r.BossItem, r.HeroStand, r.HeroMed, r.HeroCast)),"),
])
patch('wwwroot/index.html', [
("""  // tu sĩ ngồi thiền (ảnh thiết kế img/med_ref.png, dùng chung mọi cảnh giới; tách lại: tools/cut_med.py)
  const src = 'img/med_ref.png';""",
 """  // tu sĩ ngồi thiền theo cảnh giới (img/med_<cảnh giới>.png; tách lại: tools/cut_heroes.py), thiếu thì dùng ảnh chung cũ
  const src = realm().heroMed || 'img/med_ref.png';"""),
("""<img class="hero ref" src="img/hero_ref_stand.png" alt="${S.name}">""",
 """<img class="hero ref" src="${realm().heroStand || 'img/hero_ref_stand.png'}" alt="${S.name}">"""),
("  const ref = skImg(HC_REF.src), wolf =", "  const ref = skImg(realm().heroCast || HC_REF.src), wolf ="),
("  const refHero = skImg(HC_REF.src);", "  const refHero = skImg(realm().heroCast || HC_REF.src);           // khung tung chiêu theo cảnh giới (cùng bố cục HC_REF)"),
])
