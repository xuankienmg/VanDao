namespace VanDao.Game;

/// <summary>Hệ tu luyện (ngũ hành và các hệ biến dị). Open = đã có cây kỹ năng chơi được.</summary>
public record ElementDef(string Id, string Name, string Color, string Color2, bool Open, string Root, string Desc);

/// <summary>
/// Một kỹ năng trên cây của một hệ. Row/Col là vị trí trên lưới cây (7 hàng × 5 cột) để mọi hệ dùng chung một khung.
/// Req: "id:cấp" — mọi điều kiện phải đạt. Chỉ số cộng thêm tính theo mỗi cấp.
/// </summary>
public record SkillDef(string Id, string Element, string Name, string Category, int Row, int Col, int MaxLv, int MinRealm, int Cost,
    string[] Req, string Effect, string Anim, int QiCost = 0, double Cooldown = 0, int DmgBase = 0, int DmgPerLv = 0,
    double AtkPct = 0, double DefPct = 0, double HpPct = 0, double Crit = 0, double Speed = 0, double Move = 0,
    double QiPct = 0, double MonAtkDown = 0)
{
    /// <summary>Điểm cần để lên từ cấp lv lên lv+1.</summary>
    public int CostAt(int lv) => Cost + lv / 2;
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
        new ElementDef("kim",  "Kim",  "#c9ccd8", "#ffffff", false, "Kim Linh Căn",  "Kim khí sắc bén: kiếm quang, giáp cứng, kiếm vũ."),
        new ElementDef("tho",  "Thổ",  "#b08a4a", "#e6c46a", false, "Thổ Linh Căn",  "Đất đá vững chãi: tường đá, giáp đất, sơn nhạc."),
        new ElementDef("loi",  "Lôi",  "#a46ae6", "#6fb8ff", false, "Lôi Linh Căn",  "Lôi điện nhanh như chớp: lôi bộ, xích điện, thiên lôi."),
        new ElementDef("bang", "Băng", "#a8dcf0", "#ffffff", false, "Băng Linh Căn", "Hàn khí đóng băng: băng tiễn, băng thuẫn, tuyệt đối linh độ."),
        new ElementDef("gio",  "Phong","#8fd0b0", "#e6fff4", false, "Phong Linh Căn","Gió vô hình: thân pháp, phong nhận, cuồng phong."),
    };

    public static readonly IReadOnlyList<SkillDef> Skills = new SkillDef[]
    {
        // ===== HỎA: gốc =====
        new("hoa_linh_can", "hoa", "Hỏa Linh Căn", "Căn cơ", 0, 2, 5, 0, 1, Array.Empty<string>(),
            "Linh căn hệ Hỏa trong cơ thể. Tăng tấn công và tốc độ hấp thu linh khí.", "root", AtkPct: 2, QiPct: 2),
        new("hoa_cau", "hoa", "Hỏa Cầu", "Công", 1, 2, 5, 0, 1, new[] { "hoa_linh_can:1" },
            "Ngưng tụ một quả cầu lửa linh khí bắn thẳng về phía địch.", "bolt", 30, 3, 100, 25, AtkPct: 3),
        // ===== nhánh Công =====
        new("hoa_tien", "hoa", "Hỏa Tiễn", "Công", 2, 1, 5, 0, 1, new[] { "hoa_cau:2" },
            "Bắn ba mũi tên lửa liên tiếp, dễ trúng chỗ hiểm.", "arrow", 40, 4, 60, 15, AtkPct: 3, Crit: 1),
        new("liet_diem", "hoa", "Liệt Diễm", "Công", 3, 1, 5, 0, 2, new[] { "hoa_tien:3" },
            "Liệt hỏa bùng lên quanh mục tiêu, thiêu đốt liên tục.", "burst", 60, 6, 150, 30, AtkPct: 4),
        new("hoa_long", "hoa", "Hỏa Long Thuật", "Công", 4, 2, 5, 1, 3, new[] { "liet_diem:3", "hoa_giap:3" },
            "Hóa linh hỏa thành rồng lửa lao vào kẻ địch.", "dragon", 120, 10, 260, 45, AtkPct: 6),
        new("phan_thien", "hoa", "Phần Thiên", "Công", 5, 2, 5, 1, 3, new[] { "hoa_long:3" },
            "Cột lửa thiêu trời từ dưới chân kẻ địch bốc lên.", "pillar", 180, 14, 380, 60, AtkPct: 8, Crit: 2),
        // ===== nhánh Thủ =====
        new("hoa_thuan", "hoa", "Hỏa Thuẫn", "Thủ", 2, 3, 5, 0, 1, new[] { "hoa_cau:2" },
            "Tấm khiên lửa chặn đòn trước mặt.", "shield", 35, 8, DefPct: 4),
        new("hoa_giap", "hoa", "Hỏa Giáp", "Thủ", 3, 3, 5, 0, 2, new[] { "hoa_thuan:3" },
            "Lửa bám thân thành giáp, vừa chống đỡ vừa đốt kẻ chạm vào.", "aura", 50, 12, HpPct: 4, DefPct: 2),
        new("ho_the", "hoa", "Hộ Thể Liệt Diễm", "Thủ", 4, 4, 5, 1, 2, new[] { "hoa_giap:3" },
            "Vòng liệt diễm bao quanh thân, đòn đánh khó xuyên qua.", "aura", 80, 15, HpPct: 5, DefPct: 3),
        // ===== nhánh Khống =====
        new("hoa_troi", "hoa", "Hỏa Trói", "Khống", 2, 0, 5, 0, 1, new[] { "hoa_cau:1" },
            "Xích lửa quấn lấy địch, làm chậm và yếu đòn đánh.", "bind", 30, 7, MonAtkDown: 2),
        new("liet_diem_lao", "hoa", "Liệt Diễm Lao", "Khống", 3, 0, 5, 0, 2, new[] { "hoa_troi:3" },
            "Lồng song lửa giam kẻ địch tại chỗ.", "bind", 60, 10, MonAtkDown: 3),
        new("hoa_tran", "hoa", "Hỏa Trận", "Khống", 4, 0, 5, 1, 2, new[] { "liet_diem_lao:3" },
            "Bày trận lửa dưới chân, kẻ địch trong trận bị thiêu và suy yếu.", "field", 90, 14, 80, 20, AtkPct: 2, MonAtkDown: 3),
        // ===== nhánh Thân pháp =====
        new("hoa_bo", "hoa", "Hỏa Bộ", "Thân pháp", 2, 4, 5, 0, 1, new[] { "hoa_linh_can:2" },
            "Bước chân mang lửa, di chuyển nhanh hơn.", "dash", 20, 5, Move: 3),
        new("viem_don", "hoa", "Viêm Độn", "Thân pháp", 3, 4, 5, 1, 2, new[] { "hoa_bo:3" },
            "Hóa thân thành lửa lướt đi trong chớp mắt.", "dash", 50, 10, Move: 3, Speed: 3),
        // ===== Tuyệt kỹ =====
        new("xich_diem_thien", "hoa", "Xích Diễm Thiên", "Tuyệt kỹ", 6, 2, 3, 2, 5, new[] { "phan_thien:3" },
            "Nhuộm đỏ cả bầu trời, liệt diễm từ trời giáng xuống thiêu rụi vạn vật.", "ultimate", 400, 40, 900, 200, AtkPct: 15, Crit: 5),

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
        new("kim_linh_can", "kim", "Kim Linh Căn", "Căn cơ", 0, 2, 5, 0, 1, Array.Empty<string>(), "Linh căn hệ Kim.", "root"),
        new("kim_nhan", "kim", "Kim Nhận", "Công", 2, 1, 5, 0, 1, new[] { "kim_linh_can:1" }, "Lưỡi kim khí.", "bolt"),
        new("kim_giap", "kim", "Kim Giáp", "Thủ", 2, 3, 5, 0, 1, new[] { "kim_linh_can:1" }, "Giáp kim cương.", "shield"),
        new("kiem_vu", "kim", "Kiếm Vũ", "Công", 4, 2, 5, 1, 2, new[] { "kim_nhan:3", "kim_giap:3" }, "Mưa kiếm.", "field"),
        new("thien_kim", "kim", "Thiên Kim Quyết", "Tuyệt kỹ", 6, 2, 3, 2, 5, new[] { "kiem_vu:3" }, "Kim khí trời đất.", "ultimate"),
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

    /// <summary>Số ô: 3 ô chiêu thức (Công, Thủ, Khống, Thân pháp) + 1 ô tuyệt kỹ.</summary>
    public const int SkillSlotCount = 4, UltimateSlot = 3;
    public static bool FitsSlot(SkillDef s, int slot) =>
        slot == UltimateSlot ? s.Category == "Tuyệt kỹ" : s.Category is "Công" or "Thủ" or "Khống" or "Thân pháp";

    /// <summary>
    /// Hiệu ứng khi kỹ năng được đặt vào ô và tự thi triển trong trận (cấp lv):
    /// Every = mấy lượt dùng một lần (0 = chỉ lượt đầu), Shield = % giảm sát thương nhận, Stun = số lượt quái bị khống,
    /// Dodge = % né đòn, Burn = % sát thương thiêu đốt mỗi lượt.
    /// </summary>
    public static (int every, int first, double shield, int stun, double dodge, double burn) Active(SkillDef s, int lv) => s.Id switch
    {
        "hoa_cau"         => (3, 0, 0, 0, 0, 0),
        "hoa_tien"        => (4, 1, 0, 0, 0, 0),
        "liet_diem"       => (5, 1, 0, 0, 0, 0),
        "hoa_long"        => (8, 1, 0, 0, 0, 0),
        "phan_thien"      => (10, 1, 0, 0, 0, 0),
        "xich_diem_thien" => (0, 1, 0, 0, 0, 0),
        "hoa_thuan"       => (0, 0, Math.Min(40, 8 + 4 * lv), 0, 0, 0),
        "hoa_giap"        => (0, 0, Math.Min(40, 5 + 3 * lv), 0, 0, 0),
        "ho_the"          => (0, 0, Math.Min(50, 10 + 4 * lv), 0, 0, 0),
        "hoa_troi"        => (0, 0, 0, lv >= 3 ? 2 : 1, 0, 0),
        "liet_diem_lao"   => (0, 0, 0, lv >= 3 ? 3 : 2, 0, 0),
        "hoa_tran"        => (0, 0, 0, 1, 0, 25),
        "hoa_bo"          => (0, 0, 0, 0, Math.Min(30, 4 + 2 * lv), 0),
        "viem_don"        => (0, 0, 0, 0, Math.Min(40, 6 + 3 * lv), 0),
        _                 => (0, 0, 0, 0, 0, 0),
    };

    /// <summary>Sát thương (% công) của kỹ năng ở cấp lv.</summary>
    public static int Damage(SkillDef s, int lv) => s.DmgBase == 0 ? 0 : s.DmgBase + s.DmgPerLv * Math.Max(0, lv - 1);

    /// <summary>Linh căn bẩm sinh: được cấp 1 miễn phí.</summary>
    public const string StartSkill = "hoa_linh_can";

    /// <summary>Điểm kỹ năng nhận được: 1 lúc đầu, +1 mỗi tầng/kỳ đã lên, +3 mỗi đại cảnh giới, +2 mỗi lần hạ Boss.</summary>
    public static int PointsEarned(int level, int realm, int bossKills) => 1 + (level - 1) + 3 * realm + 2 * bossKills;
}
