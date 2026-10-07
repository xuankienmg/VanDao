namespace VanDao.Data;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Đạo hữu";

    /// <summary>Chỉ số cảnh giới trong GameData.Realms.</summary>
    public int Realm { get; set; }

    /// <summary>Chỉ số tầng/kỳ trong cảnh giới hiện tại.</summary>
    public int Stage { get; set; }

    public double Qi { get; set; }

    /// <summary>Tỉ lệ cộng thêm khi đột phá cảnh giới (tăng sau mỗi lần thất bại).</summary>
    public double Bonus { get; set; }

    /// <summary>Luôn true: nhân vật luôn tự đi thám hiểm. Giữ cột để tương thích DB cũ.</summary>
    public bool AutoExplore { get; set; } = true;

    public DateTime ExploreReadyAt { get; set; } = DateTime.UtcNow;
    public DateTime LastTickAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Bản đồ: ải đang đánh (1..20, 21 = Boss) và ải cao nhất đã mở trong cảnh giới hiện tại ----
    public int Area { get; set; } = 1;
    public int AreaMax { get; set; } = 1;

    // ---- Đột phá đại cảnh giới: thất bại phải điều tức (chờ), tẩu hỏa nhập ma bị suy yếu tạm thời ----
    public DateTime BreakCooldownUntil { get; set; } = DateTime.MinValue;
    public DateTime DebuffUntil { get; set; } = DateTime.MinValue;

    // ---- Công pháp: cấp từng kỹ năng ("id:cấp;id:cấp") và số lần hạ Boss (cho điểm kỹ năng) ----
    public string Skills { get; set; } = "";
    public int BossKills { get; set; }

    /// <summary>Ô kỹ năng: 3 ô chiêu thức + 1 ô tuyệt kỹ, "id,id,,id" (trống = chưa đặt).</summary>
    public string SkillSlots { get; set; } = "";

    /// <summary>Admin mode: cờ gian lận đang bật (god · break · fast), cách nhau dấu phẩy; điểm kỹ năng admin cộng thêm.</summary>
    public string Admin { get; set; } = "";
    public int AdminPoints { get; set; }
    public bool Cheat(string flag) => Admin.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(flag);

    /// <summary>2 ô: [0] đòn đánh thường · [1] kỹ năng có hồi chiêu. Dữ liệu 4 ô của bản cũ được xếp lại theo loại.</summary>
    public string[] Slots()
    {
        var a = SkillSlots.Split(',').Select(x => x.Trim()).ToArray();
        var n = VanDao.Game.SkillData.SkillSlotCount;
        string Fit(string id, int i) => VanDao.Game.SkillData.ById.TryGetValue(id, out var s) && VanDao.Game.SkillData.FitsSlot(s, i) ? id : "";
        if (a.Length == n) return Enumerable.Range(0, n).Select(i => Fit(a[i], i)).ToArray();
        return Enumerable.Range(0, n).Select(i => a.Select(x => Fit(x, i)).FirstOrDefault(x => x.Length > 0) ?? "").ToArray();
    }

    public Dictionary<string, int> SkillLevels() =>
        Skills.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(':'))
            .Where(x => x.Length == 2 && int.TryParse(x[1], out _)).ToDictionary(x => x[0], x => int.Parse(x[1]));

    public void SetSkillLevels(Dictionary<string, int> d) => Skills = string.Join(';', d.Where(kv => kv.Value > 0).Select(kv => $"{kv.Key}:{kv.Value}"));

    // ---- Luyện đan (bản cũ, không còn dùng; giữ cột cho DB cũ) ----
    /// <summary>Tổng kinh nghiệm luyện đan (cấp đan sư tính từ đây).</summary>
    public int AlchemyExp { get; set; }
    /// <summary>Đan phương đang luyện, -1 = lò nghỉ.</summary>
    public int RefineRecipe { get; set; } = -1;
    public DateTime RefineStartAt { get; set; } = DateTime.UtcNow;
    public DateTime RefineEndAt { get; set; } = DateTime.UtcNow;
    /// <summary>Số lò còn lại tính cả lò đang luyện; -1 = tự động luyện đến khi hết nguyên liệu.</summary>
    public int RefineLeft { get; set; }
    /// <summary>Lò hiện tại có bỏ dẫn dược hay không (đã trừ khi nhóm lò).</summary>
    public bool RefineCatalyst { get; set; }
    /// <summary>Kết quả lò gần nhất: "success|item|n", "crit|item|n" hoặc "fail|item|0".</summary>
    public string LastRefine { get; set; } = "";
    public DateTime LastRefineAt { get; set; } = DateTime.MinValue;

    /// <summary>Chỉ số vĩnh viễn từ Luyện Thể Đan.</summary>
    public int BodyHp { get; set; }
    public int BodyDef { get; set; }

    public List<InventoryItem> Inventory { get; set; } = new();
    public List<LogEntry> Logs { get; set; } = new();
    public List<EquippedItem> Equipment { get; set; } = new();

    public int Count(string itemId) => Inventory.FirstOrDefault(i => i.ItemId == itemId)?.Count ?? 0;

    public void Add(string itemId, int amount)
    {
        var slot = Inventory.FirstOrDefault(i => i.ItemId == itemId);
        if (slot is null)
        {
            slot = new InventoryItem { PlayerId = Id, ItemId = itemId, Count = 0 };
            Inventory.Add(slot);
        }
        slot.Count += amount;
    }

    public bool Has(IReadOnlyDictionary<string, int> cost) => cost.All(kv => Count(kv.Key) >= kv.Value);

    public void Pay(IReadOnlyDictionary<string, int> cost)
    {
        foreach (var (item, amount) in cost) Add(item, -amount);
    }
}

public class InventoryItem
{
    public Guid PlayerId { get; set; }
    public string ItemId { get; set; } = "";
    public int Count { get; set; }
}

public class LogEntry
{
    public long Id { get; set; }
    public Guid PlayerId { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string Text { get; set; } = "";

    /// <summary>"", "good", "bad" hoặc "gold" — quyết định màu trong nhật ký.</summary>
    public string Kind { get; set; } = "";
}

/// <summary>Trang bị đang mặc, mỗi ô (Slot) một món.</summary>
public class EquippedItem
{
    public Guid PlayerId { get; set; }
    public string Slot { get; set; } = "";
    public string ItemId { get; set; } = "";
}
