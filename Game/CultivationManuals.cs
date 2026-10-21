using VanDao.Data;

namespace VanDao.Game;

public record CultivationManual(string Item, string Element, string Name, string Color);

/// <summary>Permanent tree access is separate from refundable skill levels.</summary>
public static class CultivationManuals
{
    public const double BossChance = .01;
    public const string Starter = "bi_kip_hoa";
    public static readonly CultivationManual[] All =
    [
        new(Starter, "hoa", "Xích Diễm Phần Thiên Kinh", "#e0521e"),
        new("bi_kip_thuy", "thuy", "Thương Hải Huyền Thủy Kinh", "#3f8fd8"),
        new("bi_kip_moc", "moc", "Thanh Đế Trường Sinh Kinh", "#4f9a5a"),
        new("bi_kip_tho", "tho", "Hậu Thổ Trấn Sơn Kinh", "#b08a4a"),
        new("bi_kip_loi", "loi", "Cửu Tiêu Ngự Lôi Kinh", "#a46ae6"),
        new("bi_kip_bang", "bang", "Huyền Băng Ngưng Thiên Kinh", "#a8dcf0"),
        new("bi_kip_phong", "gio", "Thái Hư Ngự Phong Kinh", "#8fd0b0"),
    ];
    public static CultivationManual[] Available => All.Where(m => SkillData.Elements.Any(e => e.Id == m.Element && e.Open)).ToArray();
    public static readonly ItemDef[] Items = All.Select(m => new ItemDef(m.Item, m.Name, "manual", m.Color,
        $"Bí kíp đạo pháp hệ {SkillData.Elements.First(e => e.Id == m.Element).Name}. Lĩnh ngộ một lần để mở vĩnh viễn cây kỹ năng của hệ này.",
        Icon: "img/bi_kip_cong_phap.png", Tint: m.Element switch
        {
            "hoa" => "hue-rotate(-28deg) saturate(2.6) brightness(1.12)",
            "thuy" => "hue-rotate(180deg) saturate(2.4) brightness(1.25)",
            "moc" => "hue-rotate(82deg) saturate(2.2) brightness(1.15)",
            "tho" => "saturate(.7)",
            "loi" => "hue-rotate(245deg) saturate(2) brightness(1.2)",
            "bang" => "hue-rotate(155deg) saturate(.8) brightness(1.65)",
            "gio" => "hue-rotate(110deg) saturate(1.2) brightness(1.4)",
            _ => "none"
        }, Rarity: "legendary", Kind: "Công pháp",
        Use: Available.Contains(m)
            ? "Dùng: mở cây kỹ năng tương ứng. Nâng kỹ năng vẫn cần điểm và cảnh giới. Rương Boss: 1% nhận bí kíp Hỏa, Thủy hoặc Mộc."
            : "Hệ này chưa mở. Bí kíp được giữ lại, hiện không thể học và không rơi từ rương." )).ToArray();

    public static string[] Unlocked(Player p) => p.LearnedManuals.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Where(id => Available.Any(m => m.Element == id)).Distinct().ToArray();
    public static bool IsUnlocked(Player p, string element) => Unlocked(p).Contains(element);
    public static void Unlock(Player p, string element) => p.LearnedManuals = string.Join(',', p.LearnedManuals.Split(',', StringSplitOptions.RemoveEmptyEntries).Append(element).Distinct());
    public static void Migrate(Player p)
    {
        if (p.ManualVersion > 0) return;
        // Legacy characters keep the innate fire tree and every learned/equipped tree.
        Unlock(p, "hoa");
        foreach (var id in p.SkillLevels().Where(kv => kv.Value > 0).Select(kv => kv.Key).Concat(p.Slots()))
            if (SkillData.ById.TryGetValue(id, out var skill)) Unlock(p, skill.Element);
        p.ManualVersion = 1;
    }
}
