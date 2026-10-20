using VanDao.Data;

namespace VanDao.Game;

public record DivineProgress(int Tier, string Quality, string Name, string Description, int Count, bool Active);
public record DivineAbilityState(int Tier, string? SetId, string? SetName, string Name, string Description, DivineProgress[] Progress);

public static class DivineAbility
{
    public static readonly (int Tier, string Name, string Description)[] Definitions =
    {
        (6, "Bất Diệt Thần Khu", "Nhận đòn chí tử: giữ 1 HP, bất tử đến hết lượt. Một lần mỗi trận."),
        (7, "Tiên Linh Hộ Mệnh", "Có 15% cơ hội vô hiệu hóa đòn tấn công của quái nếu né tránh thất bại."),
        (8, "Đạo Ảnh Liên Kích", "Thêm 20% liên kích."),
        (9, "Nghịch Chuyển Thiên Cơ", "Có 20% cơ hội vô hiệu hóa đòn tấn công của quái nếu né tránh thất bại. Khi né hoặc vô hiệu hóa thành công, đòn thường hoặc kỹ năng tấn công kế tiếp tăng 100% sát thương. Không cộng dồn.")
    };
    public static DivineAbilityState For(Player p)
    {
        var groups = EquipmentEffects.ValidItems(p).Where(i => i.SetId != null)
            .GroupBy(i => i.SetId).Select(g => new { Id=g.Key, Items=g.ToArray(),
                Tier=g.Count()==10 ? g.Min(i=>Array.IndexOf(GameData.RarityOrder,i.Rarity)) : 0 })
            .OrderByDescending(g=>g.Tier).ThenByDescending(g=>g.Items.Length).ThenBy(g=>g.Id).ToArray();
        var best=groups.FirstOrDefault();
        var tier=best?.Tier >= 6 ? best.Tier : 0;
        var definition=Definitions.FirstOrDefault(d=>d.Tier==tier);
        return new(tier,best?.Id,best?.Items[0].Set,definition.Name ?? "Chưa kích hoạt",definition.Description ?? "Cần 10 món cùng bộ đạt phẩm Thần trở lên.",
            Definitions.Select(d=>new DivineProgress(d.Tier,GameData.QualityName[d.Tier],d.Name,d.Description,
                best?.Items.Count(i=>Array.IndexOf(GameData.RarityOrder,i.Rarity)>=d.Tier) ?? 0,tier==d.Tier)).ToArray());
    }
}

/// <summary>One instance per battle, retained across waves. Only the selected tier operates.</summary>
public sealed class DivineCombat(int tier)
{
    private bool used, protectedThisTurn, charged;
    public bool Saved { get; private set; }
    public bool Charged => charged;
    public void BeginTurn(){protectedThisTurn=false;Saved=false;}
    public double ExtraChance(double chance) => tier==8 ? Math.Min(50,chance+20) : chance;
    public bool Nullify(Random rng) => (tier==7 || tier==9) && rng.NextDouble()<(tier==9 ? .20 : .15);
    public void Avoided(){if(tier==9)charged=true;}
    public int Amplify(int damage)
    {
        if(!charged || damage<=0)return damage;
        charged=false;
        return (int)Math.Min(int.MaxValue,(long)damage*2);
    }
    public int Receive(int hp,int damage)
    {
        if(protectedThisTurn)return hp;
        if(tier==6 && !used && damage>=hp && hp>0){used=true;protectedThisTurn=true;Saved=true;return 1;}
        return hp-damage;
    }
}
