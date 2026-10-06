namespace VanDao.Game;

// ---- Dữ liệu tĩnh cho giao diện ----
public record GameDataDto(
    IEnumerable<ItemDef> Items,
    IEnumerable<SlotDef> Slots,
    IEnumerable<StatDef> ExtraStats,
    IEnumerable<RealmInfoDto> Realms,
    IEnumerable<RecipeInfoDto> Recipes,
    string[] LockedRealms);

public record RealmInfoDto(int Index, string Id, string Name, string Desc, string[] Stages,
    string Aura, string Lotus, string Sash, string Hero, string[] Resources,
    string Cn, int BagSlots, string[] EquipSlots);

public record RecipeInfoDto(int Index, string Name, string OutItem, int OutCount,
    Dictionary<string, int> In, double Rate, int MinRealm, string MinRealmName);

// ---- Trạng thái người chơi ----
public record CostDto(string Item, int Need, int Have);

public record RecipeStateDto(int Index, bool Locked, bool CanCraft);

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
    List<RecipeStateDto> Recipes,
    List<LogDto> Logs);

public record BonusDto(double QiPct, double LuckPct, double SpeedPct);

/// <summary>
/// Kết quả trận đánh của chuyến thám hiểm (tính sẵn từ chỉ số, không ngẫu nhiên)
/// để hoạt ảnh ở client khớp đúng với server.
/// </summary>
public record BattleDto(string Monster, string Image, int MonsterHp, int MonsterAtk, int MonsterDef,
    int HeroDmg, int MonsterDmg, int Hits, bool Win);

public record ActionResponse(StateDto State, string? Message = null, string? Kind = null);

public record CreatePlayerRequest(string? Name);

public class GameException(string message) : Exception(message);
