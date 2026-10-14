#!/usr/bin/env python3
"""Quái 3 đợt mỗi ải + bộ quái mới (10 thường · 3 tinh anh · 3 Boss). Chạy từ thư mục gốc dự án:
python3 tools/apply_waves.py            (giữ CRLF, chạy lại không lỗi: bước nào đã làm thì bỏ qua)"""
import sys

def patch(path, pairs):
    s = open(path, encoding='utf-8', newline='').read()
    nl = '\r\n' if '\r\n' in s else '\n'
    for a, b in pairs:
        a, b = a.replace('\n', nl), b.replace('\n', nl)
        if b in s and a not in s: continue          # đã áp
        n = s.count(a)
        if n != 1: sys.exit(f'{path}: tìm thấy {n} chỗ cho:\n{a[:200]}')
        s = s.replace(a, b)
    open(path, 'w', encoding='utf-8', newline='').write(s)
    print('ok', path)

# ======================= Dtos.cs =======================
patch('Game/Dtos.cs', [
("public record AreaDto(int Index, string Monster, string Place, int Hp, int Atk, int Def, bool Win, int Hits, bool Open, string Image = \"\");",
 """public record AreaDto(int Index, string Monster, string Place, int Hp, int Atk, int Def, bool Win, int Hits, bool Open, string Image = "",
    List<WaveInfoDto>? Waves = null, bool Elite = false);

/// <summary>Một đợt quái của ải (bảng thông tin ải trên bản đồ).</summary>
public record WaveInfoDto(string Name, string Image, string Tier, int Hp, int Atk, int Def);"""),
("""    int EnrageTurn = -1, int MonsterDmgEnraged = 0);   // Boss Cuồng bạo: lượt (đã lấy mẫu) bắt đầu nổi giận · sát thương mỗi đòn khi đó
""",
 """    int EnrageTurn = -1, int MonsterDmgEnraged = 0,   // Boss Cuồng bạo: lượt (đã lấy mẫu) bắt đầu nổi giận · sát thương mỗi đòn khi đó
    List<WaveDto>? Waves = null);           // các đợt quái đã giao chiến (ải thường 3 đợt): Start = lượt (đã lấy mẫu) đợt bắt đầu

/// <summary>Một đợt quái trong trận: chỉ số đã tính công pháp/trang bị, sát thương hai bên, lượt bắt đầu (đã lấy mẫu).</summary>
public record WaveDto(string Name, string Image, string Tier, int Hp, int Atk, int Def, int HeroDmg, int MonsterDmg, int Start);
"""),
])

# ======================= GameData.cs =======================
NAMES = {
 'lk': ('"Yêu Lang", "Độc Xà", "Thạch Trư", "Băng Hồ", "Phá Sơn Viên", "Tinh Thạch Tích", "Huyền Ô", "Ma Đằng", "Cốt Khuyển", "Lôi Ưng"',
        '"Thiết Giáp Hổ", "Cốt Tướng", "Hắc Mãng"'),
 'tc': ('"Huyết Yêu Lang", "Huyết Độc Xà", "Huyết Thạch Trư", "Huyết Băng Hồ", "Huyết Sơn Viên", "Huyết Thạch Tích", "Huyết Huyền Ô", "Huyết Ma Đằng", "Huyết Cốt Khuyển", "Huyết Lôi Ưng"',
        '"Huyết Giáp Hổ", "Huyết Cốt Tướng", "Huyết Hắc Mãng"'),
 'kd': ('"Yêu Lang Vương", "Độc Xà Vương", "Thạch Trư Vương", "Băng Hồ Vương", "Sơn Viên Vương", "Thạch Tích Vương", "Huyền Ô Vương", "Ma Đằng Vương", "Cốt Khuyển Vương", "Lôi Ưng Vương"',
        '"Giáp Hổ Vương", "Cốt Tướng Vương", "Hắc Mãng Vương"'),
}
patch('Game/GameData.cs', [
("public record MonsterDef(string Name, string Image, int Hp, int Atk, int Def);",
 "public record MonsterDef(string Name, string Image, int Hp, int Atk, int Def, string Tier = \"\");   // Tier: \"\" thường · \"elite\" tinh anh · \"boss\""),
("""    /// <summary>Tên 7 loài quái theo thứ tự GameData.MonsterImages (ải 1-15 chia đều, mạnh dần); null = dùng Monster.</summary>
    public string[]? MonsterNames { get; init; }""",
 """    /// <summary>Tên 10 loài quái thường theo thứ tự GameData.MonsterImages (mạnh dần); null = dùng Monster.</summary>
    public string[]? MonsterNames { get; init; }
    /// <summary>Tên 3 quái tinh anh theo thứ tự GameData.EliteImages (đợt cuối ở ải EliteMinArea–15).</summary>
    public string[]? EliteNames { get; init; }"""),
("""    /// <summary>Ảnh 7 loài quái thường, dùng chung mọi cảnh giới (tên riêng từng cảnh giới ở RealmDef.MonsterNames).
    /// Ải 1-3 · 4-6 · 7-9 · 10-12 · 13-15 · 16-18 · 19-20.</summary>
    public static readonly string[] MonsterImages =
    {
        "img/quai_yeu_lang.png", "img/quai_doc_xa.png", "img/quai_thach_quy.png", "img/quai_hoa_ho.png",
        "img/quai_bang_lang.png", "img/quai_thiet_trung.png", "img/quai_cot_khuyen.png",
    };

    /// <summary>Loài quái (0..6) ở ải s.</summary>
    public static int MonsterKind(int s) => Math.Min(MonsterImages.Length - 1, (Math.Clamp(s, 1, Areas) - 1) * MonsterImages.Length / Areas);

    public static MonsterDef AreaMonster(RealmDef r, int s)
    {
        if (s >= BossArea) return r.Boss with { Hp = (int)Math.Round(r.Boss.Hp * MonsterHpMult * BossMult * BossHpByRealm[Math.Min(RealmIndex(r), BossHpByRealm.Length - 1)]),
            Atk = (int)Math.Round(r.Boss.Atk * BossMult), Def = (int)Math.Round(r.Boss.Def * BossMult) };
        var m = 0.6 + 2.0 * (Math.Clamp(s, 1, Areas) - 1) / (Areas - 1);
        var b = r.Monster;
        if (r.MonsterNames is { } names) { var k = MonsterKind(s); b = b with { Name = names[k], Image = MonsterImages[k] }; }
        return b with { Hp = (int)Math.Round(b.Hp * m * MonsterHpMult), Atk = (int)Math.Round(b.Atk * m), Def = (int)Math.Round(b.Def * m) };
    }""",
 """    /// <summary>Ảnh 10 loài quái thường (mạnh dần) và 3 quái tinh anh, dùng chung mọi cảnh giới
    /// (tên riêng từng cảnh giới ở RealmDef.MonsterNames / EliteNames). Cắt từ tools/monsters2_design.png bằng tools/cut_monsters2.py.</summary>
    public static readonly string[] MonsterImages =
    {
        "img/quai_yeu_lang.png", "img/quai_doc_xa.png", "img/quai_thach_tru.png", "img/quai_bang_ho.png", "img/quai_pha_son_vien.png",
        "img/quai_tinh_thach_tich.png", "img/quai_huyen_o.png", "img/quai_ma_dang.png", "img/quai_cot_khuyen.png", "img/quai_loi_ung.png",
    };
    public static readonly string[] EliteImages = { "img/tinh_thiet_giap_ho.png", "img/tinh_cot_tuong.png", "img/tinh_hac_mang.png" };

    /// <summary>Mỗi chuyến ở ải thường đánh Waves đợt liên tiếp (máu tu sĩ mang qua đợt sau); từ ải EliteMinArea đợt cuối là quái tinh anh.</summary>
    public const int Waves = 3, EliteMinArea = 10;
    /// <summary>Chỉ số mỗi đợt so với quái ải (trước đây 1 con mỗi chuyến): quái thường máu ×0.4;
    /// tinh anh máu ×0.8 · công ×1.3 · thủ ×1.2 và giảm EliteDmgReduce% sát thương nhận vào.</summary>
    public const double WaveHp = 0.40, EliteHp = 0.80, EliteAtk = 1.3, EliteDef = 1.2, EliteDmgReduce = 5;
    /// <summary>Thưởng thêm khi thắng chuyến có tinh anh: linh thạch ×EliteStoneMult, rương hiếm EliteRareChestChance.</summary>
    public const double EliteStoneMult = 1.5, EliteRareChestChance = 0.20;

    /// <summary>Loài quái chính (0..9) ở ải s.</summary>
    public static int MonsterKind(int s) => Math.Min(MonsterImages.Length - 1, (Math.Clamp(s, 1, Areas) - 1) * MonsterImages.Length / Areas);

    /// <summary>Quái đầu tiên của ải (tên hiển thị / dự đoán cũ).</summary>
    public static MonsterDef AreaMonster(RealmDef r, int s) => AreaWaves(r, s)[0];

    /// <summary>Các đợt quái của ải s: ải Boss 1 đợt (Boss); ải thường 3 đợt — loài chính, loài chính (từ ải EliteMinArea: loài kế),
    /// rồi loài kế hoặc quái tinh anh. Chỉ số theo ải: ×0.6 (ải 1) → ×2.6 (ải 15) so với quái gốc của cảnh giới.</summary>
    public static MonsterDef[] AreaWaves(RealmDef r, int s)
    {
        if (s >= BossArea) return new[] { r.Boss with { Hp = (int)Math.Round(r.Boss.Hp * MonsterHpMult * BossMult * BossHpByRealm[Math.Min(RealmIndex(r), BossHpByRealm.Length - 1)]),
            Atk = (int)Math.Round(r.Boss.Atk * BossMult), Def = (int)Math.Round(r.Boss.Def * BossMult), Tier = "boss" } };
        var m = 0.6 + 2.0 * (Math.Clamp(s, 1, Areas) - 1) / (Areas - 1);
        var b = r.Monster with { Hp = (int)Math.Round(r.Monster.Hp * m * MonsterHpMult), Atk = (int)Math.Round(r.Monster.Atk * m), Def = (int)Math.Round(r.Monster.Def * m) };
        MonsterDef Common(int k) => b with { Name = r.MonsterNames is { } n ? n[k] : b.Name, Image = MonsterImages[k], Hp = Math.Max(1, (int)Math.Round(b.Hp * WaveHp)) };
        int k0 = MonsterKind(s), k1 = Math.Min(k0 + 1, MonsterImages.Length - 1);
        if (s < EliteMinArea) return new[] { Common(k0), Common(k0), Common(k1) };
        var e = Math.Min(EliteImages.Length - 1, (s - EliteMinArea) / 2);
        var elite = b with { Name = r.EliteNames is { } en ? en[e] : b.Name, Image = EliteImages[e], Hp = (int)Math.Round(b.Hp * EliteHp),
            Atk = (int)Math.Round(b.Atk * EliteAtk), Def = (int)Math.Round(b.Def * EliteDef), Tier = "elite" };
        return new[] { Common(k0), Common(k1), elite };
    }"""),
('            MonsterNames = new[] { "Yêu Lang", "Độc Xà", "Thạch Giáp Quy", "Hỏa Hồ", "Băng Lang", "Thiết Giáp Trùng", "Cốt Khuyển" },',
 f'            MonsterNames = new[] {{ {NAMES["lk"][0]} }},\n            EliteNames = new[] {{ {NAMES["lk"][1]} }},'),
('            MonsterNames = new[] { "Huyết Yêu Lang", "Huyết Độc Xà", "Huyết Giáp Quy", "Huyết Hỏa Hồ", "Huyết Băng Lang", "Huyết Giáp Trùng", "Huyết Cốt Khuyển" },',
 f'            MonsterNames = new[] {{ {NAMES["tc"][0]} }},\n            EliteNames = new[] {{ {NAMES["tc"][1]} }},'),
('            MonsterNames = new[] { "Yêu Lang Vương", "Độc Xà Vương", "Thạch Quy Vương", "Hỏa Hồ Vương", "Băng Lang Vương", "Thiết Trùng Vương", "Cốt Khuyển Vương" },',
 f'            MonsterNames = new[] {{ {NAMES["kd"][0]} }},\n            EliteNames = new[] {{ {NAMES["kd"][1]} }},'),
# Boss mới
('            Boss = new MonsterDef("Kim Ô Yêu Vương", "img/boss_kim_o.png", Hp: 140, Atk: 23, Def: 5),',
 '            Boss = new MonsterDef("Tứ Tý Ma Viên", "img/boss_tu_ty_ma_vien.png", Hp: 140, Atk: 23, Def: 5),'),
('            Boss = new MonsterDef("Huyết Giao", "img/boss_huyet_giao.png", Hp: 940, Atk: 172, Def: 52),',
 '            Boss = new MonsterDef("Hắc Giao", "img/boss_hac_giao.png", Hp: 940, Atk: 172, Def: 52),'),
('            Boss = new MonsterDef("Hắc Lân Mãng", "img/boss_hac_lan.png", Hp: 5600, Atk: 860, Def: 260),',
 '            Boss = new MonsterDef("Băng Phượng", "img/boss_bang_phuong.png", Hp: 5600, Atk: 860, Def: 260),'),
# yêu hạch / chìa khóa đổi tên theo Boss mới (giữ id)
('"Hắc Lân Yêu Hạch",  "boss", "#3d6fd8", "Yêu hạch của Hắc Lân Mãng, vảy đen bao quanh một đốm u quang."',
 '"Băng Phượng Tinh Hạch", "boss", "#3d6fd8", "Tinh hạch của Băng Phượng, hàn khí vạn năm ngưng lại quanh một đốm u quang."'),
('"Huyết Giao Yêu Hạch", "boss", "#d8404a", "Yêu hạch của Huyết Giao, nóng như máu sôi."',
 '"Hắc Giao Yêu Hạch", "boss", "#d8404a", "Yêu hạch của Hắc Giao, u quang lạnh lẽo chảy trong vảy đen."'),
('"Kim Ô Tinh Hạch",    "boss", "#f2b23c", "Tinh hạch của Kim Ô yêu vương, chứa thái dương chân hỏa."',
 '"Ma Viên Yêu Hạch",   "boss", "#f2b23c", "Yêu hạch của Tứ Tý Ma Viên, nặng như đá núi, ẩn sức bốn tay."'),
('Mảnh bản đồ cổ dẫn tới Hư Thiên Điện, nơi Hắc Lân Mãng ẩn mình.', 'Mảnh bản đồ cổ dẫn tới Hư Thiên Điện, nơi Băng Phượng ẩn mình.'),
# thời gian chuyến: mỗi đợt tối đa MaxWaveHits lượt hoạt ảnh + nghỉ WaveSeconds giữa các đợt; tu vi mỗi chuyến ×1.5 để giữ tu vi mỗi giờ
("    public const int MaxHits = 10;",
 "    public const int MaxHits = 10;\n    /// <summary>Ải thường 3 đợt: mỗi đợt tối đa MaxWaveHits lượt hoạt ảnh, giữa hai đợt nghỉ WaveSeconds giây (quái mới bước vào).</summary>\n    public const int MaxWaveHits = 5;\n    public const double WaveSeconds = 0.8;"),
("    public const double ExploreQiSeconds = 4;", "    public const double ExploreQiSeconds = 6;   // chuyến 3 đợt dài hơn ~1.5 lần (trước 4)"),
])

# ======================= GameService.cs =======================
patch('Game/GameService.cs', [
# bản đồ: danh sách đợt của từng ải
("""            var b = BattleAt(p, a);
            return new AreaDto(a, b.Monster, GameData.AreaPlace(r, a),
                b.MonsterHp, b.MonsterAtk, b.MonsterDef, b.Win, b.Hits, a <= amax || (a == GameData.BossArea && amax >= GameData.Areas), b.Image);""",
 """            var b = BattleAt(p, a);
            var ws = GameData.AreaWaves(r, a);
            return new AreaDto(a, b.Monster, GameData.AreaPlace(r, a),
                b.MonsterHp, b.MonsterAtk, b.MonsterDef, b.Win, b.Hits, a <= amax || (a == GameData.BossArea && amax >= GameData.Areas), b.Image,
                ws.Select(w => new WaveInfoDto(w.Name, w.Image, w.Tier, w.Hp, w.Atk, w.Def)).ToList(), ws.Any(w => w.Tier == "elite"));"""),
# thua: nhớ quái đã hạ gục mình
("""                    losses++;
                    p.Qi = Math.Min(QiReq(p), p.Qi + ExploreQi(p) / 2);""",
 """                    losses++;
                    lostTo = battle.Waves is { Count: > 0 } lw ? lw[^1].Name : battle.Monster;
                    p.Qi = Math.Min(QiReq(p), p.Qi + ExploreQi(p) / 2);"""),
("        var bossWins = 0;", "        var bossWins = 0;\n        string? lostTo = null;                                      // quái (đợt) đã khiến chuyến thua"),
# tinh anh: thêm linh thạch và rương hiếm
("""                var scale = 1 + (area - 1) * 0.05;""",
 """                var eliteTrip = area >= GameData.EliteMinArea;                // chuyến có tinh anh: linh thạch ×1.5, rương hiếm 20%
                var scale = (1 + (area - 1) * 0.05) * (eliteTrip ? GameData.EliteStoneMult : 1);"""),
("""                             ("ruong_hiem", area >= GameData.RareChestMinArea ? GameData.RareChestChance : 0) })""",
 """                             ("ruong_hiem", eliteTrip ? GameData.EliteRareChestChance : area >= GameData.RareChestMinArea ? GameData.RareChestChance : 0) })"""),
# nhật ký
("""            AddLog(p, $"Thám hiểm {areaTag} · {place}: bị {GameData.AreaMonster(MapRealm(p), startArea).Name} đánh trọng thương, phải rút lui. Cần thêm tấn công, máu hoặc phòng thủ.", "bad");""",
 """            AddLog(p, $"Thám hiểm {areaTag} · {place}: bị {lostTo ?? GameData.AreaMonster(MapRealm(p), startArea).Name} đánh trọng thương, phải rút lui. Cần thêm tấn công, máu hoặc phòng thủ.", "bad");"""),
("""            AddLog(p, $"Thám hiểm {areaTag} · {place}: hạ {GameData.AreaMonster(MapRealm(p), startArea).Name}: """,
 """            AddLog(p, $"Thám hiểm {areaTag} · {place}: hạ {WaveNames(MapRealm(p), startArea)}: """),
("""    /// <summary>Tổng chỉ số % từ trang bị đang mặc.</summary>""",
 """    /// <summary>Tên các đợt quái của ải, gộp loài trùng: "Yêu Lang ×2, Độc Xà" · tinh anh ghi kèm.</summary>
    private static string WaveNames(RealmDef r, int area) =>
        string.Join(", ", GameData.AreaWaves(r, area).GroupBy(w => w.Name)
            .Select(g => g.First().Name + (g.Count() > 1 ? $" ×{g.Count()}" : "") + (g.First().Tier == "elite" ? " (tinh anh)" : "")));

    /// <summary>Tổng chỉ số % từ trang bị đang mặc.</summary>"""),
# thời gian chuyến: thêm nghỉ giữa các đợt
("""            + Battle(p).Hits * GameData.HitSeconds * (1 - Math.Min(60, Stats(p).Speed) / 100)
            + GameData.EndSeconds);""",
 """            + Battle(p).Hits * GameData.HitSeconds * (1 - Math.Min(60, Stats(p).Speed) / 100)
            + (Math.Max(1, Battle(p).Waves?.Count ?? 1) - 1) * GameData.WaveSeconds
            + GameData.EndSeconds);"""),
# ---- BattleAt: 3 đợt ----
("""        var m = GameData.AreaMonster(MapRealm(p), area);
        var fx = GearEffects(p);                               // hiệu ứng riêng của trang bị theo cấp
        var down = Math.Min(40, SkillBonus(p).monAtk + fx.MonAtkDown);   // công pháp khống chế + Thiên uy làm yếu đòn quái
        if (down > 0) m = m with { Atk = (int)Math.Round(m.Atk * (1 - down / 100)) };
        var (skillPct, pen, burnPct) = SkillExtra(p);                   // Hỏa Tụ / Liệt Diễm Tâm / Xích Diễm Đạo Thể · Hỏa Linh Chân Ý
        pen = Math.Min(80, pen + fx.Pen); skillPct += fx.SkillPct;      // Phá giáp · Linh áp
        if (pen > 0) m = m with { Def = (int)Math.Round(m.Def * (1 - pen / 100)) };""",
 """        var fx = GearEffects(p);                               // hiệu ứng riêng của trang bị theo cấp
        var down = Math.Min(40, SkillBonus(p).monAtk + fx.MonAtkDown);   // công pháp khống chế + Thiên uy làm yếu đòn quái
        var (skillPct, pen, burnPct) = SkillExtra(p);                   // Hỏa Tụ / Liệt Diễm Tâm / Xích Diễm Đạo Thể · Hỏa Linh Chân Ý
        pen = Math.Min(80, pen + fx.Pen); skillPct += fx.SkillPct;      // Phá giáp · Linh áp
        // các đợt quái của ải (ải thường 3 đợt, ải Boss 1): đã trừ công (khống chế) và thủ (phá giáp)
        var waves = GameData.AreaWaves(MapRealm(p), area).Select(w => w with {
            Atk = down > 0 ? (int)Math.Round(w.Atk * (1 - down / 100)) : w.Atk,
            Def = pen > 0 ? (int)Math.Round(w.Def * (1 - pen / 100)) : w.Def }).ToArray();
        var wave = 0; var m = waves[0];"""),
("""        var heroDmg = Math.Max(1, (int)Math.Round(bpower * bdef.DmgBase * (1 + skillPct / 100) / 100) - m.Def);""",
 """        var heroBase = (int)Math.Round(bpower * bdef.DmgBase * (1 + skillPct / 100) / 100);
        int HeroDmgVs(MonsterDef x) => Math.Max(1, heroBase - x.Def);
        var heroDmg = HeroDmgVs(m);"""),
("""        var takeK = trait is null ? 1 : 1 - trait.DmgReduce / 100;""",
 """        double TakeK(MonsterDef x) => x.Tier == "elite" ? 1 - GameData.EliteDmgReduce / 100 : trait is null ? 1 : 1 - trait.DmgReduce / 100;   // tinh anh: giảm 5% sát thương nhận
        var takeK = TakeK(m);"""),
("""        int monHp = m.Hp, heroHp = st.Hp, turns = 0, rage = (int)fx.Rage; var win = false;   // Tĩnh tâm: vào trận đã có nộ""",
 """        int monHp = m.Hp, heroHp = st.Hp, turns = 0, rage = (int)fx.Rage, tw = 0; var win = false;   // Tĩnh tâm: vào trận đã có nộ · tw: lượt trong đợt
        var waveFirst = new List<int> { 0 };                                  // lượt (thật) bắt đầu mỗi đợt
        bool NextWave(int t)                                                  // hạ xong đợt này: quái đợt sau bước vào, máu tu sĩ giữ nguyên
        {
            if (wave + 1 >= waves.Length) return false;
            wave++; m = waves[wave]; heroDmg = HeroDmgVs(m); monDmg = MonHit(m.Atk); monDmgEnr = monDmg; takeK = TakeK(m);
            monHp = m.Hp; tw = -1; waveFirst.Add(t + 1);
            return true;
        }"""),
("        for (var t = 0; t < 60; t++)\n        {\n            var dmg = 0;\n            if (burn > 0 && t > 0)",
 "        for (var t = 0; t < 150; t++, tw++)\n        {\n            var dmg = 0;\n            if (burn > 0 && tw > 0)"),
("            var burned = burnDmg > 0 && t > 0 && rng.Next(100) < SkillData.BurnChance;   // Phần Cốt: 30% mỗi lượt, từ lượt 2",
 "            var burned = burnDmg > 0 && tw > 0 && rng.Next(100) < SkillData.BurnChance;   // Phần Cốt: 30% mỗi lượt, từ lượt 2 của mỗi đợt"),
("            if (monHp <= 0) { win = true; heroLeft.Add(heroHp); skip.Add(true); heals.Add(0); reflects.Add(0); guards.Add(guardLeft); break; }\n            var stunned = t < stun;",
 "            if (monHp <= 0) { heroLeft.Add(heroHp); skip.Add(true); heals.Add(0); reflects.Add(0); guards.Add(guardLeft); if (NextWave(t)) continue; win = true; break; }\n            var stunned = tw < stun;                                            // khống: các lượt đầu của mỗi đợt"),
("            if (monHp <= 0) { win = true; break; }                              // quái gục vì phản đòn",
 "            if (monHp <= 0) { if (NextWave(t)) continue; win = true; break; }   // quái gục vì phản đòn"),
("""        var hits = Math.Min(GameData.MaxHits, turns);
        int Pick(int i) => turns <= hits ? i : (int)Math.Round((i + 1) * (double)turns / hits) - 1;
        var idx = Enumerable.Range(0, hits).Select(Pick).ToArray();""",
 """        // (ải 3 đợt: mỗi đợt tối đa MaxWaveHits lượt, luôn giữ lượt hạ gục để client đổi quái đúng lúc)
        LastTurns = turns;
        var cap = waves.Length > 1 ? GameData.MaxWaveHits : GameData.MaxHits;
        var idxL = new List<int>(); var waveDtos = new List<WaveDto>();
        for (var w = 0; w < waveFirst.Count; w++)
        {
            int s0 = waveFirst[w], n = (w + 1 < waveFirst.Count ? waveFirst[w + 1] : turns) - s0;
            if (n <= 0) break;
            var h = Math.Min(cap, n); var start = idxL.Count;
            for (var i = 0; i < h; i++) idxL.Add(s0 + (n <= h ? i : (int)Math.Round((i + 1) * (double)n / h) - 1));
            var wm = waves[w];
            waveDtos.Add(new WaveDto(wm.Name, wm.Image, wm.Tier, wm.Hp, wm.Atk, wm.Def, HeroDmgVs(wm), MonHit(wm.Atk), start));
        }
        var idx = idxL.ToArray(); var hits = idx.Length; var m0 = waves[0];"""),
("""        return new BattleDto(m.Name, m.Image, m.Hp, m.Atk, m.Def, heroDmg, monDmg, hits, win,""",
 """        return new BattleDto(m0.Name, m0.Image, m0.Hp, m0.Atk, m0.Def, waveDtos[0].HeroDmg, waveDtos[0].MonsterDmg, hits, win,"""),
("""            enrageAt < 0 ? -1 : Array.FindIndex(idx, i => i >= enrageAt), monDmgEnr);
    }""",
 """            enrageAt < 0 ? -1 : Array.FindIndex(idx, i => i >= enrageAt), monDmgEnr, waveDtos);
    }
    /// <summary>Số lượt thật của trận vừa tính (công cụ mô phỏng cân bằng).</summary>
    [ThreadStatic] public static int LastTurns;"""),
])
