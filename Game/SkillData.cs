namespace VanDao.Game;

/// <summary>Hệ tu luyện (ngũ hành và các hệ biến dị). Open = đã có cây kỹ năng chơi được.</summary>
public record ElementDef(string Id, string Name, string Color, string Color2, bool Open, string Root, string Desc);

/// <summary>
/// Một kỹ năng trên cây của một hệ. Row/Col là vị trí trên lưới cây (7 hàng × 5 cột) để mọi hệ dùng chung một khung.
/// Req: "id:cấp" — mọi điều kiện phải đạt. Chỉ số cộng thêm tính theo mỗi cấp.
/// </summary>
public record SkillDef(string Id, string Element, string Name, string Category, int Row, int Col, int MaxLv, int MinRealm, int Cost,
    string[] Req, string Effect, string Anim, double Cooldown = 0, int DmgBase = 0, int DmgPerLv = 0,
    double AtkPct = 0, double DefPct = 0, double HpPct = 0, double Crit = 0, double Speed = 0, double Move = 0,
    double QiPct = 0, double MonAtkDown = 0,
    double SkillPct = 0,       // % sát thương cộng thêm cho mọi chiêu hệ Hỏa tự thi triển
    double Pen = 0,            // % phòng thủ của quái bị bỏ qua (xuyên thấu)
    int Every = 0,             // (cũ) hồi chiêu theo lượt — không còn dùng: kỹ năng chủ động nay thi triển khi đầy nộ
    string Code = "",          // Skill ID (FIRE_ACTIVE_001…)
    string AnimId = "NONE",    // Animation ID (bị động: NONE). Animation chỉ để hiển thị, không chứa logic
    double Lead = 0,           // bị động: giá trị = mức/cấp × (cấp + Lead) — cho phép Lv.1 cao hơn tuyến tính (vd. 5% → 20%)
    double BurnPct = 0)        // bị động: thiêu đốt = % công (mỗi cấp), BurnChance% kích hoạt mỗi lượt từ lượt 2, bỏ qua phòng thủ
{
    /// <summary>Điểm cần để lên từ cấp lv lên lv+1: lĩnh ngộ tốn Cost, mỗi lần nâng tốn 1 + lv/2.</summary>
    public int CostAt(int lv) => lv == 0 ? Cost : 1 + lv / 2;
    /// <summary>Hệ số cấp cho chỉ số bị động.</summary>
    public double Factor(int lv) => lv <= 0 ? 0 : lv + Lead;
    /// <summary>Cây: basic (đòn thường, không dùng điểm) · active · passive.</summary>
    public string Tree => Category == "Đòn thường" ? "basic" : Category is "Căn cơ" or "Bị động" ? "passive" : "active";
}

/// <summary>
/// Hệ thống công pháp theo hệ. Muốn thêm hệ mới: đặt Open = true trong Elements rồi khai báo các SkillDef cùng Element,
/// đặt Row/Col trên lưới 7×5 và Req nối các nút — giao diện tự vẽ cây theo dữ liệu này.
/// </summary>
public static class SkillData
{
    public static readonly IReadOnlyList<ElementDef> Elements = new[]
    {
        new ElementDef("hoa",  "Hỏa",  "#e0521e", "#f8d25a", true,  "Hỏa Linh Căn",  "Lửa linh khí do tu sĩ khống chế: công mạnh, có thủ, có khống, có thân pháp."),
        new ElementDef("thuy", "Thủy", "#3f8fd8", "#6fe0e0", false, "Thủy Linh Căn", "Nước nhu mà bền: phòng ngự, hồi phục, nhấn chìm kẻ địch."),
        new ElementDef("moc",  "Mộc",  "#4f9a5a", "#9fe6a8", false, "Mộc Linh Căn",  "Cỏ cây sinh trưởng: trói buộc, chữa thương, trường sinh."),
        new ElementDef("tho",  "Thổ",  "#b08a4a", "#e6c46a", false, "Thổ Linh Căn",  "Đất đá vững chãi: tường đá, giáp đất, sơn nhạc."),
        new ElementDef("loi",  "Lôi",  "#a46ae6", "#6fb8ff", false, "Lôi Linh Căn",  "Lôi điện nhanh như chớp: lôi bộ, xích điện, thiên lôi."),
        new ElementDef("bang", "Băng", "#a8dcf0", "#ffffff", false, "Băng Linh Căn", "Hàn khí đóng băng: băng tiễn, băng thuẫn, tuyệt đối linh độ."),
        new ElementDef("gio",  "Phong","#8fd0b0", "#e6fff4", false, "Phong Linh Căn","Gió vô hình: thân pháp, phong nhận, cuồng phong."),
    };

    public static readonly IReadOnlyList<SkillDef> Skills = new SkillDef[]
    {
        // ===== HỎA: Đánh thường (2, không dùng điểm) + Chủ động (4) + Bị động (7). Mọi hệ sau dùng cùng khung =====
        // Row/Col: vị trí trong cây của chính nó (chủ động 3 hàng × 3 cột, bị động 5 hàng × 3 cột).
        // ---- ĐÁNH THƯỜNG: luôn có, không có cấp, mạnh lên theo cảnh giới / chỉ số / trang bị / công pháp bị động ----
        new("hoa_cau", "hoa", "Hỏa Cầu", "Đòn thường", 0, 0, 0, 0, 0, Array.Empty<string>(),
            "Ngưng tụ hỏa cầu nhỏ bắn thẳng vào một mục tiêu. Đơn mục tiêu, tầm xa.", "bolt", 0, 115, 0,
            Code: "FIRE_BASIC_001", AnimId: "FIRE_BOLT_001"),
        new("hoa_tien", "hoa", "Hỏa Tiễn", "Đòn thường", 0, 1, 0, 0, 0, Array.Empty<string>(),
            "Bắn liên tiếp nhiều hỏa tiễn nhỏ, dễ trúng chỗ hiểm. Đa mục tiêu / bộc phát, tầm xa.", "arrow", 0, 105, 0, Crit: 15,
            Code: "FIRE_BASIC_002", AnimId: "FIRE_ARROW_001"),
        // ---- CHỦ ĐỘNG ----
        new("liet_diem", "hoa", "Liệt Diễm", "Công", 0, 1, 5, 0, 1, Array.Empty<string>(),
            "Ngưng tụ linh lực Hỏa hệ thành một vụ bùng cháy dữ dội, thiêu đốt kẻ địch xung quanh.", "burst", 6, 150, 15, AtkPct: 2, Every: 3,
            Code: "FIRE_ACTIVE_001", AnimId: "FIRE_AOE_001"),
        new("hoa_long", "hoa", "Hỏa Long Thuật", "Công", 1, 0, 5, 1, 3, new[] { "liet_diem:3" },
            "Triệu hồi một con hỏa long linh khí lao về phía kẻ địch. Sát thương đơn mục tiêu cao, va chạm diện nhỏ, tầm xa.", "dragon", 10, 200, 20, AtkPct: 3, Every: 4,
            Code: "FIRE_ACTIVE_002", AnimId: "FIRE_DRAGON_001"),
        new("phan_thien", "hoa", "Phần Thiên", "Công", 1, 2, 5, 1, 5, new[] { "liet_diem:5" },
            "Triệu gọi cột linh hỏa khổng lồ giáng từ trên cao. Diện rộng, sát thương cao, hồi chiêu dài.", "pillar", 14, 240, 25, AtkPct: 3, Every: 6,
            Code: "FIRE_ACTIVE_003", AnimId: "FIRE_HEAVEN_001"),
        new("xich_diem_thien", "hoa", "Xích Diễm Thiên", "Tuyệt kỹ", 2, 1, 5, 2, 10, new[] { "hoa_long:5", "phan_thien:5" },
            "Giải phóng thượng cổ hỏa long cùng vụ nổ linh hỏa quy mô lớn. Sát thương cực cao, diện rộng.", "ultimate", 30, 350, 40, AtkPct: 5, Every: 8,
            Code: "FIRE_ULTIMATE_001", AnimId: "FIRE_ULTIMATE_001"),
        // ---- BỊ ĐỘNG (không có animation) ----
        new("hoa_linh_can", "hoa", "Hỏa Linh Căn", "Căn cơ", 0, 1, 5, 0, 1, Array.Empty<string>(),
            "Linh căn hệ Hỏa trong cơ thể (mở thân hòa Hỏa). Tăng tấn công và tốc độ hấp thu linh khí.", "root", AtkPct: 2, QiPct: 2, Code: "FIRE_ROOT_001"),
        new("hoa_tu", "hoa", "Hỏa Tụ", "Bị động", 1, 1, 5, 0, 1, new[] { "hoa_linh_can:1" },
            "Tụ hỏa linh trong kinh mạch: tăng sát thương Hỏa (Lv.1 +5% → Lv.5 +20%).", "passive", SkillPct: 3.75, Lead: 1.0 / 3, Code: "FIRE_PASSIVE_001"),
        new("phan_cot", "hoa", "Phần Cốt", "Bị động", 1, 2, 5, 0, 1, new[] { "hoa_tu:1" },
            "Hỏa độc bám xương: từ lượt 2, mỗi lượt có 30% cơ hội thiêu đốt quái (Lv.1 1% → Lv.5 5% công), bỏ qua phòng thủ.", "passive", BurnPct: 1, Code: "FIRE_PASSIVE_007"),
        new("nhiet_huyet", "hoa", "Nhiệt Huyết", "Bị động", 2, 0, 5, 0, 1, new[] { "hoa_tu:2" },
            "Máu nóng sục sôi: tăng tỉ lệ chí mạng Hỏa (Lv.1 +3% → Lv.5 +15%).", "passive", Crit: 3, Code: "FIRE_PASSIVE_002"),
        new("hoa_the", "hoa", "Hỏa Thể", "Bị động", 2, 1, 5, 0, 1, new[] { "hoa_tu:2" },
            "Thân thể quen lửa: kháng Hỏa (Lv.1 +5% → Lv.5 +20%), tính là giảm sát thương nhận vào qua phòng thủ.", "passive", DefPct: 3.75, Lead: 1.0 / 3, Code: "FIRE_PASSIVE_003"),
        new("hoa_tam", "hoa", "Hỏa Tâm", "Bị động", 2, 2, 5, 1, 2, new[] { "hoa_tu:3" },
            "Tâm hỏa bình ổn: chiêu Hỏa +3% sát thương mỗi cấp (Lv.5 +15%).", "passive", SkillPct: 3, Code: "FIRE_PASSIVE_004"),
        new("liet_diem_tam", "hoa", "Liệt Diễm Tâm", "Bị động", 3, 1, 5, 1, 2, new[] { "liet_diem:3", "hoa_tam:3" },
            "Tâm như liệt diễm: chiêu Hỏa +10% sát thương (ở Lv.5).", "passive", SkillPct: 2, Code: "FIRE_PASSIVE_005"),
        new("hoa_linh_chan_y", "hoa", "Hỏa Linh Chân Ý", "Bị động", 4, 1, 1, 2, 5, new[] { "liet_diem:5", "hoa_long:3", "liet_diem_tam:5" },
            "Bị động cao cấp: chiêu Hỏa +20% sát thương, +10% xuyên thấu.", "passive", SkillPct: 20, Pen: 10, Code: "FIRE_PASSIVE_006"),

        // ===== các hệ chưa mở: khung cây để hiển thị (cùng lưới) =====
        new("thuy_linh_can", "thuy", "Thủy Linh Căn", "Căn cơ", 0, 2, 5, 0, 1, Array.Empty<string>(), "Linh căn hệ Thủy.", "root"),
        new("thuy_nhan", "thuy", "Thủy Nhận", "Công", 2, 1, 5, 0, 1, new[] { "thuy_linh_can:1" }, "Lưỡi nước chém địch.", "bolt"),
        new("thuy_thuan", "thuy", "Thủy Thuẫn", "Thủ", 2, 3, 5, 0, 1, new[] { "thuy_linh_can:1" }, "Màn nước chắn đòn.", "shield"),
        new("hong_thuy", "thuy", "Hồng Thủy", "Khống", 4, 2, 5, 1, 2, new[] { "thuy_nhan:3", "thuy_thuan:3" }, "Nước dâng cuốn trôi kẻ địch.", "field"),
        new("thuong_hai", "thuy", "Thương Hải Quyết", "Tuyệt kỹ", 6, 2, 3, 2, 5, new[] { "hong_thuy:3" }, "Sức mạnh biển cả.", "ultimate"),
        new("moc_linh_can", "moc", "Mộc Linh Căn", "Căn cơ", 0, 2, 5, 0, 1, Array.Empty<string>(), "Linh căn hệ Mộc.", "root"),
        new("dang_man", "moc", "Đằng Mạn", "Khống", 2, 1, 5, 0, 1, new[] { "moc_linh_can:1" }, "Dây leo trói địch.", "bind"),
        new("hoi_xuan", "moc", "Hồi Xuân", "Thủ", 2, 3, 5, 0, 1, new[] { "moc_linh_can:1" }, "Chữa thương.", "aura"),
        new("lam_khoa", "moc", "Lâm Khóa", "Khống", 4, 2, 5, 1, 2, new[] { "dang_man:3", "hoi_xuan:3" }, "Rừng cây giam địch.", "field"),
        new("co_moc", "moc", "Cổ Mộc Quyết", "Tuyệt kỹ", 6, 2, 3, 2, 5, new[] { "lam_khoa:3" }, "Thần mộc thượng cổ.", "ultimate"),
        new("tho_linh_can", "tho", "Thổ Linh Căn", "Căn cơ", 0, 2, 5, 0, 1, Array.Empty<string>(), "Linh căn hệ Thổ.", "root"),
        new("thach_dan", "tho", "Thạch Đạn", "Công", 2, 1, 5, 0, 1, new[] { "tho_linh_can:1" }, "Đạn đá.", "bolt"),
        new("tho_tuong", "tho", "Thổ Tường", "Thủ", 2, 3, 5, 0, 1, new[] { "tho_linh_can:1" }, "Tường đất.", "shield"),
        new("tho_giap", "tho", "Thổ Giáp", "Thủ", 4, 2, 5, 1, 2, new[] { "thach_dan:3", "tho_tuong:3" }, "Giáp đất.", "aura"),
        new("son_nhac", "tho", "Sơn Nhạc Quyết", "Tuyệt kỹ", 6, 2, 3, 2, 5, new[] { "tho_giap:3" }, "Sức nặng núi non.", "ultimate"),
        new("loi_linh_can", "loi", "Lôi Linh Căn", "Căn cơ", 0, 2, 5, 0, 1, Array.Empty<string>(), "Linh căn hệ Lôi.", "root"),
        new("loi_kich", "loi", "Lôi Kích", "Công", 2, 1, 5, 0, 1, new[] { "loi_linh_can:1" }, "Sét đánh.", "bolt"),
        new("loi_bo", "loi", "Lôi Bộ", "Thân pháp", 2, 3, 5, 0, 1, new[] { "loi_linh_can:1" }, "Bước chân sấm sét.", "dash"),
        new("lien_loi", "loi", "Liên Hoàn Lôi", "Công", 4, 2, 5, 1, 2, new[] { "loi_kich:3", "loi_bo:3" }, "Xích sét.", "field"),
        new("thien_loi", "loi", "Thiên Lôi", "Tuyệt kỹ", 6, 2, 3, 2, 5, new[] { "lien_loi:3" }, "Sấm trời.", "ultimate"),
        new("bang_linh_can", "bang", "Băng Linh Căn", "Căn cơ", 0, 2, 5, 0, 1, Array.Empty<string>(), "Linh căn hệ Băng.", "root"),
        new("bang_tien", "bang", "Băng Tiễn", "Công", 2, 1, 5, 0, 1, new[] { "bang_linh_can:1" }, "Mũi băng.", "bolt"),
        new("bang_thuan", "bang", "Băng Thuẫn", "Thủ", 2, 3, 5, 0, 1, new[] { "bang_linh_can:1" }, "Khiên băng.", "shield"),
        new("dong_dia", "bang", "Đông Địa", "Khống", 4, 2, 5, 1, 2, new[] { "bang_tien:3", "bang_thuan:3" }, "Đất đóng băng.", "field"),
        new("tuyet_doi", "bang", "Tuyệt Đối Linh Độ", "Tuyệt kỹ", 6, 2, 3, 2, 5, new[] { "dong_dia:3" }, "Hàn khí tuyệt đối.", "ultimate"),
        new("phong_linh_can", "gio", "Phong Linh Căn", "Căn cơ", 0, 2, 5, 0, 1, Array.Empty<string>(), "Linh căn hệ Phong.", "root"),
        new("phong_nhan", "gio", "Phong Nhận", "Công", 2, 1, 5, 0, 1, new[] { "phong_linh_can:1" }, "Lưỡi gió.", "bolt"),
        new("phong_bo", "gio", "Phong Bộ", "Thân pháp", 2, 3, 5, 0, 1, new[] { "phong_linh_can:1" }, "Bước gió.", "dash"),
        new("cuong_phong", "gio", "Cuồng Phong", "Khống", 4, 2, 5, 1, 2, new[] { "phong_nhan:3", "phong_bo:3" }, "Gió lốc.", "field"),
        new("thien_phong", "gio", "Thiên Phong Quyết", "Tuyệt kỹ", 6, 2, 3, 2, 5, new[] { "cuong_phong:3" }, "Gió trời.", "ultimate"),
    };

    public static readonly IReadOnlyDictionary<string, SkillDef> ById = Skills.ToDictionary(s => s.Id);

    /// <summary>2 ô chiến đấu: ô 0 = đòn đánh thường (mỗi lượt), ô 1 = kỹ năng chủ động (Công hoặc Tuyệt kỹ), thi triển khi đầy nộ.</summary>
    public const int SkillSlotCount = 2, BasicSlot = 0, UltimateSlot = 1;

    /// <summary>Nộ: mỗi đòn đánh thường tích RagePerHit, đủ RageMax thì lượt kế tiếp tung kỹ năng chủ động THAY cho đòn thường, nộ về 0 (4 đòn thường → 1 kỹ năng).</summary>
    public const int RagePerHit = 25, RageMax = 100;

    /// <summary>Thiêu đốt (Phần Cốt): % cơ hội kích hoạt mỗi lượt, từ lượt 2.</summary>
    public const int BurnChance = 30;
    public static bool FitsSlot(SkillDef s, int slot) =>
        slot == BasicSlot ? s.Category == "Đòn thường" : s.Category is "Công" or "Tuyệt kỹ" or "Khống" or "Thủ" or "Thân pháp";

    /// <summary>
    /// Hiệu ứng khi kỹ năng được đặt vào ô và tự thi triển trong trận (cấp lv):
    /// Chiêu công không còn theo hồi chiêu mà thi triển khi đầy nộ (xem RagePerHit / RageMax). Shield = % giảm sát thương nhận, Stun = số lượt quái bị khống,
    /// Dodge = % né đòn, Burn = % sát thương thiêu đốt mỗi lượt.
    /// </summary>
    public static (int every, int first, double shield, int stun, double dodge, double burn) Active(SkillDef s, int lv) =>
        (0, 0, 0, 0, 0, 0);

    /// <summary>Sát thương (% công) của kỹ năng ở cấp lv.</summary>
    public static int Damage(SkillDef s, int lv) => s.DmgBase == 0 ? 0 : s.DmgBase + s.DmgPerLv * Math.Max(0, lv - 1);

    /// <summary>Linh căn bẩm sinh: được cấp 1 miễn phí.</summary>
    public const string StartSkill = "hoa_linh_can";

    /// <summary>Điểm kỹ năng nhận được: 1 lúc đầu, +1 mỗi tầng/kỳ đã lên, +3 mỗi đại cảnh giới, +2 mỗi lần hạ Boss.</summary>
    public static int PointsEarned(int level, int realm, int bossKills) => 1 + (level - 1) + 3 * realm + 2 * bossKills;
}
