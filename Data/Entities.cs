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
