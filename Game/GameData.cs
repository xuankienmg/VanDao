namespace VanDao.Game;

/// <summary>
/// Vật phẩm. Trang bị có Slot và chỉ số cộng thêm (đơn vị %):
/// Atk = tấn công, Hp = máu, Def = phòng thủ (giá trị cộng thẳng);
/// QiPct = tốc độ tu vi, SpeedPct = tốc độ thám hiểm (đơn vị %).
/// </summary>
public record ItemDef(string Id, string Name, string Type, string Color, string Desc,
    string? Icon = null, string? Slot = null, double QiPct = 0, double SpeedPct = 0,
    int Atk = 0, int Hp = 0, int Def = 0,
    int MinRealm = 0,          // cảnh giới tối thiểu để mặc
    string? Tint = null,       // bộ lọc màu CSS để phân biệt bậc trang bị dùng chung ảnh
    string Rarity = "common",  // common · uncommon · rare · epic · legendary
    string? Kind = null,       // loại hiển thị: Kiếm, Đao, Thương, Áo, Đan dược…
    double CritPct = 0,        // Bạo kích %
    double UseQiSeconds = 0,   // dùng được: cộng tu vi = tốc độ tu luyện × số giây này
    string? Special = null,    // dòng thuộc tính đặc biệt hiển thị trong tooltip
    int Tier = 0,              // bậc đan trong chuỗi hợp thành 9→1 (0 = không phải đan)
    string? Use = null,        // công dụng của đan / chìa khóa / nguyên liệu Boss
    int Sell = 0,              // giá bán (linh thạch); trang bị và đan dược bán được
    string? Set = null,        // bộ trang bị (hiện thành dòng màu riêng trong thẻ vật phẩm)
    int ItemLevel = 0,         // cấp trang bị (1 · 10 · 15) theo ải rơi; 0 = không phải trang bị
    string? Base = null,       // id món gốc trong GearBases (trang bị sinh theo cấp + phẩm)
    ItemEffect[]? Effects = null);   // hiệu ứng riêng theo cấp bậc (Linh → Thiên), xem GameData.EffectsFor

/// <summary>Hiệu ứng riêng của trang bị: Grade = cấp mở ra nó (1 Linh · 2 Huyền · 3 Địa · 4 Thiên);
/// Value cộng dồn giữa các món đang mặc (GameService.GearEffects), Value2 = thông số phụ (Thiên lôi: % công mỗi tia).</summary>
public record ItemEffect(string Id, string Name, int Grade, double Value, double Value2, string Text);

/// <summary>
/// Chỉ số chiến đấu. Speed (Thân pháp) tính bằng %
/// và chỉ có tác dụng khi cảnh giới đã mở chỉ số đó.
/// </summary>
public record CombatStats(int Atk, int Hp, int Def, double Speed = 0, double Crit = 0);

/// <summary>Chỉ số phụ mở theo cảnh giới.</summary>
public record StatDef(string Id, string Name, string Desc, int MinRealm);

/// <summary>Quái vật gặp khi thám hiểm ở một cảnh giới.</summary>
public record MonsterDef(string Name, string Image, int Hp, int Atk, int Def, string Tier = "");   // Tier: "" thường · "elite" tinh anh · "boss"

public record SlotDef(string Id, string Name);

public record DropDef(string Item, int Min, int Max, double Chance);

/// <summary>Hợp thành: 9 viên đan From + phí linh thạch → 1 viên đan To. Luôn thành công.</summary>
/// <summary>Trang phục (skin) nhân vật: bộ ảnh đứng / ngồi thiền / tung chiêu / sprite chiến đấu. Mở khi đạt cảnh giới MinRealm.</summary>
public record SkinDef(string Id, string Name, string Desc, int MinRealm, string Hero, string HeroStand, string HeroMed, string HeroCast);
public record SynthDef(string From, string To, int Count, int Stones, Dictionary<string, int>? Herbs = null);   // Herbs: linh thảo cần thêm mỗi lần hợp thành
/// <summary>Linh thảo rơi sau mỗi chuyến thắng ở các chương Chapters, từ ải MinArea (tỉ lệ tăng 3% mỗi ải như vật phẩm khác).</summary>
public record HerbDropDef(string Item, int[] Chapters, int MinArea, int Min, int Max, double Chance);
/// <summary>Đan phương: luyện 1 viên Out từ các nguyên liệu In (id → số lượng, gồm cả linh thạch).</summary>
public record BrewDef(string Out, Dictionary<string, int> In);

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
    /// <summary>Ảnh nét theo cảnh giới: đứng (Hành trang) · ngồi thiền (Đột phá) · bảng khung tung Hỏa Cầu (trận đấu cửa sổ lớn).
    /// Cắt lại: python tools/cut_heroes.py tools wwwroot/img</summary>
    public string? HeroStand { get; init; }
    public string? HeroMed { get; init; }
    public string? HeroCast { get; init; }
    /// <summary>Vòng sáng xoay sau lưng tu sĩ ở menu Đột phá (null = chưa có, Luyện Khí). Cắt: python tools/cut_halos.py tools/halos_design.png wwwroot/img</summary>
    public string? Halo { get; init; }

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

    /// <summary>Boss giữ nguyên liệu đột phá đại cảnh giới (rơi ở Boss chương cùng số: Luyện Khí → chương 1…); trận Boss nằm ở ChapterDef.</summary>
    public required MonsterDef Boss { get; init; }
    public required string BossItem { get; init; }
    /// <summary>Đồ rơi theo cảnh giới: dùng cho rương (trang bị trong rương) và làm gốc đồ rơi của chương cùng số.</summary>
    public required DropDef[] Drops { get; init; }
    public required string[] Resources { get; init; }
}

/// <summary>Một loài quái trên bản đồ chương: tên hiển thị (có tiền tố màu khi dùng lại) và ảnh (bản đổi màu: _v1 Huyết · _v2 U Minh · _v3 Kim · _v4 Ám).</summary>
public record ChapterMon(string Name, string Image);

/// <summary>
/// Chương bản đồ (1-1 → 10-15): 15 ải + ải Boss. Không gắn với cảnh giới — Realm chỉ là cảnh giới gợi ý.
/// Thắng Boss chương N mở chương N+1. Lệnh Boss dùng chung theo cặp chương (1–2 · 3–4 · 5–6).
/// </summary>
public class ChapterDef
{
    public const int CommonCount = 5;
    public required int Index { get; init; }
    /// <summary>Vùng của chương (tiêu đề "Chương N · Name").</summary>
    public required string Name { get; init; }
    /// <summary>Cảnh giới gợi ý (chỉ để hiển thị).</summary>
    public required string Realm { get; init; }
    /// <summary>false = chương tạm khóa (hiện ổ khóa trên thanh chương).</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>3 vùng nhỏ chia đều 15 ải.</summary>
    public required string[] Places { get; init; }
    /// <summary>5 loài quái thường (mỗi loài 3 ải) và 3 tinh anh (ải 10–11 · 12–13 · 14–15).</summary>
    public required ChapterMon[] Commons { get; init; }
    public required ChapterMon[] Elites { get; init; }
    /// <summary>Chỉ số gốc của quái thường chương này (nhân ×0.6 → ×2.6 theo ải).</summary>
    public required MonsterDef Monster { get; init; }
    public required MonsterDef Boss { get; init; }
    /// <summary>Hệ số máu riêng của Boss (đã tính cùng kỹ năng ẩn BossTrait).</summary>
    public double BossHp { get; init; } = 1;
    public required GameData.BossTrait Trait { get; init; }
    public required string BossLair { get; init; }
    public required string KeyItem { get; init; }
    /// <summary>Nguyên liệu đột phá rơi khi hạ Boss (chương 1–3); null = chỉ rơi Rương Boss.</summary>
    public string? BossItem { get; init; }
    public required DropDef[] Drops { get; init; }
    /// <summary>Tỉ lệ cấp bậc trang bị rơi (Phàm → Thiên); null = theo cảnh giới của món (GameData.DropQuality).</summary>
    public double[]? Quality { get; init; }
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
        new ItemDef("linh_thach",    "Linh thạch",       "stone", "#8fd0c9", "Tiền tệ chung của giới tu tiên: dùng để tu luyện, hợp thành đan và đột phá.", "img/stone/linh_thach.png", Rarity: "common", Kind: "Linh thạch"),
        // thượng phẩm linh thạch: chưa có nguồn nhận / công dụng — tạm chỉ hiện số dư (0) cạnh linh thạch
        new ItemDef("thuong_pham_linh_thach", "Thượng phẩm linh thạch", "stone", "#4fd6e6", "Linh thạch tinh thuần ngưng tụ từ linh mạch ngàn năm, linh khí cuộn xoáy bên trong.", "img/stone/thuong_pham_linh_thach.png",
            Rarity: "epic", Kind: "Linh thạch", Use: "Chưa mở: sẽ có trong bản cập nhật sau."),
        // vé Tài Xỉu: đổi bằng linh thạch ở Luyện chế → Tài Xỉu, mỗi vé quay 1 lần nhận 1 trang bị (GameService.Spin)
        new ItemDef("ve_tai_xiu", "Vé Tài Xỉu", "ticket", "#f2c94c", "Thẻ ngọc khắc vòng thập phẩm, truyền linh lực vào là vòng quay tự chuyển.", Rarity: "epic", Kind: "Vé quay",
            Use: "Dùng ở Luyện chế → Tài Xỉu: mỗi vé quay 1 lần, nhận 1 trang bị cảnh giới hiện tại, phẩm Phàm → Huyền Thiên."),
        // ---- rương: rơi khi diệt yêu, mở ra nhận 1 phần thưởng ngẫu nhiên theo cảnh giới (GameService.OpenChest) ----
        new ItemDef("ruong_thuong", "Rương thường", "chest", "#a8a39a", "Rương gỗ bọc sắt rơi khi diệt yêu.", Rarity: "common", Kind: "Rương",
            Use: "Mở ra nhận ngẫu nhiên trang bị (Phàm → Linh, cao hơn khi lên cảnh giới) hoặc đan dược của cảnh giới hiện tại · 10% ra pháp bảo."),
        new ItemDef("ruong_hiem",   "Rương hiếm",   "chest", "#5aa2e6", "Rương lam khảm ngọc, phù văn còn sáng. Chỉ rơi ở ải 10–15 mỗi chương.", Rarity: "rare", Kind: "Rương",
            Use: "Mở ra nhận ngẫu nhiên trang bị Linh trở lên hoặc nhiều đan dược của cảnh giới hiện tại · 20% ra pháp bảo."),
        new ItemDef("ruong_boss",   "Rương Boss",   "chest", "#f2c94c", "Rương vàng chạm long phượng, chắc chắn rơi khi hạ Boss.", Rarity: "legendary", Kind: "Rương",
            Use: "Mở ra nhận ngẫu nhiên trang bị Huyền / Địa (Kim Đan: có thể ra Thiên), hoặc nhiều đan dược của cảnh giới hiện tại · 40% ra pháp bảo."),
        // ---- chuỗi đan 9→1: đan chỉ dùng để lên tầng và đột phá ----
        new ItemDef("tu_khi_dan",    "Tụ Khí Đan",       "pill",  "#bfeee2", "Đan nhập môn, rơi từ yêu thú vùng Luyện Khí.", "img/dan/dan_01.png", Rarity: "common", Kind: "Đan dược", Tier: 1,
            Use: "Dùng để đột phá các tầng Luyện Khí. 9 viên hợp thành 1 Trúc Cơ Đan."),
        new ItemDef("truc_co_dan",   "Trúc Cơ Đan",      "pill",  "#6fd39a", "Đan xây nền đạo cơ.", "img/dan/dan_04.png", Rarity: "rare", Kind: "Đan dược", Tier: 2,
            Use: "Dùng để đột phá đại cảnh giới Luyện Khí → Trúc Cơ, và lên kỳ trong Trúc Cơ."),
        new ItemDef("ngung_kim_dan", "Ngưng Kim Đan",    "pill",  "#f2c94c", "Đan ngưng tụ kim đan nơi đan điền.", "img/dan/dan_08.png", Rarity: "epic", Kind: "Đan dược", Tier: 3,
            Use: "Dùng để đột phá đại cảnh giới Trúc Cơ → Kim Đan, và lên kỳ trong Kim Đan."),
        new ItemDef("ket_anh_dan",   "Kết Anh Đan",      "pill",  "#c58cff", "Đan phá đan thành anh, cực kỳ hiếm có.", "img/dan/dan_10.png", Rarity: "legendary", Kind: "Đan dược", Tier: 4,
            Use: "Dùng để đột phá Kim Đan → Nguyên Anh, và lên kỳ trong Nguyên Anh. 9 viên hợp thành 1 Hóa Thần Đan."),
        new ItemDef("hoa_than_dan",  "Hóa Thần Đan",     "pill",  "#f4f0ff", "Đan luyện nguyên anh thành nguyên thần, trăm năm khó gặp một viên.", "img/dan/dan_12.png",
            Rarity: "mythic", Kind: "Đan dược", Tier: 5,
            Use: "Dùng để đột phá Nguyên Anh → Hóa Thần, và lên kỳ trong Hóa Thần. 9 viên hợp thành 1 Luyện Hư Đan."),
        new ItemDef("luyen_hu_dan",  "Luyện Hư Đan",     "pill",  "#f2d68a", "Đan luyện thần hoàn hư, kim quang xoáy như dải ngân hà, ngàn năm khó thành một lò.", "img/dan/dan_13.png",
            Rarity: "mythic", Kind: "Đan dược", Tier: 6,
            Use: "Dùng để đột phá Hóa Thần → Luyện Hư, và lên kỳ trong Luyện Hư."),
        // ---- linh thảo: rơi khi diệt yêu theo chương (GameData.HerbDrops; nơi rơi hiện trong thẻ vật phẩm), dùng để luyện đan ----
        // ảnh img/herb/herb_01 → herb_25 (thấp → cao, 5 bậc × 5 loại); danh sách đầy đủ và các loại để dành: xem GameData.HerbArt
        new ItemDef("tay_tuy_thao", "Tẩy Tủy Thảo", "herb", "#3f8f7a", "Cỏ dại mọc khắp nơi linh khí tụ lại, lá xanh thẫm ánh sao, có thể gột rửa kinh mạch.", "img/herb/herb_25.png", Rarity: "uncommon", Kind: "Linh thảo",
            Use: "Linh thảo bậc 1 · luyện Tẩy Tủy Đan (5 cây)."),
        new ItemDef("tu_linh_thao", "Tụ Linh Thảo", "herb", "#5fbf6a", "Cỏ non hút linh khí quanh mình, mọc thành bụi ven rừng.", "img/herb/herb_01.png", Rarity: "common", Kind: "Linh thảo",
            Use: "Linh thảo bậc 1 · hợp thành Trúc Cơ Đan (2 cây mỗi viên)."),
        new ItemDef("huyet_can_thao", "Huyết Căn Thảo", "herb", "#b8343a", "Rễ đỏ như máu, quả mọng kết dưới gốc, chỉ mọc nơi yêu thú tinh anh trú ngụ.", "img/herb/herb_02.png", Rarity: "uncommon", Kind: "Linh thảo",
            Use: "Linh thảo bậc 1 · luyện Trúc Cơ Phá Chướng Đan (3 cây)."),
        new ItemDef("bich_ngoc_linh_thao", "Bích Ngọc Linh Thảo", "herb", "#4fd6a0", "Lá xanh trong như ngọc bích, thân vàng óng.", "img/herb/herb_06.png", Rarity: "uncommon", Kind: "Linh thảo",
            Use: "Linh thảo bậc 2 · hợp thành Ngưng Kim Đan (2 cây mỗi viên)."),
        new ItemDef("hoa_tam_hoa", "Hỏa Tâm Hoa", "herb", "#e0462a", "Hoa đỏ rực, nhụy cháy âm ỉ không bao giờ tắt.", "img/herb/herb_07.png", Rarity: "rare", Kind: "Linh thảo",
            Use: "Linh thảo bậc 2 · luyện Kim Đan Phá Chướng Đan (3 cây)."),
        new ItemDef("tu_van_sam", "Tử Vân Sâm", "herb", "#9a62d8", "Nhân sâm tím thành hình người, quanh thân vờn mây tím.", "img/herb/herb_11.png", Rarity: "rare", Kind: "Linh thảo",
            Use: "Linh thảo bậc 3 · hợp thành Kết Anh Đan (2 cây mỗi viên)."),
        new ItemDef("loi_mach_thao", "Lôi Mạch Thảo", "herb", "#4a6ae0", "Cỏ lam mọc nơi sét đánh, lôi quang chạy dọc gân lá.", "img/herb/herb_12.png", Rarity: "epic", Kind: "Linh thảo",
            Use: "Linh thảo bậc 3 · luyện Nguyên Anh Phá Chướng Đan (3 cây)."),
        new ItemDef("thien_nien_sam", "Thiên Niên Nhân Sâm", "herb", "#a8763a", "Sâm nghìn năm đã thành tinh, râu bạc rủ dài như lão nhân.", "img/herb/herb_16.png", Rarity: "epic", Kind: "Linh thảo",
            Use: "Linh thảo bậc 4 · hợp thành Hóa Thần Đan (2 cây mỗi viên)."),
        new ItemDef("cuu_diep_hon_lien", "Cửu Diệp Hồn Liên", "herb", "#7a8aff", "Sen chín cánh nở trên hồn hải, ánh lam u tịch.", "img/herb/herb_17.png", Rarity: "legendary", Kind: "Linh thảo",
            Use: "Linh thảo bậc 4 · luyện Hóa Thần Phá Chướng Đan (3 cây)."),
        new ItemDef("kien_moc_mieu", "Kiến Mộc Miêu", "herb", "#3fb86a", "Mầm Kiến Mộc, cây trụ nối trời đất; một lá non chứa sinh cơ vạn năm.", "img/herb/herb_22.png", Rarity: "legendary", Kind: "Linh thảo",
            Use: "Linh thảo bậc 5 · hợp thành Luyện Hư Đan (2 cây mỗi viên)."),
        new ItemDef("bat_tu_phuong_hoa", "Bất Tử Phượng Hoa", "herb", "#ff6a2a", "Hoa lửa nở từ tro tàn phượng hoàng, cháy mãi không tàn.", "img/herb/herb_21.png", Rarity: "mythic", Kind: "Linh thảo",
            Use: "Linh thảo bậc 5 · luyện Luyện Hư Phá Chướng Đan (3 cây)."),
        // ---- đan đặc biệt: luyện từ linh dược, dùng trong Hành trang ----
        new ItemDef("tay_tuy_dan", "Tẩy Tủy Đan", "elixir", "#3fd04a", "Đan gột rửa kinh mạch, xóa sạch dấu vết công pháp đã tu.", "img/dan/dan_07.png",
            Rarity: "epic", Kind: "Đan dược", Use: "Dùng: tẩy toàn bộ công pháp, hoàn lại mọi điểm kỹ năng để lĩnh ngộ lại (giữ linh căn bẩm sinh)."),
        new ItemDef("pha_chuong_dan_0", "Trúc Cơ Phá Chướng Đan", "elixir", "#a8302a", "Đan phá bình chướng Luyện Khí → Trúc Cơ.", "img/dan/dan_03.png",
            Rarity: "rare", Kind: "Đan dược", Use: "Dùng khi ở Luyện Khí: +10% tỉ lệ đột phá lên Trúc Cơ (tối đa +30%). Hiệu lực giữ đến khi đột phá thành công."),
        new ItemDef("pha_chuong_dan_1", "Kim Đan Phá Chướng Đan", "elixir", "#c9823a", "Đan phá bình chướng Trúc Cơ → Kim Đan.", "img/dan/dan_06.png",
            Rarity: "epic", Kind: "Đan dược", Use: "Dùng khi ở Trúc Cơ: +10% tỉ lệ đột phá lên Kim Đan (tối đa +30%). Hiệu lực giữ đến khi đột phá thành công."),
        new ItemDef("pha_chuong_dan_2", "Nguyên Anh Phá Chướng Đan", "elixir", "#8a4ad0", "Đan phá bình chướng Kim Đan → Nguyên Anh.", "img/dan/dan_09.png",
            Rarity: "legendary", Kind: "Đan dược", Use: "Dùng khi ở Kim Đan: +10% tỉ lệ đột phá lên Nguyên Anh (tối đa +30%). Hiệu lực giữ đến khi đột phá thành công."),
        new ItemDef("pha_chuong_dan_3", "Hóa Thần Phá Chướng Đan", "elixir", "#4a6ae0", "Đan phá bình chướng Nguyên Anh → Hóa Thần (vượt lôi kiếp).", "img/dan/dan_11.png",
            Rarity: "mythic", Kind: "Đan dược", Use: "Dùng khi ở Nguyên Anh: +10% tỉ lệ đột phá lên Hóa Thần (tối đa +30%). Hiệu lực giữ đến khi đột phá thành công."),
        new ItemDef("pha_chuong_dan_4", "Luyện Hư Phá Chướng Đan", "elixir", "#3fd0e0", "Đan phá bình chướng Hóa Thần → Luyện Hư, thần hồn hóa hư.", "img/dan/dan_05.png",
            Rarity: "mythic", Kind: "Đan dược", Use: "Dùng khi ở Hóa Thần: +10% tỉ lệ đột phá lên Luyện Hư (tối đa +30%). Hiệu lực giữ đến khi đột phá thành công."),
        // ---- chìa khóa vào ải Boss (rơi ở ải 1–15) ----
        new ItemDef("dong_hai_lenh",   "Đông Hải Long Lệnh", "key", "#4fa8ff", "Lệnh bài ngọc lam khắc long văn, mở cửa thủy cung nơi Thao Thiết ẩn mình ở Đông Hải bí cảnh.", "img/huyet_sac_lenh.png",
            Tint: "hue-rotate(200deg) saturate(1.4) brightness(1.1)", Rarity: "epic", Kind: "Chìa khóa",
            Use: "Cần để khiêu chiến Boss chương 9. Mỗi lần khiêu chiến tốn 1 tấm."),
        new ItemDef("hoa_nguc_lenh",   "Hỏa Ngục Lệnh",     "key", "#ff7a3a", "Lệnh bài nung trong địa hỏa, mở lối vào hang Boss ở Hỏa Diễm Sơn và Táng Tiên Cốc.", "img/huyet_sac_lenh.png",
            Tint: "hue-rotate(25deg) saturate(1.6) brightness(1.15)", Rarity: "epic", Kind: "Chìa khóa",
            Use: "Cần để khiêu chiến Boss chương 7–8. Mỗi lần khiêu chiến tốn 1 tấm."),
        new ItemDef("hu_thien_tan_do", "Hư Thiên Tàn Đồ",  "key", "#c9a86a", "Mảnh bản đồ cổ dẫn vào hang ổ hung thú ở Vạn Thú Sơn và Đoạn Hồn Nhai.", Rarity: "epic", Kind: "Chìa khóa",
            Use: "Cần để khiêu chiến Boss chương 5–6. Mỗi lần khiêu chiến tốn 1 tấm."),
        new ItemDef("huyet_sac_lenh",  "Huyết Sắc Lệnh Bài", "key", "#c0463a", "Lệnh bài đỏ thẫm mở cửa phong ấn Côn Ngô Sơn và Huyết Sắc Cấm Địa.", Rarity: "epic", Kind: "Chìa khóa",
            Use: "Cần để khiêu chiến Boss chương 3–4. Mỗi lần khiêu chiến tốn 1 tấm."),
        new ItemDef("con_ngo_lenh",    "Côn Ngô Sơn Lệnh",   "key", "#e6c46a", "Lệnh bài vàng khắc phù trận, mở lối vào hang Boss ở Hắc Phong Lâm và Thanh Vân Cốc.", Rarity: "epic", Kind: "Chìa khóa",
            Use: "Cần để khiêu chiến Boss chương 1–2. Mỗi lần khiêu chiến tốn 1 tấm."),
        // ---- nguyên liệu Boss: bắt buộc khi đột phá đại cảnh giới ----
        // ảnh img/core/core_01 → core_15 (thấp → cao). Đang dùng: 03 Ma Viên · 06 Hắc Giao · 08 Băng Phượng · 12 Cùng Kỳ.
        // Để dành cho Boss / cảnh giới sau: 01 đá xám · 02 ngọc lục · 04 hỏa hạch · 05 băng tinh · 07 lôi tinh · 09 kim hạch ·
        // 10 long hạch (rồng cuộn) · 11 tử tinh liên · 13 long văn bích ngọc · 14 đồng hạch xoáy · 15 hỗn độn hạch (tối thượng).
        new ItemDef("am_ma_ma_hach",     "Ám Ma Ma Hạch", "boss", "#8a6a3a", "Ma hạch của Ám Ma, ba xoáy ma khí cuộn mãi không tan trong lớp vỏ đồng đen.", "img/core/core_14.png", Rarity: "mythic", Kind: "Nguyên liệu Boss",
            Use: "Rơi từ Boss chương 7. Bắt buộc khi đột phá Hóa Thần Hậu kỳ → Luyện Hư."),
        new ItemDef("hac_lan_yeu_hach",  "Băng Phượng Tinh Hạch", "boss", "#b8c0f0", "Tinh hạch của Băng Phượng, hàn khí vạn năm ngưng lại quanh một đốm u quang.", "img/core/core_08.png", Rarity: "legendary", Kind: "Nguyên liệu Boss",
            Use: "Rơi từ Boss chương 3. Bắt buộc khi đột phá Kim Đan Hậu kỳ → Nguyên Anh."),
        new ItemDef("cung_ky_yeu_hach",  "Cùng Kỳ Yêu Hạch", "boss", "#a01830", "Yêu hạch của Cùng Kỳ, sát khí hung thú cuộn trong ánh huyết.", "img/core/core_12.png",
            Rarity: "legendary", Kind: "Nguyên liệu Boss",
            Use: "Rơi từ Boss chương 5. Bắt buộc khi đột phá Nguyên Anh Hậu kỳ → Hóa Thần."),
        new ItemDef("huyet_giao_hach",   "Hắc Giao Yêu Hạch", "boss", "#6a3ab0", "Yêu hạch của Hắc Giao, u quang lạnh lẽo chảy trong vảy đen.", "img/core/core_06.png", Rarity: "legendary", Kind: "Nguyên liệu Boss",
            Use: "Rơi từ Boss chương 2. Bắt buộc khi đột phá Trúc Cơ Hậu kỳ → Kim Đan."),
        new ItemDef("kim_o_tinh_hach",   "Ma Viên Yêu Hạch",   "boss", "#a8724a", "Yêu hạch của Tứ Tý Ma Viên, nặng như đá núi, ẩn sức bốn tay.", "img/core/core_03.png", Rarity: "legendary", Kind: "Nguyên liệu Boss",
            Use: "Rơi từ Boss chương 1. Bắt buộc khi đột phá Luyện Khí Tầng 9 → Trúc Cơ."),

    };

    // ---- món gốc của trang bị: chỉ số gốc ứng với phẩm ghi ở Rarity, cấp 1. Món thật trong game sinh ra ở GearVariants ----
    private static readonly ItemDef[] CoreGear =
    {

        // ---- bộ Thanh Vân (chỉ số gốc ở phẩm Lục): trang bị cảnh giới Luyện Khí, 10 món. Ảnh: python tools/cut_lk_set.py tools wwwroot/img ----
        new("lk_kiem",  "Kiếm Sắt Luyện Khí",  "equip", "#5fbf6a", "Kiếm sắt lưỡi sáng lam, chuôi đồng buộc tua ngọc, bội kiếm của đệ tử mới nhập môn.", "img/lk_kiem.png", "weapon", Atk: 13, CritPct: 5, Rarity: "uncommon", Kind: "Kiếm", Special: "Kiếm pháp: +5% Bạo kích", Set: LkSetName),
        new("lk_mu",    "Mũ Vải Thanh Vân",    "equip", "#5fbf6a", "Mũ vải lam khảm ngọc, dải lụa trắng buông tua, giữ đầu óc tỉnh táo khi tọa thiền.", "img/lk_mu.png", "head", Hp: 30, Def: 3, QiPct: 2, Rarity: "uncommon", Kind: "Mũ", Set: LkSetName),
        new("lk_ao",    "Trường Bào Luyện Khí", "equip", "#5fbf6a", "Trường bào trắng thêu vân lam, đai ngọc thắt eo, dệt lẫn sợi linh tơ.", "img/lk_ao.png", "body", Hp: 85, Def: 5, Rarity: "uncommon", Kind: "Đạo bào", Set: LkSetName),
        new("lk_gang",  "Hộ Thủ Vải",          "equip", "#5fbf6a", "Hộ thủ vải trắng quấn đai đồng khảm lam ngọc, ra quyền nhanh mà chắc.", "img/lk_gang.png", "hands", Atk: 4, Def: 3, Rarity: "uncommon", Kind: "Găng tay", Set: LkSetName),
        new("lk_quan",  "Trường Khố",          "equip", "#5fbf6a", "Quần vải trắng ống rộng, đai lam buộc gối, ngồi thiền lâu không tê.", "img/lk_quan.png", "legs", Hp: 40, Def: 3, Rarity: "uncommon", Kind: "Quần", Set: LkSetName),
        new("lk_giay",  "Giày Vân Hành",       "equip", "#5fbf6a", "Ủng vải trắng cổ cao viền đồng, đi đường núi nhẹ như mây trôi.", "img/lk_giay.png", "feet", Hp: 20, Def: 3, Rarity: "uncommon", Kind: "Giày", Set: LkSetName),
        new("lk_day",   "Ngọc Bội Sơ Cấp",     "equip", "#5fbf6a", "Ngọc bội lam chạm vân, treo tua ngọc, khẽ tụ linh khí quanh ngực.", "img/lk_day.png", "necklace", Hp: 30, QiPct: 4, Rarity: "uncommon", Kind: "Dây chuyền", Set: LkSetName),
        new("lk_nhan",  "Nhẫn Đồng",           "equip", "#5fbf6a", "Nhẫn đồng chạm hoa văn, lam ngọc ở giữa tụ linh khí quanh ngón tay.", "img/lk_nhan.png", "ring", QiPct: 7, Rarity: "uncommon", Kind: "Nhẫn", Set: LkSetName),
        new("lk_phu",   "Phù Gỗ",              "equip", "#5fbf6a", "Hộ phù gỗ đào khắc phù văn, treo tua lam, xua tà khí nhập thể.", "img/lk_phu.png", "charm", Hp: 15, Def: 2, Rarity: "uncommon", Kind: "Hộ phù", Set: LkSetName),
        new("lk_thach", "Thanh Vân Linh Thạch", "equip", "#5fbf6a", "Khối linh thạch lam trong suốt lơ lửng bên người, nhả linh khí thành kình lực.", "img/lk_thach.png", "offhand", Atk: 7, QiPct: 5, Rarity: "uncommon", Kind: "Pháp bảo", Set: LkSetName),
        // pháp bảo nhập môn: ô Pháp bảo mở từ Trúc Cơ
        new("pb_holo",  "Linh Hồ Lô",     "equip", "#5fbf6a", "Hồ lô gỗ khắc phù, nuốt linh khí rồi phun ra thành kình lực.", "img/pb_holo.png",  "offhand", Atk: 22, QiPct: 6, MinRealm: 1, Rarity: "uncommon", Kind: "Pháp bảo"),
        new("pb_guong", "Hộ Tâm Kính",    "equip", "#5fbf6a", "Gương đồng soi tâm, phản lại một phần yêu khí.",               "img/pb_guong.png", "offhand", Hp: 160, Def: 14, MinRealm: 1, Rarity: "uncommon", Kind: "Pháp bảo"),
        new("pb_kiem",  "Thanh Văn Kiếm", "equip", "#5fbf6a", "Đoản kiếm vân xanh, ngự khí bay quanh thân hộ chủ.",           "img/pb_kiem.png",  "offhand", Atk: 18, CritPct: 4, MinRealm: 1, Rarity: "uncommon", Kind: "Pháp bảo", Special: "Ngự kiếm: +4% Bạo kích"),

        // ---- bộ Huyền Băng (chỉ số gốc ở phẩm Lam): trang bị cảnh giới Trúc Cơ, rơi ở Huyết Sắc Cấm Địa ----
        new("tc_kiem",   "Huyền Văn Kiếm",   "equip", "#5aa2e6", "Trường kiếm lưỡi đen khắc phù văn lam, hàn khí quấn quanh thân kiếm.", "img/tc_kiem.png",   "weapon",   Atk: 70, CritPct: 6, MinRealm: 1, Rarity: "rare", Kind: "Kiếm", Special: "Kiếm pháp: +6% Bạo kích", Set: "Huyền Băng"),
        new("tc_thuong", "Huyền Băng Thương", "equip", "#5aa2e6", "Thương bạc mũi băng tinh, đâm ra kéo theo một dải hàn quang.",          "img/tc_thuong.png", "weapon",   Atk: 62, CritPct: 9, MinRealm: 1, Rarity: "rare", Kind: "Thương", Special: "Đâm xa: +9% Bạo kích", Set: "Huyền Băng"),
        new("tc_mu",     "Huyền Khôi",       "equip", "#5aa2e6", "Mũ giáp bạc gắn lam ngọc, giữ thần thức thanh minh.",                  "img/tc_mu.png",     "head",     Hp: 160, Def: 16, QiPct: 3, MinRealm: 1, Rarity: "rare", Kind: "Mũ", Set: "Huyền Băng"),
        new("tc_ao",     "Huyền Băng Giáp",  "equip", "#5aa2e6", "Giáp bạc khảm linh thạch lam, vai giáp chạm vân mây.",                 "img/tc_ao.png",     "body",     Hp: 460, Def: 27, MinRealm: 1, Rarity: "rare", Kind: "Giáp", Set: "Huyền Băng"),
        new("tc_gang",   "Huyền Thủ Giáp",   "equip", "#5aa2e6", "Găng giáp bạc nối khớp, mu tay khảm lam ngọc.",                        "img/tc_gang.png",   "hands",    Atk: 22, Def: 16, MinRealm: 1, Rarity: "rare", Kind: "Găng tay", Set: "Huyền Băng"),
        new("tc_quan",   "Huyền Giáp Khố",   "equip", "#5aa2e6", "Quần giáp vạt dài, đầu gối bọc bạc khảm ngọc bích.",                   "img/tc_quan.png",   "legs",     Hp: 220, Def: 14, MinRealm: 1, Rarity: "rare", Kind: "Quần", Set: "Huyền Băng"),
        new("tc_giay",   "Huyền Hài",        "equip", "#5aa2e6", "Ủng bạc cổ cao khảm ngọc lục, bước đi nhẹ như gió.",                   "img/tc_giay.png",   "feet",     Hp: 110, Def: 16, MinRealm: 1, Rarity: "rare", Kind: "Giày", Set: "Huyền Băng"),
        new("tc_day",    "Huyền Băng Trụy",  "equip", "#5aa2e6", "Mặt dây băng tinh khắc phù văn, tỏa hàn khí nhè nhẹ.",                 "img/tc_day.png",    "necklace", Hp: 160, QiPct: 6, MinRealm: 1, Rarity: "rare", Kind: "Dây chuyền", Set: "Huyền Băng"),
        new("tc_phu",    "Huyền Băng Phù",   "equip", "#5aa2e6", "Hộ phù băng tinh treo tua lam, hàn khí hộ thân, tà khí khó xâm.",           "img/lh_phu.png",    "charm",    Hp: 80, Def: 8, MinRealm: 1, Rarity: "rare", Kind: "Hộ phù", Set: "Huyền Băng", Tint: "hue-rotate(-20deg) saturate(.7) brightness(1.1)"),
        new("tc_nhan",   "Huyền Ngọc Giới",  "equip", "#5aa2e6", "Nhẫn bạc khắc phù văn, lam ngọc ở giữa tụ linh khí.",                  "img/tc_nhan.png",   "ring",     QiPct: 9, MinRealm: 1, Rarity: "rare", Kind: "Nhẫn", Set: "Huyền Băng"),
        new("tc_chau",   "Huyền Thiên Châu", "equip", "#5aa2e6", "Linh châu lam lơ lửng giữa hai vòng phù văn, nuốt nhả linh khí.",      "img/tc_chau.png",   "offhand",  Atk: 35, QiPct: 8, MinRealm: 1, Rarity: "rare", Kind: "Pháp bảo", Set: "Huyền Băng"),

        // ---- bộ Xích Kim (chỉ số gốc ở phẩm Lam): trang bị cảnh giới Kim Đan, dùng lại ảnh bộ Huyền Băng; rơi ở Kim Đan ----
        new("kd_kiem",   "Xích Kim Kiếm",     "equip", "#5aa2e6", "Trường kiếm tôi trong kim hỏa đan điền, lưỡi kiếm ngân vang như long ngâm.", "img/tc_kiem.png",   "weapon",   Atk: 350, CritPct: 8, MinRealm: 2, Rarity: "rare", Kind: "Kiếm", Special: "Kiếm pháp: +8% Bạo kích", Set: "Xích Kim"),
        new("kd_thuong", "Xích Kim Thương",   "equip", "#5aa2e6", "Thương dài mũi kim tinh, một đâm xuyên ba tầng hộ thể yêu khí.",            "img/tc_thuong.png", "weapon",   Atk: 310, CritPct: 11, MinRealm: 2, Rarity: "rare", Kind: "Thương", Special: "Đâm xa: +11% Bạo kích", Set: "Xích Kim"),
        new("kd_mu",     "Xích Kim Khôi",     "equip", "#5aa2e6", "Mũ giáp khắc trận Tụ Linh, giữ kim đan vững như bàn thạch.",                "img/tc_mu.png",     "head",     Hp: 800, Def: 80, QiPct: 4, MinRealm: 2, Rarity: "rare", Kind: "Mũ", Set: "Xích Kim"),
        new("kd_ao",     "Xích Kim Giáp",     "equip", "#5aa2e6", "Chiến giáp luyện từ kim tinh nghìn năm, đao kiếm khó lòng xuyên qua.",        "img/tc_ao.png",     "body",     Hp: 2300, Def: 135, MinRealm: 2, Rarity: "rare", Kind: "Giáp", Set: "Xích Kim"),
        new("kd_gang",   "Xích Kim Thủ Giáp", "equip", "#5aa2e6", "Găng giáp khảm kim tinh, một quyền đánh ra mang theo kim hỏa.",               "img/tc_gang.png",   "hands",    Atk: 110, Def: 80, MinRealm: 2, Rarity: "rare", Kind: "Găng tay", Set: "Xích Kim"),
        new("kd_quan",   "Xích Kim Giáp Khố", "equip", "#5aa2e6", "Quần giáp nặng, vạt khắc phù văn hộ thân.",                                   "img/tc_quan.png",   "legs",     Hp: 1100, Def: 70, MinRealm: 2, Rarity: "rare", Kind: "Quần", Set: "Xích Kim"),
        new("kd_giay",   "Xích Kim Hài",      "equip", "#5aa2e6", "Ủng kim tinh khắc trận Phi Hành, bước một bước vượt mười trượng.",            "img/tc_giay.png",   "feet",     Hp: 550, Def: 80, MinRealm: 2, Rarity: "rare", Kind: "Giày", Set: "Xích Kim"),
        new("kd_day",    "Xích Kim Trụy",     "equip", "#5aa2e6", "Mặt dây kim đan thu nhỏ, nhịp đập theo hơi thở chủ nhân.",                    "img/tc_day.png",    "necklace", Hp: 800, QiPct: 8, MinRealm: 2, Rarity: "rare", Kind: "Dây chuyền", Set: "Xích Kim"),
        new("kd_phu",    "Xích Kim Phù",      "equip", "#5aa2e6", "Hộ phù kim tinh khắc trận Hộ Đan, kim quang hộ thể khi gặp hiểm.",          "img/lh_phu.png",    "charm",    Hp: 400, Def: 40, MinRealm: 2, Rarity: "rare", Kind: "Hộ phù", Set: "Xích Kim", Tint: "sepia(.8) hue-rotate(-25deg) saturate(2.2)"),
        new("kd_nhan",   "Xích Kim Giới",     "equip", "#5aa2e6", "Nhẫn kim tinh nạm linh ngọc, tụ linh khí thành sương quanh ngón tay.",        "img/tc_nhan.png",   "ring",     QiPct: 11, MinRealm: 2, Rarity: "rare", Kind: "Nhẫn", Set: "Xích Kim"),
        new("kd_chau",   "Xích Kim Châu",     "equip", "#5aa2e6", "Linh châu kim hỏa xoay quanh chủ nhân, phun ra kim diễm thiêu đốt yêu tà.",   "img/tc_chau.png",   "offhand",  Atk: 175, QiPct: 10, MinRealm: 2, Rarity: "rare", Kind: "Pháp bảo", Set: "Xích Kim") };

    /// <summary>Chỉ số phụ mở theo cảnh giới (hiện mờ khi chưa mở).</summary>
    public static readonly IReadOnlyList<StatDef> ExtraStats = new[]
    {
        new StatDef("crit",   "Bạo kích",  "Tỉ lệ đánh chí mạng (×1.5 sát thương), tính theo kỳ vọng.", 0),
        new StatDef("speed",  "Thân pháp", "Đánh nhanh hơn: rút ngắn mỗi lượt giao chiến.", 1) };

    /// <summary>Thân pháp gốc theo cảnh giới (%).</summary>
    /// <summary>Bộ đồ cảnh giới mới sinh từ bộ Xích Kim (kd_*): chỉ số × k, dùng chung ảnh kèm bộ lọc màu Tint (chờ ảnh thiết kế riêng).</summary>
    private static IEnumerable<ItemDef> DeriveSet(string pre, string setName, int realm, double k, string tint, string desc) =>
        CoreGear.Where(b => b.Id.StartsWith("kd_")).Select(b => b with
        {
            Id = pre + b.Id[3..], Name = b.Name.Replace("Xích Kim", setName), Desc = desc, MinRealm = realm, Set = setName, Tint = tint,
            Atk = (int)Math.Round(b.Atk * k), Hp = (int)Math.Round(b.Hp * k), Def = (int)Math.Round(b.Def * k),
            QiPct = b.QiPct == 0 ? 0 : b.QiPct + 2 * (realm - 2), CritPct = b.CritPct == 0 ? 0 : b.CritPct + 2 * (realm - 2),
            Special = b.Special?.Replace(((int)b.CritPct).ToString(), ((int)(b.CritPct + 2 * (realm - 2))).ToString()) });
    /// <summary>Bộ Huyền Long (Luyện Hư): ảnh riêng (tools/cut_lh_set.py → img/lh_*.png), chỉ số từ bộ Xích Kim × 125 như các bộ suy ra khác.
    /// 10 món: thêm Hộ phù (đeo ô nhẫn) vì bản thiết kế có cả nhẫn lẫn hộ phù.</summary>
    public const string LhSetName = "Huyền Long", LhSetColor = "#fc246c";
    /// <summary>Bộ Thanh Vân (Luyện Khí, ảnh riêng img/lk_*.png): mọi phẩm dùng chung ảnh.</summary>
    public const string LkSetName = "Thanh Vân";
    private static IEnumerable<ItemDef> LhSet()
    {
        const int realm = 5; const double k = 125;
        ItemDef From(string kd, string id, string name, string kind, string desc, string? special = null)
        {
            var b = CoreGear.First(x => x.Id == kd);
            return b with
            {
                Id = id, Name = name, Kind = kind, Desc = desc, Icon = $"img/{id}.png", Tint = null, MinRealm = realm, Set = LhSetName, Color = LhSetColor,
                Atk = (int)Math.Round(b.Atk * k), Hp = (int)Math.Round(b.Hp * k), Def = (int)Math.Round(b.Def * k),
                QiPct = b.QiPct == 0 ? 0 : b.QiPct + 2 * (realm - 2), CritPct = b.CritPct == 0 ? 0 : b.CritPct + 2 * (realm - 2),
                Special = special };
        }
        yield return From("kd_kiem", "lh_truong", "Thiên Đạo Trượng", "Trượng", "Pháp trượng đầu rồng ngậm thiên châu, một vung là lôi quang hư không đổ xuống.", "Trượng pháp: +14% Bạo kích");
        yield return From("kd_mu",   "lh_mu",     "Thiên Huyền Quan", "Mũ",        "Ngọc quan khảm lam tinh, buộc tóc bạc, giữ nguyên thần thanh minh giữa hư không.");
        yield return From("kd_ao",   "lh_ao",     "Huyền Long Bào",   "Đạo bào",   "Đạo bào lam thẫm thêu kim long, vạt áo phiêu dật như mây cuốn.");
        yield return From("kd_gang", "lh_gang",   "Lôi Vân Thủ",      "Găng tay",  "Hộ thủ giáp lam viền vàng, lôi vân chạy dọc mu tay.");
        yield return From("kd_quan", "lh_quan",   "Thanh Hư Sam",     "Quần",      "Hạ sam nhiều lớp lam trắng, vạt thêu vân thanh hư.");
        yield return From("kd_giay", "lh_giay",   "Tinh Vân Hà",      "Giày",      "Ủng lam viền kim, bước đi như dạo trên dải ngân hà.");
        yield return From("kd_day",  "lh_day",    "Thanh Liên Ngọc",  "Dây chuyền", "Mặt ngọc thanh liên nối chuỗi lam tinh, tua ngọc lay động theo linh khí.");
        yield return From("kd_nhan", "lh_nhan",   "Hư Không Nhẫn",    "Nhẫn",      "Nhẫn bạc quấn kim long, lam châu ở giữa nuốt nhả hư không.");
        yield return From("kd_phu",  "lh_phu",    "Thiên Đạo Phù",    "Hộ phù",    "Hộ phù khắc thiên đạo phù văn, ngăn tà khí xâm thể.");
        yield return From("kd_chau", "lh_kinh",   "Huyền Long Kính",  "Pháp bảo",  "Bảo kính rồng vàng ôm lam nguyệt, soi đâu hư không vỡ đến đó.");
    }
    /// <summary>Mọi món gốc: bộ Thanh Vân (Luyện Khí) · Huyền Băng · Xích Kim (+ pháp bảo nhập môn) · Tử Vân (Nguyên Anh) · Thái Hư (Hóa Thần) · Huyền Long (Luyện Hư).</summary>
    public static readonly ItemDef[] GearBases = CoreGear
        .Concat(DeriveSet("tv_", "Tử Vân", 3, 5, "hue-rotate(70deg) saturate(1.25)", "Bảo khí luyện từ tử vân thạch, linh lực nguyên anh lưu chuyển không dứt."))
        .Concat(DeriveSet("th_", "Thái Hư", 4, 25, "saturate(.3) brightness(1.3)", "Bảo khí thái hư, nguyên thần nương vào mà hư thực khó lường."))
        .Concat(LhSet())
        .ToArray();

    public static readonly double[] BaseSpeed = { 0, 5, 10, 15, 20 };

    public static readonly IReadOnlyList<SlotDef> Slots = new[]
    {
        new SlotDef("head", "Nón"), new SlotDef("body", "Áo"), new SlotDef("hands", "Găng tay"),
        new SlotDef("legs", "Quần"), new SlotDef("feet", "Giày"),
        new SlotDef("necklace", "Dây chuyền"), new SlotDef("charm", "Hộ phù"), new SlotDef("ring", "Nhẫn"),
        new SlotDef("weapon", "Vũ khí"), new SlotDef("offhand", "Pháp bảo") };

    /// <summary>Vật phẩm có ảnh pixel 48×48 trong wwwroot/img/{id}.png.</summary>
    private static readonly HashSet<string> PixelIcons = new()
    {
        "ruong_thuong", "ruong_hiem", "ruong_boss", "ve_tai_xiu",
        "hu_thien_tan_do", "huyet_sac_lenh", "con_ngo_lenh" };   // món mới dùng Icon + Tint riêng

    /// <summary>Giá gốc theo độ hiếm; trang bị cảnh giới cao bán đắt hơn (×5 mỗi cảnh giới).</summary>
    public static readonly Dictionary<string, int> RarityPrice = new()
    {
        ["common"] = 3, ["uncommon"] = 12, ["rare"] = 50, ["epic"] = 200, ["legendary"] = 800, ["mythic"] = 3200,
        ["divine"] = 12800, ["immortal"] = 51200, ["dao"] = 204800, ["supreme"] = 819200 };
    /// <summary>10 phẩm (10/2026): Phàm · Linh · Huyền · Địa · Thiên · Thánh · Thần · Tiên · Đạo · Huyền Thiên. Khóa giữ tên cũ cho 6 phẩm đầu để không đổi dữ liệu đã lưu.</summary>
    public static readonly string[] RarityOrder = { "common", "uncommon", "rare", "epic", "legendary", "mythic", "divine", "immortal", "dao", "supreme" };
    /// <summary>Giá bán đan dược theo bậc (Tụ Khí → Hóa Thần): thấp hơn chi phí hợp thành 9→1 nên không lách được.</summary>
    public static readonly int[] PillSellPrice = { 2, 25, 300, 3000, 30000, 300000 };
    public static int SellPrice(ItemDef it) =>
        it.Tier > 0 ? PillSellPrice[Math.Clamp(it.Tier - 1, 0, PillSellPrice.Length - 1)]
        : it.Slot is null ? 0 : Math.Max(1, RarityPrice.GetValueOrDefault(it.Rarity, 3) * (1 + it.MinRealm * 4));

    // ---- trang bị theo CẤP (gắn với cảnh giới yêu cầu) và PHẨM màu ----
    /// <summary>Cấp trang bị theo cảnh giới yêu cầu (MinRealm): cấp 1 = Luyện Khí · cấp 10 = Trúc Cơ · cấp 15 = Kim Đan.
    /// Mỗi bộ đồ có đúng 1 cấp: Thanh Vân cấp 1 · Huyền Băng và pháp bảo nhập môn cấp 10 · Xích Kim cấp 15.</summary>
    public static readonly int[] GearLevels = { 1, 10, 15, 20, 25, 30 };
    public static int GearLevelOf(ItemDef b) => GearLevels[Math.Clamp(b.MinRealm, 0, GearLevels.Length - 1)];
    /// <summary>Cấp bậc trang bị: 0 Phàm · 1 Linh · 2 Huyền · 3 Địa · 4 Thiên (RarityOrder: common → legendary).
    /// Thần / Tiên cấp (khí linh) và hiệu ứng riêng của từng cấp (Hấp linh, Đại địa, Hoàng thiên…) chưa làm.</summary>
    public static readonly double[] QualityMult = { 1, 1.25, 1.55, 1.9, 2.4, 3.0, 3.8, 4.8, 6.0, 7.5 };   // 6 phẩm đầu giữ hệ số cũ; Thần → Huyền Thiên chỉ lên bằng Luyện khí
    public static readonly string[] QualityColor = { "#FFFFFF", "#00FF7F", "#00E5FF", "#1E90FF", "#BF00FF", "#FF8C00", "#FF0033", "#FF69B4", "#FFD700", "#E0FFFF" };
    public static readonly string[] QualityName = { "Phàm", "Linh", "Huyền", "Địa", "Thiên", "Thánh", "Thần", "Tiên", "Đạo", "Huyền Thiên" };
    /// <summary>Luyện khí: ghép FuseCount trang bị cùng phẩm q → 1 món phẩm q+1 với tỉ lệ FuseChance[q] (thất bại: 1 món phẩm q).</summary>
    public const int FuseCount = 9;
    public static readonly double[] FuseChance = { 0.90, 0.75, 0.60, 0.45, 0.30, 0.20, 0.15, 0.10, 0.05 };   // Phàm→Linh … Thiên→Thánh · Thánh→Thần · Thần→Tiên · Tiên→Đạo · Đạo→Huyền Thiên

    // ---- Tài Xỉu (Luyện chế): đổi SpinTicketPrice linh thạch lấy 1 vé; mỗi lượt quay ra 1 trang bị bộ cảnh giới hiện tại, phẩm theo SpinWeights ----
    public const string SpinTicket = "ve_tai_xiu";
    public const int SpinTicketPrice = 100_000, SpinMaxBatch = 10;
    /// <summary>Tỉ lệ (%) 10 ô vòng quay, Phàm → Huyền Thiên (tổng 100). Phẩm cao cực hiếm: Huyền Thiên ≈ 1/3.333 lượt.</summary>
    /// <summary>Code quà tặng (nhập ở menu Cài đặt, không phân biệt hoa thường): code → (vật phẩm, số lượng). Mỗi nhân vật dùng mỗi code 1 lần.</summary>
    public static readonly IReadOnlyDictionary<string, (string Item, int Qty)> GiftCodes = new Dictionary<string, (string, int)>
    {
        ["KIENDEPTRAI"] = (SpinTicket, 10),
    };
    public static readonly double[] SpinWeights = { 35, 25, 16, 10, 6, 3.5, 2, 1.2, .6, .03 };
    /// <summary>Gieo phẩm (chỉ số trong RarityOrder) theo trọng số w.</summary>
    public static int RollQuality(double[] w)
    {
        var x = Random.Shared.NextDouble() * w.Sum();
        for (var i = 0; i < w.Length; i++) if ((x -= w[i]) < 0) return i;
        return 0;
    }
    /// <summary>Món gốc vòng quay: bộ đồ của cảnh giới cao nhất đã có (≤ cảnh giới người chơi).</summary>
    public static ItemDef[] SpinPool(int realm)
    {
        var r = GearBases.Where(b => b.MinRealm <= realm).Select(b => b.MinRealm).DefaultIfEmpty(0).Max();
        return GearBases.Where(b => b.MinRealm == r).ToArray();
    }
    /// <summary>Tỉ lệ cấp (%) khi quái thường rơi trang bị, theo cảnh giới của món: Luyện Khí chủ yếu Phàm,
    /// Trúc Cơ mở Huyền, Kim Đan mở Địa. Thiên cấp chỉ có từ Rương Boss Kim Đan.</summary>
    public static readonly double[][] DropQuality =
    {
        new double[] { 85, 15, 0, 0, 0 },     // Luyện Khí
        new double[] { 50, 40, 10, 0, 0 },    // Trúc Cơ
        new double[] { 0, 50, 40, 10, 0 },    // Kim Đan
        new double[] { 0, 15, 50, 30, 5 },    // Nguyên Anh
        new double[] { 0, 0, 35, 50, 15 },    // Hóa Thần
        new double[] { 0, 0, 25, 50, 25 },    // Luyện Hư
    };
    public static double[] DropQualityOf(int realm) => DropQuality[Math.Clamp(realm, 0, DropQuality.Length - 1)];
    /// <summary>Tên hiển thị trong nhật ký: trang bị kèm cấp + phẩm, ví dụ "Huyền Văn Kiếm [cấp 10 · Huyền cấp]".</summary>
    public static string Label(ItemDef d) =>
        d.ItemLevel > 0 ? $"{d.Name} [cấp {d.ItemLevel} · {QualityName[Math.Clamp(Array.IndexOf(RarityOrder, d.Rarity), 0, QualityName.Length - 1)]}]" : d.Name;
    public static readonly IReadOnlyDictionary<string, ItemDef> GearBaseById = GearBases.ToDictionary(b => b.Id);
    public static string GearId(ItemDef b, int q) => $"{b.Id}_c{GearLevelOf(b)}q{q}";

    /// <summary>Sinh món theo phẩm: chỉ số gốc (ở phẩm Rarity của món gốc) quy về phẩm Trắng rồi nhân hệ số phẩm. Sức mạnh theo cấp đã nằm sẵn trong chỉ số gốc của từng bộ.</summary>
    /// <summary>Cân bằng 10/2026: trang bị đang gánh phần lớn sức mạnh (công/máu gấp 2–5 lần chỉ số gốc, thủ gấp 8–25 lần) → thu nhỏ chỉ số
    /// trang bị, quái các chương giảm theo (ChapterMonHp / ChapterMonAtk) để độ khó các ải giữ như cũ với đồ phẩm Huyền. Chạy lại: tools/sim_balance.</summary>
    public const double GearAtkHpMult = 0.42, GearDefMult = 0.55;
    private static ItemDef Variant(ItemDef b, int q)
    {
        var k = QualityMult[q] / QualityMult[Math.Max(0, Array.IndexOf(RarityOrder, b.Rarity))];
        int I(int v) => v == 0 ? 0 : Math.Max(1, (int)Math.Round(v * k * GearAtkHpMult));
        int Df(int v) => v == 0 ? 0 : Math.Max(1, (int)Math.Round(v * k * GearDefMult));
        double D(double v) => v == 0 ? 0 : Math.Round(v * k, 1);
        var crit = D(b.CritPct);
        var special = b.Special is null ? null : System.Text.RegularExpressions.Regex.Replace(b.Special, @"\+[\d.]+%", $"+{crit:0.#}%");
        return b with { Id = GearId(b, q), Rarity = RarityOrder[q], Color = QualityColor[q], Atk = I(b.Atk), Hp = I(b.Hp), Def = Df(b.Def),
            QiPct = D(b.QiPct), SpeedPct = D(b.SpeedPct), CritPct = crit, Special = special,
            ItemLevel = GearLevelOf(b), Base = b.Id,
            Icon = GradeIcon(b, q), Effects = EffectsFor(b, q) };
    }

    // ---- hiệu ứng riêng theo cấp bậc: món cấp cao giữ hiệu ứng của các cấp dưới ----
    /// <summary>Nhóm món: vũ khí (weapon, offhand) · trang sức (necklace, ring) · giáp (còn lại).</summary>
    public static string GearGroup(string? slot) => slot is "weapon" or "offhand" ? "weapon" : slot is "necklace" or "ring" or "charm" ? "jewel" : "armor";
    /// <summary>Hấp linh: % tốc độ tu luyện mỗi món theo cấp của món (Linh 2 · Huyền 3 · Địa 4 · Thiên 6).</summary>
    public static readonly double[] HapLinh = { 0, 2, 3, 4, 6, 8, 10, 12, 15, 20 };
    /// <summary>Thưởng bộ Thiên cấp (% công, thủ, máu) theo số món Thiên đang mặc; "đủ bộ" = mọi ô trang bị của cảnh giới.</summary>
    public const double ThienSet3 = 5, ThienSet5 = 10, ThienSetFull = 20;
    /// <summary>Hiệu ứng cộng thêm của đồ Đỏ theo số món Đỏ đang mặc (mốc 3 · 6 · 9): % tăng giá trị mọi hiệu ứng trang bị (trước khi áp trần).
    /// Đủ 9 món = kích hoạt toàn bộ. Đồ Đỏ cũng được tính vào thưởng bộ Vàng.</summary>
    public static readonly (int Count, double Pct)[] RedTiers = { (3, 10), (6, 20), (9, 30) };
    public static double RedBonus(int red) => RedTiers.Where(t => red >= t.Count).Select(t => t.Pct).DefaultIfEmpty(0).Max();
    private static ItemEffect? HuyenEffect(string? slot) => slot switch
    {
        "weapon"   => new("pha_giap",  "Phá giáp",  2, 6,   0, "Bỏ qua 6% phòng thủ quái"),
        "offhand"  => new("linh_ap",   "Linh áp",   2, 6,   0, "+6% sát thương kỹ năng"),
        "head"     => new("tinh_tam",  "Tĩnh tâm",  2, 25,  0, "Vào trận với 25 nộ"),
        "body"     => new("ho_the",    "Hộ thể",    2, 4,   0, "Giảm 4% sát thương nhận"),
        "hands"    => new("lien_kich", "Liên kích", 2, 6,   0, "6% cơ hội đánh thêm 1 đòn thường mỗi lượt"),
        "legs"     => new("vung_than", "Vững thân", 2, 4,   0, "+4% máu tối đa"),
        "feet"     => new("than_phap", "Thân pháp", 2, 4,   0, "4% né đòn quái"),
        "necklace" => new("hoi_khi",   "Hồi khí",   2, 1.5, 0, "Hồi 1.5% máu tối đa mỗi lượt"),
        "ring"     => new("tu_linh", "Tụ linh", 2, 8, 0, "+8% tu vi nhận khi thám hiểm"),
        "charm"    => new("ho_the",  "Hộ thể",  2, 4, 0, "Giảm 4% sát thương nhận"),
        _ => null };
    private static ItemEffect DiaEffect(string group) => group switch
    {
        "weapon" => new("dia_chan",  "Địa chấn",  3, 40, 0, "Đòn đầu trận thêm 40% công, bỏ qua phòng thủ"),
        "jewel"  => new("dia_mach",  "Địa mạch",  3, 6,  0, "Phản lại 6% sát thương nhận"),
        _        => new("tho_thuan", "Thổ thuẫn", 3, 4,  0, "Khiên đầu trận bằng 4% máu tối đa") };
    private static ItemEffect ThienEffect(string group) => group switch
    {
        "weapon" => new("thien_loi",  "Thiên lôi",  4, 15,   60, "Mỗi lượt 15% cơ hội giáng sét 60% công, bỏ qua phòng thủ"),
        "jewel"  => new("thien_menh", "Thiên mệnh", 4, 0.15, 0,  "Hệ số chí mạng +0.15 (×1.5 → ×1.65)"),
        _        => new("thien_uy",   "Thiên uy",   4, 5,    0,  "Giảm 5% công của quái") };
    /// <summary>Hiệu ứng của món gốc b ở cấp q: Linh → Hấp linh · Huyền → theo loại món · Địa / Thiên → theo nhóm món.</summary>
    public static ItemEffect[]? EffectsFor(ItemDef b, int q)
    {
        if (q < 1 || b.Slot is null) return null;
        var list = new List<ItemEffect> { new("hap_linh", "Hấp linh", 1, HapLinh[q], 0, $"+{HapLinh[q]:0.#}% tốc độ tu luyện") };
        if (q >= 2 && HuyenEffect(b.Slot) is { } h) list.Add(h);
        if (q >= 3) list.Add(DiaEffect(GearGroup(b.Slot)));
        if (q >= 4) list.Add(ThienEffect(GearGroup(b.Slot)));
        return list.ToArray();
    }
    /// <summary>Ảnh bộ Thiên cấp theo loại món (tools/cut_thien.py); món chưa có ảnh riêng dùng ảnh Địa cấp.</summary>
    private static readonly Dictionary<string, string> ThienIcons = new()
    {
        ["kiem"] = "img/thien_kiem.png", ["mu"] = "img/thien_mu.png", ["ao"] = "img/thien_ao.png", ["quan"] = "img/thien_quan.png",
        ["giay"] = "img/thien_giay.png", ["gang"] = "img/thien_gang.png", ["nhan"] = "img/thien_nhan.png", ["chau"] = "img/thien_chau.png" };
    /// <summary>Phàm → Huyền: ảnh gốc · Địa: ảnh hào quang tím (_epic) · Thiên: bộ ảnh Thiên cấp.</summary>
    private static string? GradeIcon(ItemDef b, int q)
    {
        if (b.Icon is null || q < 3 || b.Set == LhSetName || b.Set == LkSetName || b.Slot == "charm") return b.Icon;   // bộ / hộ phù có ảnh riêng: mọi phẩm dùng chung ảnh          // bộ có ảnh riêng: mọi phẩm dùng chung ảnh, màu phẩm hiện ở nền ô
        var epic = b.Icon.Replace(".png", "_epic.png");
        if (q == 3) return epic;
        var kind = b.Id[(b.Id.IndexOf('_') + 1)..];
        if (b.Slot == "offhand" && kind == "kiem") return "img/thien_phi_kiem.png";   // phi kiếm pháp bảo
        return ThienIcons.TryGetValue(kind, out var t) ? t : epic;
    }
    public static readonly ItemDef[] GearVariants = GearBases
        .SelectMany(b => Enumerable.Range(0, QualityMult.Length).Select(q => Variant(b, q))).ToArray();

    /// <summary>Gieo cấp bậc theo trọng số w (Phàm → Thiên) cho món gốc baseId; mặc định theo cảnh giới của món (DropQuality).</summary>
    public static string RollGear(string baseId, double[]? w = null)
    {
        w ??= DropQualityOf(GearBaseById[baseId].MinRealm);
        var x = Random.Shared.NextDouble() * w.Sum();
        var q = w.Length - 1;
        for (var i = 0; i < w.Length; i++) { if ((x -= w[i]) < 0) { q = i; break; } }
        return GearId(GearBaseById[baseId], q);
    }

    /// <summary>Trang bị bản cũ → món đúng cấp hiện hành: món gốc chưa có phẩm → giữ phẩm cũ; món có cấp sai (vd. lk_kiem_c10q2) → cùng phẩm, đúng cấp. null = không phải.</summary>
    public static string? UpgradeGear(string id)
    {
        if (GearBaseById.TryGetValue(id, out var b)) return GearId(b, Math.Max(0, Array.IndexOf(RarityOrder, b.Rarity)));
        var m = System.Text.RegularExpressions.Regex.Match(id, @"^(.+)_c\d+q(\d)$");
        if (m.Success && GearBaseById.TryGetValue(m.Groups[1].Value, out var b2)) { var to = GearId(b2, int.Parse(m.Groups[2].Value)); return to != id ? to : null; }
        return null;
    }

    public static readonly IReadOnlyList<ItemDef> AllItems = Items.Concat(GearVariants)
        .Select(it => PixelIcons.Contains(it.Id) ? it with { Icon = $"img/{it.Id}.png" } : it)
        .Select(it => it with { Sell = SellPrice(it) }).ToList();

    /// <summary>Chuỗi đan: 9 viên bậc dưới → 1 viên bậc trên.</summary>
    /// <summary>Ảnh đan (img/dan/dan_01 → dan_15, thấp → cao). Đang dùng: 01 Tụ Khí · 03 Phá Chướng (Trúc Cơ) · 04 Trúc Cơ ·
    /// 06 Phá Chướng (Kim Đan) · 07 Tẩy Tủy · 08 Ngưng Kim · 09 Phá Chướng (Nguyên Anh) · 10 Kết Anh · 11 Phá Chướng (Hóa Thần) · 12 Hóa Thần.
    /// Để dành khi mở cảnh giới cao: 13 Luyện Hư Đan (Immortal Ascension) · 14 Hợp Thể Đan (Nine-Revolution Divine) ·
    /// 15 Đại Thừa / Đạo đan tối thượng (Primordial Dao) · 02, 05: đan phụ (linh dược / hồi phục) sau này.</summary>
    public static readonly string[] PillChain = { "tu_khi_dan", "truc_co_dan", "ngung_kim_dan", "ket_anh_dan", "hoa_than_dan", "luyen_hu_dan" };
    public static readonly IReadOnlyList<SynthDef> Synths = new[]
    {
        new SynthDef("tu_khi_dan",    "truc_co_dan",   9, 50,    new() { ["tu_linh_thao"] = 2 }),
        new SynthDef("truc_co_dan",   "ngung_kim_dan", 9, 800,   new() { ["bich_ngoc_linh_thao"] = 2 }),
        new SynthDef("ngung_kim_dan", "ket_anh_dan",   9, 8000,  new() { ["tu_van_sam"] = 2 }),
        new SynthDef("ket_anh_dan",   "hoa_than_dan",  9, 80000, new() { ["thien_nien_sam"] = 2 }),
        new SynthDef("hoa_than_dan",  "luyen_hu_dan",  9, 800000, new() { ["kien_moc_mieu"] = 2 }) };

    /// <summary>Vật phẩm của bản cũ quy đổi khi nạp nhân vật: (id mới, hệ số). Không có id mới = đổi ra linh thạch.</summary>
    public static readonly Dictionary<string, (string To, int Factor)> Legacy = new()
    {
        ["lt_ha"] = ("linh_thach", 1), ["lt_trung"] = ("linh_thach", 50), ["lt_thuong"] = ("linh_thach", 1500),
        ["boi_nguyen_dan"] = ("truc_co_dan", 1), ["ngung_than_hoa"] = ("huyet_can_thao", 1), ["kim_tuy_dan"] = ("ngung_kim_dan", 1),
        ["hoi_khi_dan"] = ("linh_thach", 20), ["luyen_the_dan"] = ("linh_thach", 30), ["hoa_tinh"] = ("linh_thach", 20),
        ["linh_thao"] = ("linh_thach", 2), ["huyet_sam"] = ("linh_thach", 20), ["hoa_linh_chi"] = ("linh_thach", 300),
        ["yeu_dan_1"] = ("linh_thach", 10), ["yeu_dan_2"] = ("linh_thach", 100), ["yeu_dan_3"] = ("linh_thach", 1000),
        ["canh_kim"] = ("linh_thach", 150), ["lenh_bai"] = ("linh_thach", 50),
        // đã bỏ: bộ đồ vải thường (phẩm trắng) và Giới Phù → hoàn linh thạch bằng giá bán
        ["thiet_kiem"] = ("linh_thach", 3), ["non_vai"] = ("linh_thach", 3), ["ao_vai"] = ("linh_thach", 3), ["gang_vai"] = ("linh_thach", 3),
        ["quan_vai"] = ("linh_thach", 3), ["giay_vai"] = ("linh_thach", 3), ["lk_bua"] = ("linh_thach", 12),
        // đã bỏ: đồ lẻ không thuộc bộ (chỉ còn trang bị theo bộ của cảnh giới + pháp bảo)
        ["hac_dao"] = ("linh_thach", 12), ["gang_da"] = ("linh_thach", 12), ["nhan_ngoc"] = ("linh_thach", 12),
        ["truong_thuong"] = ("linh_thach", 12), ["ngoc_boi"] = ("linh_thach", 12) };

    /// <summary>Đột phá đại cảnh giới: phần tỉ lệ đại thành công trong thành công, phần tẩu hỏa nhập ma trong thất bại.</summary>
    public const double BreakCritShare = .2, BreakMajorShare = .25;
    /// <summary>Thất bại: tổn tu vi, thời gian điều tức trước lần thử sau (giây).</summary>
    public const double BreakFailLoss = .10, BreakMajorLoss = .25, BreakFailCooldown = 60, BreakMajorCooldown = 120;
    /// <summary>Tẩu hỏa nhập ma: suy yếu trong DebuffSeconds (tu vi ×0.7, công/thủ ×0.9).</summary>
    public const double DebuffSeconds = 300, DebuffRate = .7, DebuffStat = .9;
    /// <summary>Đại thành công: đạo cơ vững chắc, thêm vĩnh viễn 10% máu và phòng thủ gốc của cảnh giới mới.</summary>
    public const double BreakCritBonus = .10;

    /// <summary>Số ải thường mỗi cảnh giới; ải kế tiếp (21) là ải Boss.</summary>
    public const int Areas = 15;

    // ---- rương ----
    /// <summary>Tỉ lệ rơi mỗi trận thắng: rương thường ở mọi ải, rương hiếm chỉ từ ải RareChestMinArea.</summary>
    public const double ChestChance = 0.20, RareChestChance = 0.12;   // đã giảm 20% (từ 25% / 15%)
    public const int RareChestMinArea = 10;
    /// <summary>Hạ Boss: chắc chắn rơi 1 Rương Boss.</summary>
    public const string BossChest = "ruong_boss";

    /// <summary>
    /// Phần thưởng khi mở rương (1 món): Treasure = tỉ lệ ra pháp bảo (pháp bảo chỉ có từ rương);
    /// phần còn lại: GearShare ra trang bị, còn lại ra đan lên tầng PillMin–PillMax viên.
    /// Trang bị / pháp bảo: cấp bậc gieo theo Quality[cảnh giới] (Phàm → Thiên); cấp món theo cảnh giới yêu cầu của món.
    /// </summary>
    public record ChestDef(double Treasure, double GearShare, double[][] Quality, int PillMin, int PillMax)
    {
        public double[] QualityAt(int realm) => Quality[Math.Clamp(realm, 0, Quality.Length - 1)];
    }
    public static readonly IReadOnlyDictionary<string, ChestDef> Chests = new Dictionary<string, ChestDef>
    {
        ["ruong_thuong"] = new(0.10, 0.4, DropQuality, 1, 2),
        ["ruong_hiem"]   = new(0.20, 0.5, new[] {
            new double[] { 40, 50, 10, 0, 0 }, new double[] { 10, 50, 35, 5, 0 }, new double[] { 0, 25, 50, 25, 0 },
            new double[] { 0, 5, 45, 40, 10 }, new double[] { 0, 0, 25, 50, 25 } }, 2, 4),
        ["ruong_boss"]   = new(0.40, 0.6, new[] {                       // Boss: Luyện Khí Linh/Huyền · Trúc Cơ Huyền/Địa · Kim Đan Địa/Thiên
            new double[] { 0, 50, 50, 0, 0 }, new double[] { 0, 0, 70, 30, 0 }, new double[] { 0, 0, 30, 65, 5 },
            new double[] { 0, 0, 10, 60, 29, 1 }, new double[] { 0, 0, 0, 40, 58, 2 } }, 4, 7) };   // Nguyên Anh / Hóa Thần: có thể ra Đỏ
    public const int BossArea = Areas + 1;

    /// <summary>Boss mạnh thêm 100%: máu, công, thủ ×2 so với chỉ số gốc khai báo ở ChapterDef.Boss.</summary>
    public const double BossMult = 2;
    /// <summary>Hệ số máu chung cho mọi quái và Boss (1.875 = +87.5% so với gốc).</summary>
    public const double MonsterHpMult = 3.75;    // cân bằng 10/2026: gấp đôi (trước 1.875)

    /// <summary>Kỹ năng ẩn của Boss (không hiện trước trận): Yêu thân giảm % mọi sát thương nhận vào;
    /// Cuồng bạo: máu dưới EnrageAt% thì công tăng EnrageAtk%.</summary>
    public record BossTrait(double DmgReduce, double EnrageAt, double EnrageAtk);

    /// <summary>Mỗi chuyến ở ải thường đánh Waves đợt liên tiếp (máu tu sĩ mang qua đợt sau); từ ải EliteMinArea đợt cuối là quái tinh anh.</summary>
    public const int Waves = 3, EliteMinArea = 10;
    /// <summary>Chỉ số mỗi đợt so với quái ải (trước đây 1 con mỗi chuyến): quái thường máu ×0.4;
    /// tinh anh máu ×0.8 · công ×1.3 · thủ ×1.2 và giảm EliteDmgReduce% sát thương nhận vào.</summary>
    public const double WaveHp = 0.40, EliteHp = 0.80, EliteAtk = 1.3, EliteDef = 1.2, EliteDmgReduce = 5;
    /// <summary>Thưởng thêm khi thắng chuyến có tinh anh: linh thạch ×EliteStoneMult, rương hiếm EliteRareChestChance.</summary>
    public const double EliteStoneMult = 1.5, EliteRareChestChance = 0.20;
    /// <summary>Chương 1 ải LateBoostFrom–15 mạnh thêm: +LateBoostBase ở ải LateBoostFrom, mỗi ải thêm LateBoostStep (ải 15: +40%).</summary>
    public const int LateBoostFrom = 10;
    public const double LateBoostBase = 0.10, LateBoostStep = 0.06;
    public static double LateBoost(ChapterDef c, int s) => c.Index == 0 && s >= LateBoostFrom && s <= Areas ? 1 + LateBoostBase + LateBoostStep * (s - LateBoostFrom) : 1;

    /// <summary>Loài quái thường chính (0..4) của chương ở ải s: mỗi loài 3 ải liền nhau.</summary>
    public static int MonsterKind(int s) => Math.Min(ChapterDef.CommonCount - 1, (Math.Clamp(s, 1, Areas) - 1) * ChapterDef.CommonCount / Areas);

    /// <summary>Quái đầu tiên của ải (tên hiển thị / dự đoán cũ).</summary>
    public static MonsterDef AreaMonster(ChapterDef c, int s) => AreaWaves(c, s)[0];

    /// <summary>Các đợt quái của ải s trong chương c: ải Boss 1 đợt (Boss); ải thường 3 đợt — loài chính, loài chính (từ ải EliteMinArea: loài kế),
    /// rồi loài kế hoặc quái tinh anh (ải 10–11 · 12–13 · 14–15). Chỉ số theo ải: ×0.6 (ải 1) → ×2.6 (ải 15) so với quái gốc của chương.</summary>
    /// <summary>Hệ số quái theo chương (máu + thủ · công) sau khi thu nhỏ trang bị (GearAtkHpMult / GearDefMult), chốt bằng mô phỏng.</summary>
    public static readonly double[] ChapterMonHp  = { 0.62, 0.58, 0.58, 0.59, 0.63, 0.64, 0.56, 0.56, 0.66, 0.66 };   // chương 1 → 10 (chương 10 tạm theo chương 9)
    public static readonly double[] ChapterMonAtk = { 0.61, 0.71, 0.72, 0.63, 0.62, 0.63, 0.59, 0.64, 0.57, 0.57 };
    public static MonsterDef[] AreaWaves(ChapterDef c, int s)
    {
        var w = AreaWavesRaw(c, s); double kh = ChapterMonHp[Math.Clamp(c.Index, 0, 9)], ka = ChapterMonAtk[Math.Clamp(c.Index, 0, 9)];
        return kh == 1 && ka == 1 ? w : w.Select(m => m with { Hp = Math.Max(1, (int)Math.Round(m.Hp * kh)), Def = (int)Math.Round(m.Def * kh), Atk = Math.Max(1, (int)Math.Round(m.Atk * ka)) }).ToArray();
    }
    private static MonsterDef[] AreaWavesRaw(ChapterDef c, int s)
    {
        if (s >= BossArea) return new[] { c.Boss with { Hp = (int)Math.Round(c.Boss.Hp * MonsterHpMult * BossMult * c.BossHp),
            Atk = (int)Math.Round(c.Boss.Atk * BossMult), Def = (int)Math.Round(c.Boss.Def * BossMult), Tier = "boss" } };
        var m = (0.6 + 2.0 * (Math.Clamp(s, 1, Areas) - 1) / (Areas - 1)) * LateBoost(c, s);
        var b = c.Monster with { Hp = (int)Math.Round(c.Monster.Hp * m * MonsterHpMult), Atk = (int)Math.Round(c.Monster.Atk * m), Def = (int)Math.Round(c.Monster.Def * m) };
        MonsterDef Common(int k) => b with { Name = c.Commons[k].Name, Image = c.Commons[k].Image, Hp = Math.Max(1, (int)Math.Round(b.Hp * WaveHp)) };
        int k0 = MonsterKind(s), k1 = Math.Min(k0 + 1, ChapterDef.CommonCount - 1);
        if (s < EliteMinArea) return new[] { Common(k0), Common(k0), Common(k1) };
        var e = Math.Min(c.Elites.Length - 1, (s - EliteMinArea) / 2);
        var elite = b with { Name = c.Elites[e].Name, Image = c.Elites[e].Image, Hp = (int)Math.Round(b.Hp * EliteHp),
            Atk = (int)Math.Round(b.Atk * EliteAtk), Def = (int)Math.Round(b.Def * EliteDef), Tier = "elite" };
        return new[] { Common(k0), Common(k1), elite };
    }

    /// <summary>Tên vùng của ải s trong chương c.</summary>
    public static string AreaPlace(ChapterDef c, int s) =>
        s >= BossArea ? c.BossLair : c.Places[Math.Min(c.Places.Length - 1, (Math.Clamp(s, 1, Areas) - 1) * c.Places.Length / Areas)];

    /// <summary>Linh thạch rơi mỗi khi hạ 1 quái (kể cả chuyến thua): phần linh thạch của chuyến chia theo đợt —
    /// quái thường 1 phần, tinh anh StoneElite phần (chuyến có tinh anh = ×1.5 như trước), Boss StoneBoss phần (theo ải 15).</summary>
    public const double StoneElite = 2.5, StoneBoss = 10;
    public static (int Min, int Max) StoneRange(ChapterDef c, int area, string tier)
    {
        var d = c.Drops.FirstOrDefault(x => x.Item == "linh_thach");
        if (d is null) return (0, 0);
        var k = (1 + (Math.Clamp(area, 1, Areas) - 1) * 0.05) * (tier == "boss" ? StoneBoss : tier == "elite" ? StoneElite : 1) / Waves;
        return (Math.Max(1, (int)Math.Round(d.Min * k)), Math.Max(1, (int)Math.Round(d.Max * k)));
    }

    /// <summary>Tỉ lệ rơi lệnh Boss ở ải s.</summary>
    public static double KeyChance(int s) => 0.07;   // mọi ải như nhau

    public static readonly IReadOnlyDictionary<string, ItemDef> ItemById = AllItems.ToDictionary(i => i.Id);

    public static readonly IReadOnlyList<RealmDef> Realms = new[]
    {
        new RealmDef
        {
            Id = "luyen_khi", Name = "Luyện Khí",
            Desc = "Dẫn linh khí trời đất nhập thể, tẩy kinh phạt tủy. Mỗi tầng cần linh thạch và Tụ Khí Đan.",
            Stages = new[] { "Tầng 1", "Tầng 2", "Tầng 3", "Tầng 4", "Tầng 5", "Tầng 6", "Tầng 7", "Tầng 8", "Tầng 9" },
            Aura = "#7fd3c4", Lotus = "#d9a3b0", Sash = "#3f8378", Hero = "img/hero_luyenkhi.png",
            HeroStand = "img/hero_luyenkhi_stand.png", HeroMed = "img/med_luyenkhi.png", HeroCast = "img/hoa_cau_ref_luyenkhi.png",
            Cn = "", BagSlots = 60, EquipSlots = new[] { "weapon", "offhand", "head", "body", "hands", "legs", "feet", "necklace", "charm", "ring" },
            QiReq = s => Math.Round(119.6 * Math.Pow(1.38, s)),    // cả cảnh giới ≈ 5.400 tu vi = 30 phút ở tốc độ gốc 3/giây (không đồ, không thám hiểm)
            Rate = s => 3 + s * 0.6,
            StageCost = s => new() { ["linh_thach"] = 10 + 10 * s, ["tu_khi_dan"] = s < 3 ? 1 : 2 },
            BreakCost = new() { ["truc_co_dan"] = 1, ["linh_thach"] = 500, ["kim_o_tinh_hach"] = 1 }, BreakChance = .55,
            StatMult = 1,
            Boss = new MonsterDef("Tứ Tý Ma Viên", "img/boss_tu_ty_ma_vien.png", Hp: 140, Atk: 23, Def: 5),
            BossItem = "kim_o_tinh_hach",
            Drops = new[] { new DropDef("linh_thach", 3, 8, 1), new DropDef("tu_khi_dan", 1, 1, .4),
                // bộ Thanh Vân: món gốc, khi rơi gieo cấp bậc theo DropQuality
                new DropDef("lk_ao", 1, 1, .008), new DropDef("lk_mu", 1, 1, .008), new DropDef("lk_gang", 1, 1, .008), new DropDef("lk_giay", 1, 1, .008), new DropDef("lk_quan", 1, 1, .008),
                new DropDef("lk_kiem", 1, 1, .006), new DropDef("lk_nhan", 1, 1, .005),
                new DropDef("lk_day", 1, 1, .005), new DropDef("lk_phu", 1, 1, .005), new DropDef("lk_thach", 1, 1, .004) },
            Resources = new[] { "linh_thach", "tu_khi_dan", "con_ngo_lenh", "kim_o_tinh_hach" } },
        new RealmDef
        {
            Id = "truc_co", Name = "Trúc Cơ",
            Desc = "Linh khí hóa lỏng, xây nền đạo cơ. Mỗi kỳ cần linh thạch và Trúc Cơ Đan.",
            Stages = new[] { "Sơ kỳ", "Trung kỳ", "Hậu kỳ" },
            Aura = "#86b6ff", Lotus = "#9fd6c8", Sash = "#3b5f9a", Hero = "img/hero_trucco.png",
            HeroStand = "img/hero_trucco_stand.png", HeroMed = "img/med_trucco.png", HeroCast = "img/hoa_cau_ref_trucco.png", Halo = "img/halo_01.png",
            Cn = "", BagSlots = 100, EquipSlots = new[] { "weapon", "offhand", "head", "body", "hands", "legs", "feet", "necklace", "charm", "ring" },
            QiReq = s => Math.Round(20930 * Math.Pow(1.6, s)),     // cả cảnh giới ≈ 108.000 tu vi = 2 giờ ở tốc độ gốc 15/giây (không đồ, không thám hiểm)
            Rate = s => 18 + s * 6,
            StageCost = s => new() { ["linh_thach"] = 200 * (s + 1), ["truc_co_dan"] = s + 1 },
            BreakCost = new() { ["ngung_kim_dan"] = 1, ["linh_thach"] = 3000, ["huyet_giao_hach"] = 1 }, BreakChance = .4,
            StatMult = 6,
            Boss = new MonsterDef("Hắc Giao", "img/boss_hac_giao.png", Hp: 940, Atk: 172, Def: 52),
            BossItem = "huyet_giao_hach",
            Drops = new[] { new DropDef("linh_thach", 30, 80, 1), new DropDef("tu_khi_dan", 1, 3, .8), new DropDef("truc_co_dan", 1, 1, .05),
                                // bộ Huyền Băng: món gốc, khi rơi gieo phẩm. Pháp bảo không rơi từ quái: chỉ có khi mở rương
                new DropDef("tc_ao", 1, 1, .006), new DropDef("tc_mu", 1, 1, .006), new DropDef("tc_gang", 1, 1, .006), new DropDef("tc_quan", 1, 1, .006),
                new DropDef("tc_giay", 1, 1, .006), new DropDef("tc_kiem", 1, 1, .004), new DropDef("tc_thuong", 1, 1, .004),
                new DropDef("tc_day", 1, 1, .004), new DropDef("tc_nhan", 1, 1, .004), new DropDef("tc_phu", 1, 1, .004) },
            Resources = new[] { "linh_thach", "tu_khi_dan", "truc_co_dan", "huyet_sac_lenh", "huyet_giao_hach" } },
        new RealmDef
        {
            Id = "kim_dan", Name = "Kim Đan",
            Desc = "Ngưng tụ kim đan nơi đan điền, thọ nguyên năm trăm năm. Mỗi kỳ cần linh thạch và Ngưng Kim Đan.",
            Stages = new[] { "Sơ kỳ", "Trung kỳ", "Hậu kỳ" },
            Aura = "#f2c94c", Lotus = "#f0d58a", Sash = "#9a6b1f", Hero = "img/hero_kimdan.png",
            HeroStand = "img/hero_kimdan_stand.png", HeroMed = "img/med_kimdan.png", HeroCast = "img/hoa_cau_ref_kimdan.png", Halo = "img/halo_02.png",
            Cn = "", BagSlots = 160, EquipSlots = new[] { "weapon", "offhand", "head", "body", "hands", "legs", "feet", "necklace", "charm", "ring" },
            QiReq = s => Math.Round(580120 * Math.Pow(1.7, s)),    // cả cảnh giới ≈ 3,24 triệu tu vi = 8 giờ ở tốc độ gốc 112,6/giây
            Rate = s => 140 + s * 50,
            StageCost = s => new() { ["linh_thach"] = 1500 * (s + 1), ["ngung_kim_dan"] = s + 1 },
            BreakCost = new() { ["ket_anh_dan"] = 1, ["linh_thach"] = 30000, ["hac_lan_yeu_hach"] = 1 }, BreakChance = .30,
            StatMult = 35,
            Boss = new MonsterDef("Băng Phượng", "img/boss_bang_phuong.png", Hp: 5600, Atk: 860, Def: 260),
            BossItem = "hac_lan_yeu_hach",
            Drops = new[] { new DropDef("linh_thach", 200, 500, 1), new DropDef("tu_khi_dan", 3, 6, .6), new DropDef("truc_co_dan", 1, 2, .7), new DropDef("ngung_kim_dan", 1, 1, .03),
                // bộ Xích Kim: món gốc, khi rơi gieo phẩm. Pháp bảo chỉ có khi mở rương
                new DropDef("kd_ao", 1, 1, .006), new DropDef("kd_mu", 1, 1, .006), new DropDef("kd_gang", 1, 1, .006), new DropDef("kd_quan", 1, 1, .006),
                new DropDef("kd_giay", 1, 1, .006), new DropDef("kd_kiem", 1, 1, .004), new DropDef("kd_thuong", 1, 1, .004),
                new DropDef("kd_day", 1, 1, .004), new DropDef("kd_nhan", 1, 1, .004), new DropDef("kd_phu", 1, 1, .004) },
            Resources = new[] { "linh_thach", "truc_co_dan", "ngung_kim_dan", "ket_anh_dan", "huyet_sac_lenh", "hac_lan_yeu_hach" } },
        new RealmDef
        {
            Id = "nguyen_anh", Name = "Nguyên Anh",
            Desc = "Phá đan thành anh, nguyên anh xuất khiếu, thọ nguyên ngàn năm. Mỗi kỳ cần linh thạch và Kết Anh Đan.",
            Stages = new[] { "Sơ kỳ", "Trung kỳ", "Hậu kỳ" },
            Aura = "#c58cff", Lotus = "#e2c4ff", Sash = "#6a3f9a", Hero = "img/hero_kimdan.png",   // ảnh nhân vật tạm dùng Kim Đan (chờ ảnh thiết kế)
            HeroStand = "img/hero_kimdan_stand.png", HeroMed = "img/med_kimdan.png", HeroCast = "img/hoa_cau_ref_kimdan.png", Halo = "img/halo_03.png",
            Cn = "", BagSlots = 220, EquipSlots = new[] { "weapon", "offhand", "head", "body", "hands", "legs", "feet", "necklace", "charm", "ring" },
            QiReq = s => Math.Round(11130000 * Math.Pow(1.7, s)),  // cả cảnh giới ≈ 62 triệu tu vi = 24 giờ ở tốc độ gốc ~720/giây
            Rate = s => 900 + s * 300,
            StageCost = s => new() { ["linh_thach"] = 12000 * (s + 1), ["ket_anh_dan"] = s + 1 },
            BreakCost = new() { ["hoa_than_dan"] = 1, ["linh_thach"] = 200000, ["cung_ky_yeu_hach"] = 1 }, BreakChance = .25,
            StatMult = 180,
            Boss = new MonsterDef("Cùng Kỳ", "img/boss_cung_ky.png", Hp: 1, Atk: 1, Def: 1),
            BossItem = "cung_ky_yeu_hach",
            Drops = new[] { new DropDef("linh_thach", 1200, 3000, 1), new DropDef("truc_co_dan", 2, 4, .6), new DropDef("ngung_kim_dan", 1, 2, .5), new DropDef("ket_anh_dan", 1, 1, .03),
                new DropDef("hoa_than_dan", 1, 1, .002),
                // bộ Tử Vân: món gốc, khi rơi gieo phẩm. Pháp bảo chỉ có khi mở rương
                new DropDef("tv_ao", 1, 1, .006), new DropDef("tv_mu", 1, 1, .006), new DropDef("tv_gang", 1, 1, .006), new DropDef("tv_quan", 1, 1, .006),
                new DropDef("tv_giay", 1, 1, .006), new DropDef("tv_kiem", 1, 1, .004), new DropDef("tv_thuong", 1, 1, .004),
                new DropDef("tv_day", 1, 1, .004), new DropDef("tv_nhan", 1, 1, .004), new DropDef("tv_phu", 1, 1, .004) },
            Resources = new[] { "linh_thach", "ngung_kim_dan", "ket_anh_dan", "hoa_than_dan", "hu_thien_tan_do", "cung_ky_yeu_hach" } },
        new RealmDef
        {
            Id = "hoa_than", Name = "Hóa Thần",
            Desc = "Nguyên anh hóa nguyên thần, thần thức bao trùm trăm dặm, thọ nguyên hai ngàn năm. Mỗi kỳ cần linh thạch và Hóa Thần Đan.",
            Stages = new[] { "Sơ kỳ", "Trung kỳ", "Hậu kỳ" },
            Aura = "#e8eeff", Lotus = "#ffffff", Sash = "#8a9ab0", Hero = "img/hero_kimdan.png",
            HeroStand = "img/hero_kimdan_stand.png", HeroMed = "img/med_kimdan.png", HeroCast = "img/hoa_cau_ref_kimdan.png", Halo = "img/halo_04.png",
            Cn = "", BagSlots = 300, EquipSlots = new[] { "weapon", "offhand", "head", "body", "hands", "legs", "feet", "necklace", "charm", "ring" },
            QiReq = s => Math.Round(185500000 * Math.Pow(1.7, s)), // cả cảnh giới ≈ 1 tỷ tu vi = 72 giờ ở tốc độ gốc ~4.000/giây
            Rate = s => 5000 + s * 1500,
            StageCost = s => new() { ["linh_thach"] = 80000 * (s + 1), ["hoa_than_dan"] = s + 1 },
            BreakCost = new() { ["luyen_hu_dan"] = 1, ["linh_thach"] = 1500000, ["am_ma_ma_hach"] = 1 }, BreakChance = .20,
            StatMult = 900,
            Boss = new MonsterDef("Ám Ma", "img/boss_am_ma.png", Hp: 1, Atk: 1, Def: 1),
            BossItem = "am_ma_ma_hach",
            Drops = new[] { new DropDef("linh_thach", 6000, 15000, 1), new DropDef("ngung_kim_dan", 2, 3, .6), new DropDef("ket_anh_dan", 1, 1, .2), new DropDef("hoa_than_dan", 1, 1, .02),
                new DropDef("th_ao", 1, 1, .006), new DropDef("th_mu", 1, 1, .006), new DropDef("th_gang", 1, 1, .006), new DropDef("th_quan", 1, 1, .006),
                new DropDef("th_giay", 1, 1, .006), new DropDef("th_kiem", 1, 1, .004), new DropDef("th_thuong", 1, 1, .004),
                new DropDef("th_day", 1, 1, .004), new DropDef("th_nhan", 1, 1, .004), new DropDef("th_phu", 1, 1, .004) },
            Resources = new[] { "linh_thach", "ket_anh_dan", "hoa_than_dan", "hoa_nguc_lenh", "am_ma_ma_hach" } },
        new RealmDef
        {
            Id = "luyen_hu", Name = "Luyện Hư",
            Desc = "Luyện thần hoàn hư, nguyên thần hòa vào hư không, một ý niệm vượt ngàn dặm, thọ nguyên năm ngàn năm. Mỗi kỳ cần linh thạch và Luyện Hư Đan.",
            Stages = new[] { "Sơ kỳ", "Trung kỳ", "Hậu kỳ" },
            Aura = "#7fb8ff", Lotus = "#cfe4ff", Sash = "#2a4a8a", Hero = "img/hero_luyenhu.png",
            HeroStand = "img/hero_luyenhu_stand.png", HeroMed = "img/med_luyenhu.png", HeroCast = "img/hoa_cau_ref_luyenhu.png", Halo = "img/halo_05.png",
            Cn = "", BagSlots = 400, EquipSlots = new[] { "weapon", "offhand", "head", "body", "hands", "legs", "feet", "necklace", "charm", "ring" },
            QiReq = s => Math.Round(3100000000 * Math.Pow(1.7, s)),   // cả cảnh giới ≈ 16 tỷ tu vi (×16 Hóa Thần, cùng nhịp các cảnh giới trước)
            Rate = s => 28000 + s * 8000,
            StageCost = s => new() { ["linh_thach"] = 500000 * (s + 1), ["luyen_hu_dan"] = s + 1 },
            BreakCost = null, BreakChance = 1, // điền khi thêm Hợp Thể
            StatMult = 4500,
            Boss = new MonsterDef("Thao Thiết", "img/boss_thao_thiet.png", Hp: 1, Atk: 1, Def: 1),
            BossItem = "",
            Drops = new[] { new DropDef("linh_thach", 40000, 100000, 1), new DropDef("ket_anh_dan", 2, 3, .6), new DropDef("hoa_than_dan", 1, 1, .2), new DropDef("luyen_hu_dan", 1, 1, .02),
                // bộ Huyền Long (lh_*): món gốc, khi rơi gieo phẩm. Pháp bảo Huyền Long Kính chỉ có khi mở rương
                new DropDef("lh_ao", 1, 1, .006), new DropDef("lh_mu", 1, 1, .006), new DropDef("lh_gang", 1, 1, .006), new DropDef("lh_quan", 1, 1, .006),
                new DropDef("lh_giay", 1, 1, .006), new DropDef("lh_truong", 1, 1, .008),
                new DropDef("lh_day", 1, 1, .004), new DropDef("lh_nhan", 1, 1, .004), new DropDef("lh_phu", 1, 1, .004) },
            Resources = new[] { "linh_thach", "hoa_than_dan", "luyen_hu_dan", "dong_hai_lenh" } } };

    /// <summary>Vòng sáng dành sẵn cho cảnh giới chưa mở (điền vào RealmDef.Halo khi thêm cảnh giới): halo_03 … halo_09.</summary>
    public static readonly (string Realm, string Halo)[] HaloPlan =
    {
        ("Hợp Thể", "img/halo_06.png"),
        ("Đại Thừa", "img/halo_07.png"), ("Độ Kiếp", "img/halo_08.png"), ("Chân Tiên", "img/halo_09.png") };

    /// <summary>Cảnh giới hiển thị trên thanh tiến trình nhưng chưa mở.</summary>
    public static readonly string[] LockedRealms = { "Hợp Thể", "Đại Thừa" };

    /// <summary>Trang phục nhân vật. Mặc định (Skin rỗng) = tự đổi theo cảnh giới hiện tại như trước.</summary>
    public static readonly IReadOnlyList<SkinDef> Skins = new[]
    {
        new SkinDef("luyen_khi", "Thanh Sam Đệ Tử", "Áo vải xám lam của đệ tử ngoại môn, gọn gàng dễ hành tẩu.", 0,
            "img/hero_luyenkhi.png", "img/hero_luyenkhi_stand.png", "img/med_luyenkhi.png", "img/hoa_cau_ref_luyenkhi.png"),
        new SkinDef("truc_co", "Bích Vân Đạo Bào", "Đạo bào xanh thẫm thêu vân mây, nội môn đệ tử Trúc Cơ mới được khoác.", 1,
            "img/hero_trucco.png", "img/hero_trucco_stand.png", "img/med_trucco.png", "img/hoa_cau_ref_trucco.png"),
        new SkinDef("kim_dan", "Bạch Kim Tiên Y", "Tiên y trắng viền kim tuyến của chân nhân Kim Đan, tóc bạc phiêu dật.", 2,
            "img/hero_kimdan.png", "img/hero_kimdan_stand.png", "img/med_kimdan.png", "img/hoa_cau_ref_kimdan.png"),
        new SkinDef("luyen_hu", "Thương Long Hư Bào", "Đạo bào lam thẫm thêu kim long, tóc bạc buộc ngọc quan; lôi quang hư không vờn quanh thân.", 5,
            "img/hero_luyenhu.png", "img/hero_luyenhu_stand.png", "img/med_luyenhu.png", "img/hoa_cau_ref_luyenhu.png"),
    };
    public static readonly Dictionary<string, SkinDef> SkinById = Skins.ToDictionary(s => s.Id);

    // ================= Luyện đan theo đan phương (linh dược rơi khi diệt yêu) =================
    /// <summary>Nơi rơi linh thảo. Mỗi bậc: 1 loại để hợp thành đan đột phá (rơi mọi ải, chương kế tiếp rơi ít hơn)
    /// + 1 loại cho Phá Chướng Đan (chỉ ải 10–15, nơi có tinh anh). Tẩy Tủy Thảo rơi ít ở mọi chương.</summary>
    public static readonly HerbDropDef[] HerbDrops =
    {
        new("tu_linh_thao",        new[] { 0 },    1,  1, 2, .25), new("tu_linh_thao",        new[] { 1 },    1, 1, 1, .12),
        new("huyet_can_thao",      new[] { 0 },    10, 1, 1, .08),
        new("bich_ngoc_linh_thao", new[] { 1 },    1,  1, 2, .25), new("bich_ngoc_linh_thao", new[] { 2, 3 }, 1, 1, 1, .12),
        new("hoa_tam_hoa",         new[] { 1 },    10, 1, 1, .08),
        new("tu_van_sam",          new[] { 2, 3 }, 1,  1, 2, .25), new("tu_van_sam",          new[] { 4, 5 }, 1, 1, 1, .12),
        new("loi_mach_thao",       new[] { 2, 3 }, 10, 1, 1, .08),
        new("thien_nien_sam",      new[] { 4, 5 }, 1,  1, 2, .25), new("thien_nien_sam",      new[] { 6, 7 }, 1, 1, 1, .12),
        new("cuu_diep_hon_lien",   new[] { 4, 5 }, 10, 1, 1, .08),
        new("kien_moc_mieu",       new[] { 6, 7 }, 1,  1, 2, .25), new("kien_moc_mieu",       new[] { 8 },    1, 1, 1, .12),
        new("bat_tu_phuong_hoa",   new[] { 6, 7 }, 10, 1, 1, .08),
        new("tay_tuy_thao",        new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, 1, 1, 1, .03),
    };
    /// <summary>Linh thảo rơi ở chương ch, ải area (ải Boss không rơi linh thảo).</summary>
    public static IEnumerable<DropDef> HerbDropsAt(int ch, int area) =>
        HerbDrops.Where(h => h.Chapters.Contains(ch) && area >= h.MinArea).Select(h => new DropDef(h.Item, h.Min, h.Max, h.Chance));
    /// <summary>Ảnh linh thảo img/herb/herb_NN.png (thấp → cao). Đang dùng: 01 Tụ Linh Thảo · 02 Huyết Căn Thảo · 06 Bích Ngọc Linh Thảo ·
    /// 07 Hỏa Tâm Hoa · 11 Tử Vân Sâm · 12 Lôi Mạch Thảo · 16 Thiên Niên Nhân Sâm · 17 Cửu Diệp Hồn Liên · 25 Tẩy Tủy Thảo (Void Star Grass, bậc thấp).
    /// Để dành: bậc 1 — 03 Nguyệt Lộ Hoa · 04 Bách Diệp Thảo · 05 Linh Thảo Chủng; bậc 2 — 08 Băng Tinh Thảo · 09 Thủy Lộ Diệp · 10 Thạch Đài Thảo;
    /// bậc 3 — 13 Kim Dương Liên · 14 Phong Vũ Thảo · 15 Linh Cốt Chi; bậc 4 — 18 Địa Mạch Tiên Đằng · 19 Thiên Trúc Duẩn · 20 Phượng Vũ Chi;
    /// bậc 5 (cảnh giới cao) — 21 Bất Tử Phượng Hoa · 22 Kiến Mộc Miêu · 23 Hỗn Nguyên Đạo Liên · 24 Âm Dương Bàn Đào.</summary>
    public const string HerbArt = "img/herb";
    /// <summary>Phá Chướng Đan: +10% tỉ lệ đột phá đại cảnh giới mỗi viên, tối đa +30%; mất hiệu lực khi đột phá thành công.</summary>
    public const double BreakPillStep = .10, BreakPillMax = .30;
    /// <summary>Linh thảo luyện Phá Chướng Đan theo cảnh giới (bậc 1 → 4).</summary>
    public static readonly string[] BreakHerbs = { "huyet_can_thao", "hoa_tam_hoa", "loi_mach_thao", "cuu_diep_hon_lien", "bat_tu_phuong_hoa" };
    /// <summary>Phá Chướng Đan của cảnh giới r (dùng khi đang ở Realms[r]).</summary>
    public static string BreakPillId(int r) => $"pha_chuong_dan_{r}";
    public static int BreakPillRealm(string id) =>
        id.StartsWith("pha_chuong_dan_") && int.TryParse(id["pha_chuong_dan_".Length..], out var r) ? r : -1;
    /// <summary>Đan phương. Phá Chướng Đan cảnh giới r: 3 viên đan đột phá của r + 3 linh thảo bậc r + 1 (BreakHerbs) + linh thạch bằng phí đột phá.</summary>
    public static readonly IReadOnlyList<BrewDef> Brews = new[] { new BrewDef("tay_tuy_dan", new() { ["tay_tuy_thao"] = 5, ["linh_thach"] = 1000 }) }
        .Concat(Enumerable.Range(0, BreakHerbs.Length).Select(r => new BrewDef(BreakPillId(r), new()
        {
            [PillChain[r + 1]] = 3, [BreakHerbs[r]] = 3, ["linh_thach"] = Realms[r].BreakCost?.GetValueOrDefault("linh_thach") ?? 0,
        }))).ToList();
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
    /// <summary>Ải thường 3 đợt: mỗi đợt tối đa MaxWaveHits lượt hoạt ảnh, giữa hai đợt nghỉ WaveSeconds giây (quái mới bước vào).</summary>
    public const int MaxWaveHits = 5;
    public const double WaveSeconds = 0.8;

    /// <summary>Mỗi chuyến thám hiểm cho thêm tu vi bằng tốc độ tu luyện × số giây này.</summary>
    public const double ExploreQiSeconds = 6;   // chuyến 3 đợt dài hơn ~1.5 lần (trước 4)

    /// <summary>Thời gian thám hiểm tối thiểu dù trang bị tăng tốc nhiều.</summary>
    public const double MinExploreSeconds = 3;

    /// <summary>Hệ số tu vi mỗi lần bấm Vận công (nhân với tốc độ tu luyện).</summary>
    public const double ChannelMultiplier = 1.2;

    // ================= Bản đồ 10 chương =================
    private static ChapterMon Q(string name, string id, int v = 0, string pre = "") =>
        new(pre + name, $"img/{id}{(v > 0 ? $"_v{v}" : "")}.png");
    private static ChapterMon Huyet(string name, string id) => Q(name, id, 1, "Huyết ");
    private static ChapterMon UMinh(string name, string id) => Q(name, id, 2, "U Minh ");
    private static ChapterMon Kim(string name, string id) => Q(name, id, 3, "Kim ");

    /// <summary>Hệ số chỉ số chương 4–6 so với chương 3 (quái và Boss); mô phỏng 10/2026 (Kim Đan Hậu kỳ, max Hỏa hệ): chương 4 Boss cần đồ Thiên cấp; chương 5 Thiên cấp qua hết ải nhưng chưa hạ được Boss; chương 6 dành cho Nguyên Anh.</summary>
    public static readonly double[] ChapterMult = { 2.0, 6, 10, 30, 50, 150 };   // chương 4 · 5 · 6 · 7 · 8 (chốt bằng sim_ch)
    private static MonsterDef Mul(MonsterDef m, double k, string? name = null, string? img = null) =>
        m with { Name = name ?? m.Name, Image = img ?? m.Image, Hp = (int)Math.Round(m.Hp * k), Atk = (int)Math.Round(m.Atk * k), Def = (int)Math.Round(m.Def * k) };
    /// <summary>Đồ rơi chương sau dùng gốc của chương cùng cảnh giới: linh thạch ×stone, đan ×pill tỉ lệ.</summary>
    /// <summary>Hệ số tỉ lệ rơi đan dược khi diệt yêu (0.5 = giảm 50%, 10/2026). Không áp cho đan từ rương.</summary>
    public const double PillDropMult = 0.5;
    /// <summary>Tỉ lệ rơi thực của một dòng bảng rơi (trước khi cộng 3% mỗi ải): đan dược nhân PillDropMult.</summary>
    public static double DropChance(DropDef d) =>
        ItemById.TryGetValue(d.Item, out var it) && it.Type == "pill" ? d.Chance * PillDropMult : d.Chance;
    private static DropDef[] ScaleDrops(DropDef[] src, double stone, double pill) => src.Select(d =>
        d.Item == "linh_thach" ? d with { Min = (int)Math.Round(d.Min * stone), Max = (int)Math.Round(d.Max * stone) }
        : ItemById.TryGetValue(d.Item, out var it) && it.Type == "pill" ? d with { Chance = Math.Min(1, d.Chance * pill) } : d).ToArray();

    private static readonly MonsterDef Ch3Monster = new("Yêu thú Côn Ngô", "img/quai_reu_thach_nhan.png", Hp: 1200, Atk: 300, Def: 100);
    private static readonly MonsterDef Ch3Boss = new("Băng Phượng", "img/boss_bang_phuong.png", Hp: 5600, Atk: 860, Def: 260);

    private static readonly ChapterMon[] Chapters0Commons = Enumerable.Repeat(new ChapterMon("?", "img/quai_yeu_lang.png"), ChapterDef.CommonCount).ToArray();
    private static readonly ChapterMon[] Chapters0Elites = Enumerable.Repeat(new ChapterMon("?", "img/tinh_thiet_giap_ho.png"), 3).ToArray();

    public static readonly IReadOnlyList<ChapterDef> Chapters = new[]
    {
        new ChapterDef
        {
            Index = 0, Name = "Hắc Phong Lâm", Realm = "Luyện Khí",
            Places = new[] { "Hắc Phong Lâm", "Hậu sơn tông môn", "Ma Viên Lĩnh" },
            Commons = new[] { Q("Yêu Lang", "quai_yeu_lang"), Q("Độc Xà", "quai_doc_xa"), Q("Thạch Trư", "quai_thach_tru"), Q("Băng Hồ", "quai_bang_ho"), Q("Phá Sơn Viên", "quai_pha_son_vien") },
            Elites = new[] { Q("Thiết Giáp Hổ", "tinh_thiet_giap_ho"), Q("Cốt Tướng", "tinh_cot_tuong"), Q("Hắc Mãng", "tinh_hac_mang") },
            Monster = new MonsterDef("Yêu Lang", "img/quai_yeu_lang.png", Hp: 30, Atk: 8, Def: 2),
            Boss = new MonsterDef("Tứ Tý Ma Viên", "img/boss_tu_ty_ma_vien.png", Hp: 140, Atk: 23, Def: 5),
            BossHp = 0.75, Trait = new(5, 50, 0),
            BossLair = "Ma Viên Động", KeyItem = "con_ngo_lenh", BossItem = "kim_o_tinh_hach",
            Drops = Realms[0].Drops },
        new ChapterDef
        {
            Index = 1, Name = "Thanh Vân Cốc", Realm = "Trúc Cơ",
            Places = new[] { "Thanh Vân Cốc", "Vân Hải Kiều", "Lôi Minh Phong" },
            Commons = new[] { Q("Tinh Thạch Tích", "quai_tinh_thach_tich"), Q("Huyền Ô", "quai_huyen_o"), Q("Ma Đằng", "quai_ma_dang"), Q("Cốt Khuyển", "quai_cot_khuyen"), Q("Lôi Ưng", "quai_loi_ung") },
            Elites = new[] { Q("Huyết Y Kiếm Khách", "tinh_huyet_y_kiem_khach"), Q("Cốt Long", "tinh_cot_long"), Q("Lôi Kỳ Lân", "tinh_loi_ky_lan") },
            Monster = new MonsterDef("Tinh Thạch Tích", "img/quai_tinh_thach_tich.png", Hp: 200, Atk: 60, Def: 20),
            Boss = new MonsterDef("Hắc Giao", "img/boss_hac_giao.png", Hp: 940, Atk: 172, Def: 52),
            BossHp = 0.5, Trait = new(5, 50, 5),
            BossLair = "Hắc Giao Đàm", KeyItem = "con_ngo_lenh", BossItem = "huyet_giao_hach",
            Drops = Realms[1].Drops },
        new ChapterDef
        {
            Index = 2, Name = "Côn Ngô Sơn", Realm = "Kim Đan",
            Places = new[] { "Côn Ngô Sơn", "Thạch Lâm", "Côn Ngô Đỉnh" },
            Commons = new[] { Q("Rêu Thạch Nhân", "quai_reu_thach_nhan"), Q("Độc Chu Nữ", "quai_doc_chu_nu"), Q("Cửu Vĩ Hồ", "quai_cuu_vi_ho"), Q("Thiết Giáp Binh", "quai_thiet_giap_binh"), Huyet("Yêu Lang", "quai_yeu_lang") },
            Elites = new[] { Q("Kiếm Hồn", "tinh_kiem_hon"), Huyet("Thiết Giáp Hổ", "tinh_thiet_giap_ho"), Huyet("Cốt Tướng", "tinh_cot_tuong") },
            Monster = Ch3Monster, Boss = Ch3Boss,
            BossHp = 1.1, Trait = new(10, 50, 10),
            BossLair = "Băng Phượng Đài", KeyItem = "huyet_sac_lenh", BossItem = "hac_lan_yeu_hach",
            Drops = Realms[2].Drops },
        new ChapterDef
        {
            Index = 3, Name = "Huyết Sắc Cấm Địa", Realm = "Kim Đan",
            Places = new[] { "Huyết Sắc Cấm Địa", "Huyết Hà", "Cấm Địa Trung Tâm" },
            Commons = new[] { Huyet("Độc Xà", "quai_doc_xa"), Huyet("Thạch Trư", "quai_thach_tru"), Huyet("Băng Hồ", "quai_bang_ho"), Huyet("Sơn Viên", "quai_pha_son_vien"), Huyet("Thạch Tích", "quai_tinh_thach_tich") },
            Elites = new[] { Huyet("Hắc Mãng", "tinh_hac_mang"), Huyet("Kiếm Khách", "tinh_huyet_y_kiem_khach"), Huyet("Cốt Long", "tinh_cot_long") },
            Monster = Mul(Ch3Monster, ChapterMult[0]), Boss = Mul(Ch3Boss, ChapterMult[0], "Đào Ngột", "img/boss_dao_ngot.png"),
            BossHp = 1.1, Trait = new(10, 50, 15),
            BossLair = "Đào Ngột Huyệt", KeyItem = "huyet_sac_lenh",
            Drops = ScaleDrops(Realms[2].Drops, 1.6, 1.5), Quality = new double[] { 0, 40, 42, 16, 2 } },
        new ChapterDef
        {
            Index = 4, Name = "Vạn Thú Sơn", Realm = "Nguyên Anh",
            Places = new[] { "Vạn Thú Sơn", "Bách Thú Cốc", "Thú Vương Lĩnh" },
            Commons = new[] { Huyet("Huyền Ô", "quai_huyen_o"), Huyet("Ma Đằng", "quai_ma_dang"), Huyet("Cốt Khuyển", "quai_cot_khuyen"), Huyet("Lôi Ưng", "quai_loi_ung"), Huyet("Thạch Nhân", "quai_reu_thach_nhan") },
            Elites = new[] { Huyet("Lôi Kỳ Lân", "tinh_loi_ky_lan"), Huyet("Kiếm Hồn", "tinh_kiem_hon"), UMinh("Thiết Giáp Hổ", "tinh_thiet_giap_ho") },
            Monster = Mul(Ch3Monster, ChapterMult[1]), Boss = Mul(Ch3Boss, ChapterMult[1], "Cùng Kỳ", "img/boss_cung_ky.png"),
            BossHp = 1.1, Trait = new(12, 50, 20),
            BossLair = "Cùng Kỳ Sào", KeyItem = "hu_thien_tan_do", BossItem = "cung_ky_yeu_hach",
            Drops = Realms[3].Drops },
        new ChapterDef
        {
            Index = 5, Name = "Đoạn Hồn Nhai", Realm = "Nguyên Anh",
            Places = new[] { "Đoạn Hồn Nhai", "U Minh Lộ", "Đoạn Hồn Uyên" },
            Commons = new[] { Huyet("Chu Nữ", "quai_doc_chu_nu"), Huyet("Cửu Vĩ Hồ", "quai_cuu_vi_ho"), Huyet("Giáp Binh", "quai_thiet_giap_binh"), UMinh("Yêu Lang", "quai_yeu_lang"), UMinh("Độc Xà", "quai_doc_xa") },
            Elites = new[] { UMinh("Cốt Tướng", "tinh_cot_tuong"), UMinh("Hắc Mãng", "tinh_hac_mang"), UMinh("Kiếm Khách", "tinh_huyet_y_kiem_khach") },
            Monster = Mul(Ch3Monster, ChapterMult[2]), Boss = Mul(Ch3Boss, ChapterMult[2], "Hỗn Độn", "img/boss_hon_don.png"),
            BossHp = 1.1, Trait = new(15, 40, 25),
            BossLair = "Hỗn Độn Hư", KeyItem = "hu_thien_tan_do",
            Drops = ScaleDrops(Realms[3].Drops, 1.6, 1.5), Quality = new double[] { 0, 5, 45, 40, 10 } },
        new ChapterDef
        {
            Index = 6, Name = "Hỏa Diễm Sơn", Realm = "Hóa Thần",
            Places = new[] { "Hỏa Diễm Sơn", "Dung Nham Hà", "Hỏa Ngục Môn" },
            Commons = new[] { UMinh("Thạch Trư", "quai_thach_tru"), UMinh("Băng Hồ", "quai_bang_ho"), UMinh("Sơn Viên", "quai_pha_son_vien"), UMinh("Thạch Tích", "quai_tinh_thach_tich"), UMinh("Huyền Ô", "quai_huyen_o") },
            Elites = new[] { UMinh("Cốt Long", "tinh_cot_long"), UMinh("Lôi Kỳ Lân", "tinh_loi_ky_lan"), UMinh("Kiếm Hồn", "tinh_kiem_hon") },
            Monster = Mul(Ch3Monster, ChapterMult[3]), Boss = Mul(Ch3Boss, ChapterMult[3], "Ám Ma", "img/boss_am_ma.png"),
            BossHp = 1.1, Trait = new(15, 40, 30),
            BossLair = "Ám Ma Quật", KeyItem = "hoa_nguc_lenh", BossItem = "am_ma_ma_hach",
            Drops = Realms[4].Drops },
        new ChapterDef
        {
            Index = 7, Name = "Táng Tiên Cốc", Realm = "Hóa Thần",
            Places = new[] { "Táng Tiên Cốc", "Vạn Kiếm Lâm", "Kiếm Trủng" },
            Commons = new[] { UMinh("Ma Đằng", "quai_ma_dang"), UMinh("Cốt Khuyển", "quai_cot_khuyen"), UMinh("Lôi Ưng", "quai_loi_ung"), UMinh("Thạch Nhân", "quai_reu_thach_nhan"), UMinh("Chu Nữ", "quai_doc_chu_nu") },
            Elites = new[] { Kim("Thiết Giáp Hổ", "tinh_thiet_giap_ho"), Kim("Cốt Tướng", "tinh_cot_tuong"), Kim("Hắc Mãng", "tinh_hac_mang") },
            Monster = Mul(Ch3Monster, ChapterMult[4]), Boss = Mul(Ch3Boss, ChapterMult[4], "Kiếm Đế", "img/boss_kiem_de.png"),
            BossHp = 1.1, Trait = new(18, 40, 35),
            BossLair = "Kiếm Đế Cung", KeyItem = "hoa_nguc_lenh",
            Drops = ScaleDrops(Realms[4].Drops, 1.6, 1.5), Quality = new double[] { 0, 0, 25, 50, 25 } },
        new ChapterDef
        {
            Index = 8, Name = "Đông Hải Bí Cảnh", Realm = "Luyện Hư",
            Places = new[] { "Đông Hải Bí Cảnh", "Hư Không Hải Nhãn", "Long Cung Phế Tích" },
            Commons = new[] { Kim("Yêu Lang", "quai_yeu_lang"), Kim("Độc Xà", "quai_doc_xa"), Kim("Băng Hồ", "quai_bang_ho"), Kim("Cửu Vĩ Hồ", "quai_cuu_vi_ho"), Kim("Lôi Ưng", "quai_loi_ung") },
            Elites = new[] { Kim("Cốt Long", "tinh_cot_long"), Kim("Lôi Kỳ Lân", "tinh_loi_ky_lan"), Kim("Kiếm Hồn", "tinh_kiem_hon") },
            Monster = Mul(Ch3Monster, ChapterMult[5]), Boss = Mul(Ch3Boss, ChapterMult[5], "Thao Thiết", "img/boss_thao_thiet.png"),
            BossHp = 1.1, Trait = new(20, 40, 40),
            BossLair = "Thao Thiết Hải Uyên", KeyItem = "dong_hai_lenh",
            Drops = Realms[5].Drops, Quality = new double[] { 0, 0, 15, 50, 35 } },
        Locked(9, "Hư Thiên Điện", new MonsterDef("Cùng Kỳ Vương", "img/boss_cung_ky_vuong.png", 1, 1, 1)),
    };

    /// <summary>Chương tạm khóa (7–10): chỉ hiện trên thanh chương; Boss dành sẵn hình.</summary>
    private static ChapterDef Locked(int i, string name, MonsterDef boss) => new()
    {
        Index = i, Name = name, Realm = "Chưa mở", Enabled = false, Places = new[] { name },
        Commons = Chapters0Commons, Elites = Chapters0Elites, Monster = boss, Boss = boss, Trait = new(0, 0, 0),
        BossLair = name, KeyItem = "hu_thien_tan_do", Drops = Array.Empty<DropDef>() };

    /// <summary>Số chương đang mở (chương 7–10 tạm khóa).</summary>
    public static int OpenChapters => Chapters.Count(c => c.Enabled);
    /// <summary>Chương có Boss rơi nguyên liệu đột phá item (null nếu không có).</summary>
    public static ChapterDef? ChapterDropping(string item) => Chapters.FirstOrDefault(c => c.BossItem == item);
}
