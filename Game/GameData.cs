namespace VanDao.Game;

/// <summary>
/// Vật phẩm. Trang bị có Slot và chỉ số cộng thêm (đơn vị %):
/// Atk = tấn công, Hp = máu, Def = phòng thủ (giá trị cộng thẳng);
/// QiPct = tốc độ tu vi, LuckPct = tỉ lệ rơi đồ, SpeedPct = tốc độ thám hiểm (đơn vị %).
/// </summary>
public record ItemDef(string Id, string Name, string Type, string Color, string Desc,
    string? Icon = null, string? Slot = null, double QiPct = 0, double LuckPct = 0, double SpeedPct = 0,
    int Atk = 0, int Hp = 0, int Def = 0);

/// <summary>Chỉ số chiến đấu.</summary>
public record CombatStats(int Atk, int Hp, int Def);

/// <summary>Quái vật gặp khi thám hiểm ở một cảnh giới.</summary>
public record MonsterDef(string Name, string Image, int Hp, int Atk, int Def);

public record SlotDef(string Id, string Name);

public record DropDef(string Item, int Min, int Max, double Chance);

public record RecipeDef(string Name, string OutItem, int OutCount, Dictionary<string, int> In, double Rate, int MinRealm);

public class RealmDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Desc { get; init; }
    public required string[] Stages { get; init; }
    public required string Aura { get; init; }
    public required string Lotus { get; init; }
    public required string Sash { get; init; }

    /// <summary>Ảnh nhân vật khi đi thám hiểm ở cảnh giới này.</summary>
    public required string Hero { get; init; }

    /// <summary>Tu vi cần để đầy bình cảnh ở tầng/kỳ s (0-based).</summary>
    public required Func<int, double> QiReq { get; init; }

    /// <summary>Tu vi tự tăng mỗi giây ở tầng/kỳ s.</summary>
    public required Func<int, double> Rate { get; init; }

    /// <summary>Vật liệu để đi từ tầng s lên s+1 trong cùng cảnh giới.</summary>
    public required Func<int, Dictionary<string, int>> StageCost { get; init; }

    /// <summary>Vật liệu đột phá sang cảnh giới kế tiếp. null = chưa mở cảnh giới sau.</summary>
    public Dictionary<string, int>? BreakCost { get; init; }
    public double BreakChance { get; init; }

    /// <summary>Hệ số chỉ số gốc của tu sĩ ở cảnh giới này.</summary>
    public required int StatMult { get; init; }

    /// <summary>Quái vật chặn đường khi thám hiểm.</summary>
    public required MonsterDef Monster { get; init; }
    public required string[] Places { get; init; }
    public required DropDef[] Drops { get; init; }
    public required string[] Resources { get; init; }
}

/// <summary>
/// Toàn bộ dữ liệu cân bằng game.
/// Muốn thêm cảnh giới (vd. Nguyên Anh): thêm RealmDef vào cuối Realms,
/// khai báo vật phẩm mới trong Items, công thức trong Recipes,
/// rồi điền BreakCost/BreakChance cho Kim Đan.
/// </summary>
public static class GameData
{
    public static readonly IReadOnlyList<ItemDef> Items = new[]
    {
        new ItemDef("lt_ha",          "Hạ phẩm linh thạch",     "stone", "#8fd0c9", "Linh thạch phổ biến, dùng cho tu sĩ Luyện Khí."),
        new ItemDef("lt_trung",       "Trung phẩm linh thạch",  "stone", "#7aa8ff", "Linh khí tinh thuần, tài nguyên chính của Trúc Cơ."),
        new ItemDef("lt_thuong",      "Thượng phẩm linh thạch", "stone", "#e6c46a", "Hiếm có, chỉ tu sĩ Kim Đan mới hấp thu nổi."),
        new ItemDef("linh_thao",      "Linh thảo",              "herb",  "#86c97f", "Thảo dược cơ bản để luyện Tụ Khí Đan và Trúc Cơ Đan."),
        new ItemDef("huyet_sam",      "Huyết sâm trăm năm",     "herb",  "#d86a5f", "Bồi bổ khí huyết, nguyên liệu Bồi Nguyên Đan."),
        new ItemDef("hoa_linh_chi",   "Hỏa linh chi",           "herb",  "#f0913f", "Mọc bên miệng núi lửa, nguyên liệu Kim Tủy Đan."),
        new ItemDef("yeu_dan_1",      "Yêu đan nhất giai",      "core",  "#b4b4d8", "Lấy từ yêu thú cấp thấp."),
        new ItemDef("yeu_dan_2",      "Yêu đan nhị giai",       "core",  "#a385ff", "Yêu lực dồi dào, cần cho Ngưng Kim Đan."),
        new ItemDef("yeu_dan_3",      "Yêu đan tam giai",       "core",  "#ff74a8", "Yêu đan của đại yêu, cần cho Kim Tủy Đan."),
        new ItemDef("canh_kim",       "Canh kim tinh",          "ore",   "#dcdce6", "Tinh hoa kim loại, giúp ngưng tụ kim đan."),
        new ItemDef("tu_khi_dan",     "Tụ Khí Đan",             "pill",  "#bfeee2", "Dùng để lên tầng Luyện Khí."),
        new ItemDef("truc_co_dan",    "Trúc Cơ Đan",            "pill",  "#6fd39a", "Bắt buộc khi đột phá Trúc Cơ."),
        new ItemDef("boi_nguyen_dan", "Bồi Nguyên Đan",         "pill",  "#86b6ff", "Củng cố đạo cơ, dùng để lên kỳ Trúc Cơ."),
        new ItemDef("ngung_kim_dan",  "Ngưng Kim Đan",          "pill",  "#f2c94c", "Bắt buộc khi đột phá Kim Đan."),
        new ItemDef("kim_tuy_dan",    "Kim Tủy Đan",            "pill",  "#ffd98a", "Tôi luyện kim đan, dùng để lên kỳ Kim Đan."),

        // ---- trang bị (ảnh trong wwwroot/img) ----
        new ItemDef("thiet_kiem", "Thiết kiếm",     "equip", "#9aa0a8", "Kiếm sắt thường, chuôi quấn vải, đốc ngọc thô.",       "img/thiet_kiem.png", "weapon", Atk: 6),
        new ItemDef("hac_dao",    "Hắc đao",        "equip", "#7f8794", "Đao bản rộng rèn từ sắt đen, khắc phù văn mờ.",       "img/hac_dao.png",    "weapon", Atk: 10, LuckPct: 5),
        new ItemDef("non_vai",    "Nón vải rách",   "equip", "#6d7078", "Nón vải sờn mép, gắn mảnh linh thạch nhỏ.",           "img/non_vai.png",    "head",   Hp: 15, Def: 1),
        new ItemDef("ao_vai",     "Áo vải thô",     "equip", "#6d7078", "Áo giao lĩnh vá chằng vá đụp của tán tu.",            "img/ao_vai.png",     "body",   Hp: 40, Def: 2),
        new ItemDef("gang_vai",   "Găng tay vải",   "equip", "#5c5f66", "Găng vải dày buộc dây gai.",                          "img/gang_vai.png",   "hands",  Atk: 2, Def: 1),
        new ItemDef("gang_da",    "Găng tay da",    "equip", "#7a6a55", "Găng da thú, mu tay đính linh thạch.",                "img/gang_da.png",    "hands",  Atk: 3, Def: 2),
        new ItemDef("quan_vai",   "Quần vải thô",   "equip", "#6d7078", "Quần vải buộc dây thừng, gối vá.",                    "img/quan_vai.png",   "legs",   Hp: 25, Def: 1),
        new ItemDef("nhan_ngoc",  "Nhẫn ngọc thô",  "equip", "#4f7a64", "Nhẫn ngọc xanh khắc phù văn, linh khí yếu ớt.",       "img/nhan_ngoc.png",  "ring",   QiPct: 5, LuckPct: 8),
    };

    public static readonly IReadOnlyList<SlotDef> Slots = new[]
    {
        new SlotDef("weapon", "Vũ khí"), new SlotDef("head", "Nón"), new SlotDef("body", "Áo"),
        new SlotDef("hands", "Găng tay"), new SlotDef("legs", "Quần"), new SlotDef("ring", "Nhẫn"),
    };

    public static readonly IReadOnlyDictionary<string, ItemDef> ItemById = Items.ToDictionary(i => i.Id);

    public static readonly IReadOnlyList<RealmDef> Realms = new[]
    {
        new RealmDef
        {
            Id = "luyen_khi", Name = "Luyện Khí",
            Desc = "Dẫn linh khí trời đất nhập thể, tẩy kinh phạt tủy. Mỗi tầng cần linh thạch hạ phẩm và Tụ Khí Đan.",
            Stages = new[] { "Tầng 1", "Tầng 2", "Tầng 3", "Tầng 4", "Tầng 5", "Tầng 6", "Tầng 7", "Tầng 8", "Tầng 9" },
            Aura = "#7fd3c4", Lotus = "#d9a3b0", Sash = "#3f8378", Hero = "img/hero_luyenkhi.png",
            QiReq = s => Math.Round(60 * Math.Pow(1.38, s)),
            Rate = s => 3 + s * 0.6,
            StageCost = s => new() { ["lt_ha"] = 5 + 5 * s, ["tu_khi_dan"] = s < 3 ? 1 : 2 },
            BreakCost = new() { ["truc_co_dan"] = 1, ["lt_ha"] = 100 }, BreakChance = 0.55,
            StatMult = 1,
            Monster = new MonsterDef("Yêu lang", "img/yeu_lang.png", Hp: 30, Atk: 8, Def: 2),
            Places = new[] { "Hắc Phong Lâm", "Thanh Vân Cốc", "Hậu sơn tông môn" },
            Drops = new[] { new DropDef("lt_ha", 3, 8, 1), new DropDef("linh_thao", 1, 3, .85), new DropDef("tu_khi_dan", 1, 1, .3), new DropDef("yeu_dan_1", 1, 1, .3),
                new DropDef("ao_vai", 1, 1, .04), new DropDef("quan_vai", 1, 1, .04), new DropDef("non_vai", 1, 1, .03),
                new DropDef("gang_vai", 1, 1, .03), new DropDef("thiet_kiem", 1, 1, .02), new DropDef("nhan_ngoc", 1, 1, .01) },
            Resources = new[] { "lt_ha", "linh_thao", "tu_khi_dan", "yeu_dan_1", "truc_co_dan" },
        },
        new RealmDef
        {
            Id = "truc_co", Name = "Trúc Cơ",
            Desc = "Linh khí hóa lỏng, xây nền đạo cơ. Mỗi kỳ cần linh thạch trung phẩm và Bồi Nguyên Đan.",
            Stages = new[] { "Sơ kỳ", "Trung kỳ", "Hậu kỳ", "Đại viên mãn" },
            Aura = "#86b6ff", Lotus = "#9fd6c8", Sash = "#3b5f9a", Hero = "img/hero_trucco.png",
            QiReq = s => Math.Round(1500 * Math.Pow(1.6, s)),
            Rate = s => 18 + s * 6,
            StageCost = s => new() { ["lt_trung"] = 3 * (s + 1), ["boi_nguyen_dan"] = s + 1 },
            BreakCost = new() { ["ngung_kim_dan"] = 1, ["lt_trung"] = 30 }, BreakChance = 0.4,
            StatMult = 6,
            Monster = new MonsterDef("Huyết lang", "img/yeu_lang.png", Hp: 200, Atk: 60, Def: 20),
            Places = new[] { "Huyết Sắc Cấm Địa", "Vạn Thú Sơn", "Đoạn Hồn Nhai" },
            Drops = new[] { new DropDef("lt_trung", 1, 3, 1), new DropDef("lt_ha", 10, 25, .8), new DropDef("huyet_sam", 1, 2, .75), new DropDef("boi_nguyen_dan", 1, 1, .2), new DropDef("yeu_dan_2", 1, 1, .3), new DropDef("canh_kim", 1, 1, .25),
                new DropDef("gang_da", 1, 1, .04), new DropDef("hac_dao", 1, 1, .025), new DropDef("nhan_ngoc", 1, 1, .03),
                new DropDef("ao_vai", 1, 1, .02), new DropDef("quan_vai", 1, 1, .02), new DropDef("non_vai", 1, 1, .02) },
            Resources = new[] { "lt_trung", "huyet_sam", "boi_nguyen_dan", "yeu_dan_2", "canh_kim", "ngung_kim_dan" },
        },
        new RealmDef
        {
            Id = "kim_dan", Name = "Kim Đan",
            Desc = "Ngưng tụ kim đan nơi đan điền, thọ nguyên năm trăm năm. Mỗi kỳ cần linh thạch thượng phẩm và Kim Tủy Đan.",
            Stages = new[] { "Sơ kỳ", "Trung kỳ", "Hậu kỳ", "Đại viên mãn" },
            Aura = "#f2c94c", Lotus = "#f0d58a", Sash = "#9a6b1f", Hero = "img/hero_trucco.png",
            QiReq = s => Math.Round(18000 * Math.Pow(1.7, s)),
            Rate = s => 140 + s * 50,
            StageCost = s => new() { ["lt_thuong"] = 2 * (s + 1), ["kim_tuy_dan"] = s + 1 },
            BreakCost = null, BreakChance = 0, // điền khi thêm Nguyên Anh
            StatMult = 35,
            Monster = new MonsterDef("Lang vương", "img/yeu_lang.png", Hp: 1200, Atk: 300, Def: 100),
            Places = new[] { "Hỏa Diễm Sơn", "Táng Tiên Cốc", "Đông Hải bí cảnh" },
            Drops = new[] { new DropDef("lt_thuong", 1, 2, 1), new DropDef("lt_trung", 3, 8, .8), new DropDef("hoa_linh_chi", 1, 2, .75), new DropDef("kim_tuy_dan", 1, 1, .15), new DropDef("yeu_dan_3", 1, 1, .35),
                new DropDef("hac_dao", 1, 1, .04), new DropDef("gang_da", 1, 1, .04), new DropDef("nhan_ngoc", 1, 1, .04) },
            Resources = new[] { "lt_thuong", "hoa_linh_chi", "kim_tuy_dan", "yeu_dan_3" },
        },
    };

    /// <summary>Cảnh giới hiển thị trên thanh tiến trình nhưng chưa mở.</summary>
    public static readonly string[] LockedRealms = { "Nguyên Anh", "Hóa Thần" };

    public static readonly IReadOnlyList<RecipeDef> Recipes = new[]
    {
        new RecipeDef("Tụ Khí Đan",           "tu_khi_dan",     1, new() { ["linh_thao"] = 3, ["lt_ha"] = 5 }, 1.0, 0),
        new RecipeDef("Trúc Cơ Đan",          "truc_co_dan",    1, new() { ["linh_thao"] = 12, ["yeu_dan_1"] = 3, ["lt_ha"] = 40 }, .75, 0),
        new RecipeDef("Hợp luyện linh thạch", "lt_trung",       1, new() { ["lt_ha"] = 50 }, 1.0, 1),
        new RecipeDef("Bồi Nguyên Đan",       "boi_nguyen_dan", 1, new() { ["huyet_sam"] = 2, ["lt_trung"] = 1 }, .9, 1),
        new RecipeDef("Ngưng Kim Đan",        "ngung_kim_dan",  1, new() { ["huyet_sam"] = 8, ["yeu_dan_2"] = 3, ["canh_kim"] = 2 }, .65, 1),
        new RecipeDef("Hợp luyện linh thạch", "lt_thuong",      1, new() { ["lt_trung"] = 30 }, 1.0, 2),
        new RecipeDef("Kim Tủy Đan",          "kim_tuy_dan",    1, new() { ["hoa_linh_chi"] = 2, ["yeu_dan_3"] = 1, ["lt_thuong"] = 1 }, .85, 2),
    };

    public static readonly Dictionary<string, int> StartingInventory = new() { ["lt_ha"] = 20, ["linh_thao"] = 6 };

    /// <summary>Thời gian vắng mặt tối đa được cộng tu vi và thám hiểm bù.</summary>
    public const double MaxOfflineSeconds = 7200;

    /// <summary>Chỉ số gốc ở tầng/kỳ s, nhân với StatMult của cảnh giới.</summary>
    public static CombatStats BaseStats(RealmDef r, int s) =>
        new((10 + 2 * s) * r.StatMult, (100 + 15 * s) * r.StatMult, (2 + s) * r.StatMult);

    /// <summary>Thời lượng các pha của một chuyến (giây): đi đường, mỗi lượt đánh, kết thúc.</summary>
    public const double WalkSeconds = 2.5, HitSeconds = 1.0, EndSeconds = 1.0;

    /// <summary>Số lượt đánh tối đa của một trận (tránh trận kéo dài vô hạn khi quá yếu).</summary>
    public const int MaxHits = 10;

    /// <summary>Mỗi chuyến thám hiểm cho thêm tu vi bằng tốc độ tu luyện × số giây này.</summary>
    public const double ExploreQiSeconds = 4;

    /// <summary>Thời gian thám hiểm tối thiểu dù trang bị tăng tốc nhiều.</summary>
    public const double MinExploreSeconds = 3;

    /// <summary>Hệ số tu vi mỗi lần bấm Vận công (nhân với tốc độ tu luyện).</summary>
    public const double ChannelMultiplier = 1.2;
}
