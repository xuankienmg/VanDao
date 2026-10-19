namespace VanDao.Game;

// ---- Dữ liệu tĩnh cho giao diện ----
public record GameDataDto(
    IEnumerable<ItemDef> Items,
    IEnumerable<SlotDef> Slots,
    IEnumerable<StatDef> ExtraStats,
    IEnumerable<RealmInfoDto> Realms,
    IEnumerable<SynthDef> Synths,
    IEnumerable<ElementDef> Elements,
    IEnumerable<SkillDef> SkillTree,
    string[] PillChain,
    int Areas,
    string[] LockedRealms,
    double[]? FuseChance = null,
    IEnumerable<ChapterInfoDto>? Chapters = null,
    IEnumerable<BrewDef>? Brews = null,
    IEnumerable<HerbDropDef>? HerbDrops = null,
    IEnumerable<SkinDef>? Skins = null,
    double[]? SpinWeights = null,
    int SpinPrice = 0);

public record RealmInfoDto(int Index, string Id, string Name, string Desc, string[] Stages,
    string Aura, string Lotus, string Sash, string Hero, string[] Resources,
    string Cn, int BagSlots, string[] EquipSlots, string Boss, string BossImage, string BossLair, string KeyItem, string BossItem,
    string? HeroStand = null, string? HeroMed = null, string? HeroCast = null, string? Halo = null);

// ---- Trạng thái người chơi ----
public record CostDto(string Item, int Need, int Have);

/// <summary>Bản đồ cảnh giới hiện tại: ải đang đánh, ải đã mở, dự đoán thắng/thua từng ải, chìa khóa và yêu hạch Boss.</summary>
public record MapDto(int Area, int AreaMax, string KeyItem, string BossItem, string BossLair, double KeyChanceMin, double KeyChanceMax,
    List<AreaDto> Areas, List<SynthStateDto> Synths, bool HasBossItem, int Realm = 0,
    List<DropInfoDto>? Drops = null, double ChestChance = 0, double RareChestChance = 0, int RareChestMinArea = 0, string BossChest = "",
    int ChapterMax = 0, string Boss = "", string BossImage = "", double[]? Quality = null,
    int Diff = 0, bool[]? DiffOpen = null, bool[]? ChapterOpen = null, bool[]? ChapterCleared = null, double RewardMult = 1, double MonMult = 1);
// Diff = mức độ khó đang đánh (0 Thường · 1 Khó · 2 Ác mộng); DiffOpen = mức đã mở; ChapterOpen / ChapterCleared theo mức đang chọn
// Realm = chương đang đánh (0-based, tên giữ cho tương thích client: nhãn ải "C-x")

/// <summary>Thông tin tĩnh một chương cho thanh chọn chương.</summary>
public record ChapterInfoDto(int Index, string Name, string Realm, bool Enabled, string Boss, string BossImage, string BossLair, string KeyItem, string? BossItem);

/// <summary>Bảng rơi của bản đồ (tỉ lệ gốc ở ải 1; client tự nhân theo ải giống server).</summary>
public record DropInfoDto(string Item, int Min, int Max, double Chance, bool Gear, int MinArea = 1);

public record AreaDto(int Index, string Monster, string Place, int Hp, int Atk, int Def, bool Win, int Hits, bool Open, string Image = "",
    List<WaveInfoDto>? Waves = null, bool Elite = false);

/// <summary>Một đợt quái của ải (bảng thông tin ải trên bản đồ).</summary>
public record WaveInfoDto(string Name, string Image, string Tier, int Hp, int Atk, int Def, int StoneMin = 0, int StoneMax = 0);

public record SynthStateDto(string From, int Max);

/// <summary>Đột phá: tỉ lệ gốc, thời gian điều tức sau thất bại, thời gian suy yếu (tẩu hỏa nhập ma), thay đổi khi thành công.</summary>
public record BreakStateDto(double BaseChance, long CooldownMs, long DebuffMs, BreakPreviewDto Preview);

public record BreakPreviewDto(int Hp, int Atk, int Def, double RatePct, int BagSlots, string[] NewSlots, string[] NewStats, int SkillPoints = 0);

public record LogDto(DateTime At, string Text, string Kind);

public record StateDto(
    Guid Id,
    string Name,
    int Realm,
    int Stage,
    string RealmName,
    string StageName,
    double Qi,
    double QiReq,
    double Rate,
    double ChannelGain,
    bool IsPeak,
    bool HasNext,
    string TargetTitle,
    string TargetKind,
    List<CostDto> TargetCost,
    double Chance,
    double Bonus,
    long ExploreReadyInMs,
    bool AutoExplore,
    long ExploreCooldownMs,
    double ExploreQiGain,
    Dictionary<string, string> Equipment,
    BonusDto Bonuses,
    CombatStats Stats,
    BattleDto Battle,
    string RealmCn,
    string NextRealmName,
    string NextRealmCn,
    bool AtTop,
    int BagUsed,
    int BagCap,
    string[] EquipSlots,
    int Level,
    Dictionary<string, int> Inventory,
    MapDto Map,
    Dictionary<string, int> Skills,
    string[] SkillSlots,
    int SkillPoints,
    int SkillPointsTotal,
    BreakStateDto Break,
    List<LogDto> Logs,
    string Admin = "",    // cờ admin mode đang bật
    List<ChestOddsDto>? ChestOdds = null,   // tỉ lệ phần thưởng khi mở rương (hiện ở chế độ admin)
    double PillBonus = 0,    // tỉ lệ đột phá cộng thêm từ Phá Chướng Đan
    string Skin = "",
    string SpinHistory = "");   // lịch sử quay Tài Xỉu: "unixGiây|id,id,…;…", mới nhất trước       // trang phục đang mặc, rỗng = theo cảnh giới   // tỉ lệ phần thưởng khi mở rương (hiện ở chế độ admin)

/// <summary>Tỉ lệ (%) phần thưởng 1 lần mở rương với nhân vật hiện tại: Level = cấp đồ ra, Quality = % cấp Phàm → Thiên,
/// Treasure / Gear = % từng món gốc (pháp bảo / trang bị), Pill = % ra đan (PillItem, PillMin–PillMax viên).</summary>
public record ChestOddsDto(string Chest, int Level, double[] Quality, List<ItemQtyPctDto> Treasure, List<ItemQtyPctDto> Gear,
    string PillItem, int PillMin, int PillMax, double Pill);
public record ItemQtyPctDto(string Item, double Pct);

public record BonusDto(double QiPct, double SpeedPct);

/// <summary>
/// Kết quả trận đánh của chuyến thám hiểm (tính sẵn từ chỉ số, không ngẫu nhiên)
/// để hoạt ảnh ở client khớp đúng với server.
/// </summary>
public record BattleDto(string Monster, string Image, int MonsterHp, int MonsterAtk, int MonsterDef,
    int HeroDmg, int MonsterDmg, int Hits, bool Win,
    int[] MonHpLeft, int[] HeroHpLeft, bool[] MonSkip, List<CastDto> Casts, int HeroHp,
    string Basic = "hoa_cau",    // chiêu dùng làm đòn đánh thường (để client chọn animation)
    int[]? Rage = null,          // nộ cuối mỗi lượt, 0–100 (lượt tung kỹ năng: về 0)
    int Burn = 0, double BurnPct = 0,    // thiêu đốt (Phần Cốt): sát thương mỗi lần kích hoạt · % công
    bool[]? Burned = null,                // lượt nào thiêu đốt kích hoạt (30%)
    // hiệu ứng trang bị theo cấp (GameService.GearEffects), mảng theo lượt như Burned:
    bool[]? Bolt = null, int BoltDmg = 0,  // Thiên lôi: lượt nào giáng sét · sát thương mỗi tia
    bool[]? Extra = null,                  // Liên kích: lượt nào đánh thêm 1 đòn thường (HeroDmg)
    int[]? Heal = null,                    // Hồi khí: máu hồi cuối lượt
    int[]? Reflect = null,                 // Địa mạch: sát thương phản lại quái
    int Guard = 0, int[]? GuardLeft = null,   // Thổ thuẫn: khiên đầu trận · khiên còn lại cuối mỗi lượt
    int FirstHit = 0,                      // Địa chấn: sát thương thêm ở lượt đầu
    int EnrageTurn = -1, int MonsterDmgEnraged = 0,   // Boss Cuồng bạo: lượt (đã lấy mẫu) bắt đầu nổi giận · sát thương mỗi đòn khi đó
    List<WaveDto>? Waves = null);           // các đợt quái đã giao chiến (ải thường 3 đợt): Start = lượt (đã lấy mẫu) đợt bắt đầu

/// <summary>Một đợt quái trong trận: chỉ số đã tính công pháp/trang bị, sát thương hai bên, lượt bắt đầu (đã lấy mẫu).</summary>
/// <summary>Stones = linh thạch rơi khi hạ quái đợt này (đã gieo sẵn theo chuyến, server cộng đúng số này).</summary>
public record WaveDto(string Name, string Image, string Tier, int Hp, int Atk, int Def, int HeroDmg, int MonsterDmg, int Start, int Stones = 0);

/// <summary>Kỹ năng tự thi triển ở lượt Turn (0-based) của hoạt ảnh trận.</summary>
public record CastDto(int Turn, string Skill, string Name, string Anim, int Dmg);

/// <summary>Kết quả một lần luyện chế/hợp thành: thành phẩm và nguyên liệu đã tiêu hao (dùng chung cho mọi công thức).</summary>
public record ItemQtyDto(string Item, int Qty);
public record CraftResultDto(string Recipe, ItemQtyDto Output, List<ItemQtyDto> Inputs, List<ItemQtyDto>? Outputs = null);

public record ActionResponse(StateDto State, string? Message = null, string? Kind = null, CraftResultDto? Craft = null);

public record CreatePlayerRequest(string? Name);

public class GameException(string message) : Exception(message);
