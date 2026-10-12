namespace VanDao.Game;

/// <summary>
/// Vật phẩm. Trang bị có Slot và chỉ số cộng thêm (đơn vị %):
/// Atk = tấn công, Hp = máu, Def = phòng thủ (giá trị cộng thẳng);
/// QiPct = tốc độ tu vi, LuckPct = tỉ lệ rơi đồ, SpeedPct = tốc độ thám hiểm (đơn vị %).
/// </summary>
public record ItemDef(string Id, string Name, string Type, string Color, string Desc,
    string? Icon = null, string? Slot = null, double QiPct = 0, double LuckPct = 0, double SpeedPct = 0,
    int Atk = 0, int Hp = 0, int Def = 0,
    int MinRealm = 0,          // cảnh giới tối thiểu để mặc
    double SpiritPct = 0,      // Linh lực: % sát thương cộng thêm (mở ở Kim Đan)
    string? Tint = null,       // bộ lọc màu CSS để phân biệt bậc trang bị dùng chung ảnh
    string Rarity = "common",  // common · uncommon · rare · epic · legendary
    string? Kind = null,       // loại hiển thị: Kiếm, Đao, Thương, Áo, Đan dược…
    double CritPct = 0,        // Bạo kích %
    double MovePct = 0,        // Tốc chạy %: rút ngắn đoạn đi đường
    double UseQiSeconds = 0,   // dùng được: cộng tu vi = tốc độ tu luyện × số giây này
    string? Special = null,    // dòng thuộc tính đặc biệt hiển thị trong tooltip
    int Tier = 0,              // bậc đan trong chuỗi hợp thành 9→1 (0 = không phải đan)
    string? Use = null,        // công dụng của đan / chìa khóa / nguyên liệu Boss
    int Sell = 0,              // giá bán (linh thạch); chỉ trang bị bán được
    string? Set = null);       // bộ trang bị (hiện thành dòng màu riêng trong thẻ vật phẩm)

/// <summary>
/// Chỉ số chiến đấu. Speed (Thân pháp), Sense (Thần thức), Spirit (Linh lực) tính bằng %
/// và chỉ có tác dụng khi cảnh giới đã mở chỉ số đó.
/// </summary>
public record CombatStats(int Atk, int Hp, int Def, double Speed = 0, double Sense = 0, double Spirit = 0, double Crit = 0, double Move = 0);

/// <summary>Chỉ số phụ mở theo cảnh giới.</summary>
public record StatDef(string Id, string Name, string Desc, int MinRealm);

/// <summary>Quái vật gặp khi thám hiểm ở một cảnh giới.</summary>
public record MonsterDef(string Name, string Image, int Hp, int Atk, int Def);

public record SlotDef(string Id, string Name);

public record DropDef(string Item, int Min, int Max, double Chance);

/// <summary>Hợp thành: 9 viên đan From + phí linh thạch → 1 viên đan To. Luôn thành công.</summary>
public record SynthDef(string From, string To, int Count, int Stones);

public class RealmDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Desc { get; init; }
    public required string[] Stages { get; init; }
    public required string Aura { get; init; }
    public required string Lotus { get; init; }
    public required string Sash { get; init; }

    /// <summary>Tên chữ Hán của cảnh giới.</summary>
    public required string Cn { get; init; }

    /// <summary>Số ô hành trang ở cảnh giới này.</summary>
    public required int BagSlots { get; init; }

    /// <summary>Các ô trang bị được dùng ở cảnh giới này.</summary>
    public required string[] EquipSlots { get; init; }

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

    /// <summary>Quái vật chặn đường khi thám hiểm: chỉ số gốc (tên/ảnh mặc định nếu không có MonsterNames).</summary>
    public required MonsterDef Monster { get; init; }

    /// <summary>Tên 7 loài quái theo thứ tự GameData.MonsterImages (ải 1-20 chia đều, mạnh dần); null = dùng Monster.</summary>
    public string[]? MonsterNames { get; init; }
    public required string[] Places { get; init; }

    /// <summary>Boss ở ải 21 (cần chìa khóa để vào), rơi nguyên liệu Boss cho đột phá đại cảnh giới.</summary>
    public required MonsterDef Boss { get; init; }
    public required string BossLair { get; init; }
    public required string KeyItem { get; init; }
    public required string BossItem { get; init; }
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
        new ItemDef("linh_thach",    "Linh thạch",       "stone", "#8fd0c9", "Tiền tệ chung của giới tu tiên: dùng để tu luyện, hợp thành đan và đột phá.", Rarity: "common", Kind: "Linh thạch"),
        // ---- chuỗi đan 9→1: đan chỉ dùng để lên tầng và đột phá ----
        new ItemDef("tu_khi_dan",    "Tụ Khí Đan",       "pill",  "#bfeee2", "Đan nhập môn, rơi từ yêu thú vùng Luyện Khí.", Rarity: "common", Kind: "Đan dược", Tier: 1,
            Use: "Dùng để đột phá các tầng Luyện Khí. 9 viên hợp thành 1 Trúc Cơ Đan."),
        new ItemDef("truc_co_dan",   "Trúc Cơ Đan",      "pill",  "#6fd39a", "Đan xây nền đạo cơ.", Rarity: "rare", Kind: "Đan dược", Tier: 2,
            Use: "Dùng để đột phá đại cảnh giới Luyện Khí → Trúc Cơ, và lên kỳ trong Trúc Cơ."),
        new ItemDef("ngung_kim_dan", "Ngưng Kim Đan",    "pill",  "#f2c94c", "Đan ngưng tụ kim đan nơi đan điền.", Rarity: "epic", Kind: "Đan dược", Tier: 3,
            Use: "Dùng để đột phá đại cảnh giới Trúc Cơ → Kim Đan, và lên kỳ trong Kim Đan."),
        new ItemDef("ket_anh_dan",   "Kết Anh Đan",      "pill",  "#c58cff", "Đan phá đan thành anh, cực kỳ hiếm có.", Rarity: "legendary", Kind: "Đan dược", Tier: 4,
            Use: "Dùng để đột phá Kim Đan → Nguyên Anh (cảnh giới chưa khai mở)."),
        // ---- chìa khóa vào ải Boss (rơi ở ải 1–20) ----
        new ItemDef("hu_thien_tan_do", "Hư Thiên Tàn Đồ",  "key", "#c9a86a", "Mảnh bản đồ cổ dẫn tới Hư Thiên Điện, nơi Hắc Lân Mãng ẩn mình.", Rarity: "epic", Kind: "Chìa khóa",
            Use: "Cần để vào ải Boss 1-21 · Hư Thiên Điện. Chỉ mất khi thắng Boss."),
        new ItemDef("huyet_sac_lenh",  "Huyết Sắc Lệnh Bài", "key", "#c0463a", "Lệnh bài đỏ thẫm mở cửa trung tâm Huyết Sắc Cấm Địa.", Rarity: "epic", Kind: "Chìa khóa",
            Use: "Cần để vào ải Boss 2-21 · Cấm Địa Trung Tâm. Chỉ mất khi thắng Boss."),
        new ItemDef("con_ngo_lenh",    "Côn Ngô Sơn Lệnh",   "key", "#e6c46a", "Lệnh bài vàng khắc phù trận phong ấn Côn Ngô Sơn.", Rarity: "epic", Kind: "Chìa khóa",
            Use: "Cần để vào ải Boss 3-21 · Côn Ngô Sơn. Chỉ mất khi thắng Boss."),
        // ---- nguyên liệu Boss: bắt buộc khi đột phá đại cảnh giới ----
        new ItemDef("hac_lan_yeu_hach",  "Hắc Lân Yêu Hạch",  "boss", "#3d6fd8", "Yêu hạch của Hắc Lân Mãng, vảy đen bao quanh một đốm u quang.", Rarity: "legendary", Kind: "Nguyên liệu Boss",
            Use: "Bắt buộc khi đột phá Luyện Khí Tầng 9 → Trúc Cơ."),
        new ItemDef("huyet_giao_hach",   "Huyết Giao Yêu Hạch", "boss", "#d8404a", "Yêu hạch của Huyết Giao, nóng như máu sôi.", Rarity: "legendary", Kind: "Nguyên liệu Boss",
            Use: "Bắt buộc khi đột phá Trúc Cơ Đại viên mãn → Kim Đan."),
        new ItemDef("kim_o_tinh_hach",   "Kim Ô Tinh Hạch",    "boss", "#f2b23c", "Tinh hạch của Kim Ô yêu vương, chứa thái dương chân hỏa.", Rarity: "legendary", Kind: "Nguyên liệu Boss",
            Use: "Dùng để đột phá Kim Đan → Nguyên Anh (cảnh giới chưa khai mở)."),

        // ---- trang bị (ảnh trong wwwroot/img) ----
        new ItemDef("thiet_kiem", "Thiết kiếm",     "equip", "#9aa0a8", "Kiếm sắt thường, chuôi quấn vải, đốc ngọc thô.",       "img/thiet_kiem.png", "weapon", Atk: 6, Rarity: "common", Kind: "Kiếm", CritPct: 4, Special: "Kiếm pháp: +4% Bạo kích"),
        new ItemDef("hac_dao",    "Hắc đao",        "equip", "#7f8794", "Đao bản rộng rèn từ sắt đen, khắc phù văn mờ.",       "img/hac_dao.png",    "weapon", Atk: 10, LuckPct: 5, Rarity: "uncommon", Kind: "Đao", CritPct: 2, Special: "Đao thế nặng: tăng may mắn khi nhặt đồ"),
        new ItemDef("non_vai",    "Nón vải rách",   "equip", "#6d7078", "Nón vải sờn mép, gắn mảnh linh thạch nhỏ.",           "img/non_vai.png",    "head",   Hp: 15, Def: 1, Rarity: "common", Kind: "Nón"),
        new ItemDef("ao_vai",     "Áo vải thô",     "equip", "#6d7078", "Áo giao lĩnh vá chằng vá đụp của tán tu.",            "img/ao_vai.png",     "body",   Hp: 40, Def: 2, Rarity: "common", Kind: "Đạo bào"),
        new ItemDef("gang_vai",   "Găng tay vải",   "equip", "#5c5f66", "Găng vải dày buộc dây gai.",                          "img/gang_vai.png",   "hands",  Atk: 2, Def: 1, Rarity: "common", Kind: "Găng tay"),
        new ItemDef("gang_da",    "Găng tay da",    "equip", "#7a6a55", "Găng da thú, mu tay đính linh thạch.",                "img/gang_da.png",    "hands",  Atk: 3, Def: 2, Rarity: "uncommon", Kind: "Găng tay"),
        new ItemDef("quan_vai",   "Quần vải thô",   "equip", "#6d7078", "Quần vải buộc dây thừng, gối vá.",                    "img/quan_vai.png",   "legs",   Hp: 25, Def: 1, Rarity: "common", Kind: "Quần"),
        new ItemDef("nhan_ngoc",  "Nhẫn ngọc thô",  "equip", "#4f7a64", "Nhẫn ngọc xanh khắc phù văn, linh khí yếu ớt.",       "img/nhan_ngoc.png",  "ring",   QiPct: 5, LuckPct: 8, Rarity: "uncommon", Kind: "Nhẫn"),
        // trang bị Trúc Cơ / Kim Đan: sẽ thêm bộ mới có ảnh riêng (bản cũ dùng lại ảnh Luyện Khí đổi màu đã bỏ)
    };

    // ---- vật phẩm mới cho màn hình Hành trang (icon pixel trong wwwroot/img) ----
    public static readonly ItemDef[] ExtraItems =
    {
        new("truong_thuong", "Trường thương",     "equip", "#a07a50", "Thương gỗ cán dài, mũi sắt, buộc tua đỏ.",            "img/truong_thuong.png", "weapon", Atk: 8, CritPct: 3, Rarity: "uncommon", Kind: "Thương", Special: "Đâm xa: +3% Bạo kích"),
        new("giay_vai",      "Giày vải",          "equip", "#5c5f66", "Giày vải đế mỏng, đi đường núi đỡ mỏi.",              "img/giay_vai.png", "feet", Def: 1, MovePct: 6, Rarity: "common", Kind: "Giày"),
        new("ngoc_boi",      "Ngọc bội thô",      "equip", "#50aa78", "Ngọc bội đeo hông, linh khí mỏng manh.",               "img/ngoc_boi.png", "necklace", Hp: 20, QiPct: 3, Rarity: "uncommon", Kind: "Dây chuyền"),

        // ---- bộ Luyện Khí Giới (Linh phẩm, nền xanh lục): bậc trên đồ vải thường của Luyện Khí. Ảnh: tools/cut_items.py ----
        new("lk_kiem", "Giới Kiếm", "equip", "#5fbf6a", "Kiếm đen chuôi buộc tua đỏ, bội kiếm của đệ tử Luyện Khí Giới.", "img/lk_kiem.png", "weapon", Atk: 13, CritPct: 5, Rarity: "uncommon", Kind: "Kiếm", Special: "Kiếm pháp: +5% Bạo kích", Set: "Luyện Khí Giới"),
        new("lk_mu",   "Giới Mũ",   "equip", "#5fbf6a", "Mũ vải dày khóa đồng, giữ đầu óc tỉnh táo khi tọa thiền.",   "img/lk_mu.png",   "head",     Hp: 30, Def: 3, QiPct: 2, Rarity: "uncommon", Kind: "Mũ", Set: "Luyện Khí Giới"),
        new("lk_ao",   "Giới Bào",  "equip", "#5fbf6a", "Đạo bào lam thẫm viền chỉ đỏ, dệt lẫn sợi linh tơ.",        "img/lk_ao.png",   "body",     Hp: 85, Def: 5, Rarity: "uncommon", Kind: "Đạo bào", Set: "Luyện Khí Giới"),
        new("lk_gang", "Giới Thủ Sáo", "equip", "#5fbf6a", "Găng hở ngón tán đinh bạc, ra quyền nhanh mà chắc.",     "img/lk_gang.png", "hands",    Atk: 4, Def: 3, Rarity: "uncommon", Kind: "Găng tay", Set: "Luyện Khí Giới"),
        new("lk_giay", "Giới Hài",  "equip", "#5fbf6a", "Ủng vải cổ cao bọc da, đi đường núi không mỏi.",            "img/lk_giay.png", "feet",     Hp: 20, Def: 3, MovePct: 9, Rarity: "uncommon", Kind: "Giày", Set: "Luyện Khí Giới"),
        new("lk_bua",  "Giới Phù",  "equip", "#5fbf6a", "Xâu bùa giấy vẽ phù văn ngọc quang, đeo bên hông hộ thân.", "img/lk_bua.png",  "necklace", Hp: 30, QiPct: 5, LuckPct: 3, Rarity: "uncommon", Kind: "Bùa", Set: "Luyện Khí Giới"),
        new("lk_nhan", "Giới Nhẫn", "equip", "#5fbf6a", "Nhẫn đồng khảm lam ngọc, tụ linh khí quanh ngón tay.",      "img/lk_nhan.png", "ring",     QiPct: 7, LuckPct: 10, Rarity: "uncommon", Kind: "Nhẫn", Set: "Luyện Khí Giới"),
        // pháp bảo nhập môn: ô Pháp bảo mở từ Trúc Cơ
        new("pb_holo",  "Linh Hồ Lô",     "equip", "#5fbf6a", "Hồ lô gỗ khắc phù, nuốt linh khí rồi phun ra thành kình lực.", "img/pb_holo.png",  "offhand", Atk: 22, QiPct: 6, MinRealm: 1, Rarity: "uncommon", Kind: "Pháp bảo"),
        new("pb_guong", "Hộ Tâm Kính",    "equip", "#5fbf6a", "Gương đồng soi tâm, phản lại một phần yêu khí.",               "img/pb_guong.png", "offhand", Hp: 160, Def: 14, MinRealm: 1, Rarity: "uncommon", Kind: "Pháp bảo"),
        new("pb_kiem",  "Thanh Văn Kiếm", "equip", "#5fbf6a", "Đoản kiếm vân xanh, ngự khí bay quanh thân hộ chủ.",           "img/pb_kiem.png",  "offhand", Atk: 18, CritPct: 4, MinRealm: 1, Rarity: "uncommon", Kind: "Pháp bảo", Special: "Ngự kiếm: +4% Bạo kích") };

    /// <summary>Chỉ số phụ mở theo cảnh giới (hiện mờ khi chưa mở).</summary>
    public static readonly IReadOnlyList<StatDef> ExtraStats = new[]
    {
        new StatDef("crit",   "Bạo kích",  "Tỉ lệ đánh chí mạng (×1.5 sát thương), tính theo kỳ vọng.", 0),
        new StatDef("move",   "Tốc chạy",  "Rút ngắn đoạn đi đường mỗi chuyến.", 0),
        new StatDef("speed",  "Thân pháp", "Đánh nhanh hơn: rút ngắn mỗi lượt giao chiến.", 1),
        new StatDef("sense",  "Thần thức", "Tăng tỉ lệ rơi vật phẩm.", 1),
        new StatDef("spirit", "Linh lực",  "Tăng sát thương mỗi đòn đánh.", 2) };

    /// <summary>Chỉ số phụ gốc theo cảnh giới (Thân pháp, Thần thức, Linh lực, đơn vị %).</summary>
    public static readonly (double Speed, double Sense, double Spirit)[] BaseExtra =
    {
        (0, 0, 0), (5, 5, 0), (10, 10, 10) };

    public static readonly IReadOnlyList<SlotDef> Slots = new[]
    {
        new SlotDef("head", "Nón"), new SlotDef("body", "Áo"), new SlotDef("hands", "Găng tay"),
        new SlotDef("legs", "Quần"), new SlotDef("feet", "Giày"),
        new SlotDef("necklace", "Dây chuyền"), new SlotDef("ring", "Nhẫn 1"), new SlotDef("ring2", "Nhẫn 2"),
        new SlotDef("weapon", "Vũ khí"), new SlotDef("offhand", "Pháp bảo") };

    /// <summary>Vật phẩm có ảnh pixel 48×48 trong wwwroot/img/{id}.png.</summary>
    private static readonly HashSet<string> PixelIcons = new()
    {
        "linh_thach", "tu_khi_dan", "truc_co_dan", "ngung_kim_dan", "ket_anh_dan",
        "hu_thien_tan_do", "huyet_sac_lenh", "con_ngo_lenh", "hac_lan_yeu_hach", "huyet_giao_hach", "kim_o_tinh_hach" };

    /// <summary>Giá gốc theo độ hiếm; trang bị cảnh giới cao bán đắt hơn (×5 mỗi cảnh giới).</summary>
    public static readonly Dictionary<string, int> RarityPrice = new()
    {
        ["common"] = 3, ["uncommon"] = 12, ["rare"] = 50, ["epic"] = 200, ["legendary"] = 800 };
    public static readonly string[] RarityOrder = { "common", "uncommon", "rare", "epic", "legendary" };
    public static int SellPrice(ItemDef it) =>
        it.Slot is null ? 0 : Math.Max(1, RarityPrice.GetValueOrDefault(it.Rarity, 3) * (1 + it.MinRealm * 4));

    public static readonly IReadOnlyList<ItemDef> AllItems = Items.Concat(ExtraItems)
        .Select(it => PixelIcons.Contains(it.Id) ? it with { Icon = $"img/{it.Id}.png" } : it)
        .Select(it => it with { Sell = SellPrice(it) }).ToList();

    /// <summary>Chuỗi đan: 9 viên bậc dưới → 1 viên bậc trên.</summary>
    public static readonly string[] PillChain = { "tu_khi_dan", "truc_co_dan", "ngung_kim_dan", "ket_anh_dan" };
    public static readonly IReadOnlyList<SynthDef> Synths = new[]
    {
        new SynthDef("tu_khi_dan",    "truc_co_dan",   9, 50),
        new SynthDef("truc_co_dan",   "ngung_kim_dan", 9, 800),
        new SynthDef("ngung_kim_dan", "ket_anh_dan",   9, 8000) };

    /// <summary>Vật phẩm của bản cũ quy đổi khi nạp nhân vật: (id mới, hệ số). Không có id mới = đổi ra linh thạch.</summary>
    public static readonly Dictionary<string, (string To, int Factor)> Legacy = new()
    {
        ["lt_ha"] = ("linh_thach", 1), ["lt_trung"] = ("linh_thach", 50), ["lt_thuong"] = ("linh_thach", 1500),
        ["boi_nguyen_dan"] = ("truc_co_dan", 1), ["kim_tuy_dan"] = ("ngung_kim_dan", 1),
        ["hoi_khi_dan"] = ("linh_thach", 20), ["luyen_the_dan"] = ("linh_thach", 30), ["hoa_tinh"] = ("linh_thach", 20),
        ["linh_thao"] = ("linh_thach", 2), ["huyet_sam"] = ("linh_thach", 20), ["hoa_linh_chi"] = ("linh_thach", 300),
        ["yeu_dan_1"] = ("linh_thach", 10), ["yeu_dan_2"] = ("linh_thach", 100), ["yeu_dan_3"] = ("linh_thach", 1000),
        ["canh_kim"] = ("linh_thach", 150), ["lenh_bai"] = ("linh_thach", 50) };

    /// <summary>Đột phá đại cảnh giới: phần tỉ lệ đại thành công trong thành công, phần tẩu hỏa nhập ma trong thất bại.</summary>
    public const double BreakCritShare = .2, BreakMajorShare = .25;
    /// <summary>Thất bại: tổn tu vi, thời gian điều tức trước lần thử sau (giây).</summary>
    public const double BreakFailLoss = .10, BreakMajorLoss = .25, BreakFailCooldown = 60, BreakMajorCooldown = 120;
    /// <summary>Tẩu hỏa nhập ma: suy yếu trong DebuffSeconds (tu vi ×0.7, công/thủ ×0.9).</summary>
    public const double DebuffSeconds = 300, DebuffRate = .7, DebuffStat = .9;
    /// <summary>Đại thành công: đạo cơ vững chắc, thêm vĩnh viễn 10% máu và phòng thủ gốc của cảnh giới mới.</summary>
    public const double BreakCritBonus = .10;

    /// <summary>Số ải thường mỗi cảnh giới; ải kế tiếp (21) là ải Boss.</summary>
    public const int Areas = 20;
    public const int BossArea = Areas + 1;

    /// <summary>Quái ở ải s (1..20): mạnh dần từ ×0.6 đến ×2.6 so với quái gốc của cảnh giới.</summary>
    /// <summary>Hệ số máu chung cho mọi quái và Boss (1.875 = +87.5% so với gốc).</summary>
    public const double MonsterHpMult = 1.875;   // 1.25 × 1.5: tăng thêm 50% máu quái

    /// <summary>Ảnh 7 loài quái thường, dùng chung mọi cảnh giới (tên riêng từng cảnh giới ở RealmDef.MonsterNames).
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
        if (s >= BossArea) return r.Boss with { Hp = (int)Math.Round(r.Boss.Hp * MonsterHpMult) };
        var m = 0.6 + 2.0 * (Math.Clamp(s, 1, Areas) - 1) / (Areas - 1);
        var b = r.Monster;
        if (r.MonsterNames is { } names) { var k = MonsterKind(s); b = b with { Name = names[k], Image = MonsterImages[k] }; }
        return b with { Hp = (int)Math.Round(b.Hp * m * MonsterHpMult), Atk = (int)Math.Round(b.Atk * m), Def = (int)Math.Round(b.Def * m) };
    }

    /// <summary>Tên vùng của ải s.</summary>
    public static string AreaPlace(RealmDef r, int s) =>
        s >= BossArea ? r.BossLair : r.Places[Math.Min(r.Places.Length - 1, (Math.Clamp(s, 1, Areas) - 1) * r.Places.Length / Areas)];

    /// <summary>Tỉ lệ rơi chìa khóa Boss ở ải s.</summary>
    public static double KeyChance(int s) => 0.004 + 0.0015 * s;

    public static readonly IReadOnlyDictionary<string, ItemDef> ItemById = AllItems.ToDictionary(i => i.Id);

    public static readonly IReadOnlyList<RealmDef> Realms = new[]
    {
        new RealmDef
        {
            Id = "luyen_khi", Name = "Luyện Khí",
            Desc = "Dẫn linh khí trời đất nhập thể, tẩy kinh phạt tủy. Mỗi tầng cần linh thạch và Tụ Khí Đan.",
            Stages = new[] { "Tầng 1", "Tầng 2", "Tầng 3", "Tầng 4", "Tầng 5", "Tầng 6", "Tầng 7", "Tầng 8", "Tầng 9" },
            Aura = "#7fd3c4", Lotus = "#d9a3b0", Sash = "#3f8378", Hero = "img/hero_luyenkhi.png",
            Cn = "", BagSlots = 60, EquipSlots = new[] { "weapon", "head", "body", "hands", "legs", "feet", "necklace", "ring" },
            QiReq = s => Math.Round(60 * Math.Pow(1.38, s)),
            Rate = s => 3 + s * 0.6,
            StageCost = s => new() { ["linh_thach"] = 10 + 10 * s, ["tu_khi_dan"] = s < 3 ? 1 : 2 },
            BreakCost = new() { ["truc_co_dan"] = 1, ["linh_thach"] = 500, ["hac_lan_yeu_hach"] = 1 }, BreakChance = .55,
            StatMult = 1,
            Monster = new MonsterDef("Yêu lang", "img/quai_yeu_lang.png", Hp: 30, Atk: 8, Def: 2),
            MonsterNames = new[] { "Yêu Lang", "Độc Xà", "Thạch Giáp Quy", "Hỏa Hồ", "Băng Lang", "Thiết Giáp Trùng", "Cốt Khuyển" },
            Places = new[] { "Hắc Phong Lâm", "Thanh Vân Cốc", "Hậu sơn tông môn" },
            Boss = new MonsterDef("Hắc Lân Mãng", "img/boss_hac_lan.png", Hp: 140, Atk: 23, Def: 5),
            BossLair = "Hư Thiên Điện", KeyItem = "hu_thien_tan_do", BossItem = "hac_lan_yeu_hach",
            Drops = new[] { new DropDef("linh_thach", 3, 8, 1), new DropDef("tu_khi_dan", 1, 1, .4),
                new DropDef("ao_vai", 1, 1, .04), new DropDef("quan_vai", 1, 1, .04), new DropDef("non_vai", 1, 1, .03),
                new DropDef("gang_vai", 1, 1, .03), new DropDef("thiet_kiem", 1, 1, .02), new DropDef("nhan_ngoc", 1, 1, .01),
                new DropDef("truong_thuong", 1, 1, .02), new DropDef("giay_vai", 1, 1, .03), new DropDef("ngoc_boi", 1, 1, .015),
                // bộ Luyện Khí Giới (Linh phẩm): hiếm hơn đồ vải ~4–5 lần
                new DropDef("lk_ao", 1, 1, .008), new DropDef("lk_mu", 1, 1, .008), new DropDef("lk_gang", 1, 1, .008), new DropDef("lk_giay", 1, 1, .008),
                new DropDef("lk_kiem", 1, 1, .006), new DropDef("lk_bua", 1, 1, .006), new DropDef("lk_nhan", 1, 1, .005) },
            Resources = new[] { "linh_thach", "tu_khi_dan", "hu_thien_tan_do", "hac_lan_yeu_hach" } },
        new RealmDef
        {
            Id = "truc_co", Name = "Trúc Cơ",
            Desc = "Linh khí hóa lỏng, xây nền đạo cơ. Mỗi kỳ cần linh thạch và Trúc Cơ Đan.",
            Stages = new[] { "Sơ kỳ", "Trung kỳ", "Hậu kỳ", "Đại viên mãn" },
            Aura = "#86b6ff", Lotus = "#9fd6c8", Sash = "#3b5f9a", Hero = "img/hero_trucco.png",
            Cn = "", BagSlots = 100, EquipSlots = new[] { "weapon", "offhand", "head", "body", "hands", "legs", "feet", "necklace", "ring", "ring2" },
            QiReq = s => Math.Round(1500 * Math.Pow(1.6, s)),
            Rate = s => 18 + s * 6,
            StageCost = s => new() { ["linh_thach"] = 200 * (s + 1), ["truc_co_dan"] = s + 1 },
            BreakCost = new() { ["ngung_kim_dan"] = 1, ["linh_thach"] = 3000, ["huyet_giao_hach"] = 1 }, BreakChance = .4,
            StatMult = 6,
            Monster = new MonsterDef("Huyết lang", "img/quai_yeu_lang.png", Hp: 200, Atk: 60, Def: 20),
            MonsterNames = new[] { "Huyết Yêu Lang", "Huyết Độc Xà", "Huyết Giáp Quy", "Huyết Hỏa Hồ", "Huyết Băng Lang", "Huyết Giáp Trùng", "Huyết Cốt Khuyển" },
            Places = new[] { "Huyết Sắc Cấm Địa", "Vạn Thú Sơn", "Đoạn Hồn Nhai" },
            Boss = new MonsterDef("Huyết Giao", "img/boss_huyet_giao.png", Hp: 940, Atk: 172, Def: 52),
            BossLair = "Cấm Địa Trung Tâm", KeyItem = "huyet_sac_lenh", BossItem = "huyet_giao_hach",
            Drops = new[] { new DropDef("linh_thach", 30, 80, 1), new DropDef("tu_khi_dan", 1, 3, .8), new DropDef("truc_co_dan", 1, 1, .05),
                // pháp bảo nhập môn (ô Pháp bảo mở ở Trúc Cơ)
                new DropDef("pb_holo", 1, 1, .012), new DropDef("pb_guong", 1, 1, .012), new DropDef("pb_kiem", 1, 1, .008) },
            Resources = new[] { "linh_thach", "tu_khi_dan", "truc_co_dan", "huyet_sac_lenh", "huyet_giao_hach" } },
        new RealmDef
        {
            Id = "kim_dan", Name = "Kim Đan",
            Desc = "Ngưng tụ kim đan nơi đan điền, thọ nguyên năm trăm năm. Mỗi kỳ cần linh thạch và Ngưng Kim Đan.",
            Stages = new[] { "Sơ kỳ", "Trung kỳ", "Hậu kỳ", "Đại viên mãn" },
            Aura = "#f2c94c", Lotus = "#f0d58a", Sash = "#9a6b1f", Hero = "img/hero_trucco.png",
            Cn = "", BagSlots = 160, EquipSlots = new[] { "weapon", "offhand", "head", "body", "hands", "legs", "feet", "necklace", "ring", "ring2" },
            QiReq = s => Math.Round(18000 * Math.Pow(1.7, s)),
            Rate = s => 140 + s * 50,
            StageCost = s => new() { ["linh_thach"] = 1500 * (s + 1), ["ngung_kim_dan"] = s + 1 },
            BreakCost = null, BreakChance = 1, // điền khi thêm Nguyên Anh: Kết Anh Đan + Kim Ô Tinh Hạch
            StatMult = 35,
            Monster = new MonsterDef("Lang vương", "img/quai_yeu_lang.png", Hp: 1200, Atk: 300, Def: 100),
            MonsterNames = new[] { "Yêu Lang Vương", "Độc Xà Vương", "Thạch Quy Vương", "Hỏa Hồ Vương", "Băng Lang Vương", "Thiết Trùng Vương", "Cốt Khuyển Vương" },
            Places = new[] { "Hỏa Diễm Sơn", "Táng Tiên Cốc", "Đông Hải bí cảnh" },
            Boss = new MonsterDef("Kim Ô Yêu Vương", "img/boss_kim_o.png", Hp: 5600, Atk: 860, Def: 260),
            BossLair = "Côn Ngô Sơn", KeyItem = "con_ngo_lenh", BossItem = "kim_o_tinh_hach",
            Drops = new[] { new DropDef("linh_thach", 200, 500, 1), new DropDef("tu_khi_dan", 3, 6, .6), new DropDef("truc_co_dan", 1, 2, .7), new DropDef("ngung_kim_dan", 1, 1, .03) },
            Resources = new[] { "linh_thach", "truc_co_dan", "ngung_kim_dan", "con_ngo_lenh", "kim_o_tinh_hach" } } };

    /// <summary>Cảnh giới hiển thị trên thanh tiến trình nhưng chưa mở.</summary>
    public static readonly string[] LockedRealms = { "Nguyên Anh", "Hóa Thần" };
    public static readonly string[] LockedRealmsCn = { "", "" };

    public static readonly Dictionary<string, int> StartingInventory = new() { ["linh_thach"] = 30, ["tu_khi_dan"] = 1 };

    /// <summary>Thời gian vắng mặt tối đa được cộng tu vi và thám hiểm bù.</summary>
    public const double MaxOfflineSeconds = 7200;

    /// <summary>Tỉ lệ giữ lại phần tốc độ tu luyện tăng thêm khi lên đại cảnh giới (0.8 = giảm 20%),
    /// tính từ mốc Luyện Khí: tốc độ thực = gốc + (Rate(0) − gốc) × QiGrowth.</summary>
    public const double QiGrowth = 0.8;

    /// <summary>Tốc độ tu luyện gốc (linh khí/giây) của cảnh giới r: chỉ tăng khi lên đại cảnh giới,
    /// lên tầng/kỳ nhỏ không đổi (tham số s giữ cho tương thích). Luyện Khí 3 · Trúc Cơ 15 · Kim Đan 112.6.</summary>
    public static double QiRate(RealmDef r, int s)
    {
        var start = Realms[0].Rate(0);
        return start + (r.Rate(0) - start) * QiGrowth;
    }

    /// <summary>Tỉ lệ giữ lại phần chỉ số tăng thêm khi lên cảnh giới (0.7 = giảm 30%): hệ số thực = 1 + (StatMult − 1) × StatGrowth.</summary>
    public const double StatGrowth = 0.7;

    /// <summary>Hệ số chỉ số thực của cảnh giới (Luyện Khí ×1 · Trúc Cơ ×4.5 · Kim Đan ×24.8).</summary>
    public static double StatScale(RealmDef r) => 1 + (r.StatMult - 1) * StatGrowth;

    /// <summary>Chỉ số gốc ở tầng/kỳ s, nhân với hệ số chỉ số của cảnh giới.</summary>
    public static CombatStats BaseStats(RealmDef r, int s)
    {
        var k = StatScale(r);
        return new((int)Math.Round((10 + 2 * s) * k), (int)Math.Round((100 + 15 * s) * k), (int)Math.Round((2 + s) * k));
    }

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
