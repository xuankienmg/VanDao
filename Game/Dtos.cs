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
    string[] LockedRealms);

public record RealmInfoDto(int Index, string Id, string Name, string Desc, string[] Stages,
    string Aura, string Lotus, string Sash, string Hero, string[] Resources,
    string Cn, int BagSlots, string[] EquipSlots, string Boss, string BossImage, string BossLair, string KeyItem, string BossItem);

// ---- Trạng thái người chơi ----
public record CostDto(string Item, int Need, int Have);

/// <summary>Bản đồ cảnh giới hiện tại: ải đang đánh, ải đã mở, dự đoán thắng/thua từng ải, chìa khóa và yêu hạch Boss.</summary>
public record MapDto(int Area, int AreaMax, string KeyItem, string BossItem, string BossLair, double KeyChanceMin, double KeyChanceMax,
    List<AreaDto> Areas, List<SynthStateDto> Synths, bool HasBossItem);

public record AreaDto(int Index, string Monster, string Place, int Hp, int Atk, int Def, bool Win, int Hits, bool Open);

public record SynthStateDto(string From, int Max);

/// <summary>Đột phá: tỉ lệ gốc, thời gian điều tức sau thất bại, thời gian suy yếu (tẩu hỏa nhập ma), thay đổi khi thành công.</summary>
public record BreakStateDto(double BaseChance, long CooldownMs, long DebuffMs, BreakPreviewDto Preview);

public record BreakPreviewDto(int Hp, int Atk, int Def, double RatePct, int BagSlots, string[] NewSlots, string[] NewStats);

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
    string Admin = "");   // cờ admin mode đang bật

public record BonusDto(double QiPct, double LuckPct, double SpeedPct);

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
    bool[]? Burned = null);               // lượt nào thiêu đốt kích hoạt (30%)

/// <summary>Kỹ năng tự thi triển ở lượt Turn (0-based) của hoạt ảnh trận.</summary>
public record CastDto(int Turn, string Skill, string Name, string Anim, int Dmg);

/// <summary>Kết quả một lần luyện chế/hợp thành: thành phẩm và nguyên liệu đã tiêu hao (dùng chung cho mọi công thức).</summary>
public record ItemQtyDto(string Item, int Qty);
public record CraftResultDto(string Recipe, ItemQtyDto Output, List<ItemQtyDto> Inputs);

public record ActionResponse(StateDto State, string? Message = null, string? Kind = null, CraftResultDto? Craft = null);

public record CreatePlayerRequest(string? Name);

public class GameException(string message) : Exception(message);
