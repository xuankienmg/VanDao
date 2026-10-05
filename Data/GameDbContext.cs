using Microsoft.EntityFrameworkCore;

namespace VanDao.Data;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<EquippedItem> Equipment => Set<EquippedItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Player>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(64);
            e.HasMany(p => p.Inventory).WithOne().HasForeignKey(i => i.PlayerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Logs).WithOne().HasForeignKey(l => l.PlayerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Equipment).WithOne().HasForeignKey(q => q.PlayerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<InventoryItem>(e =>
        {
            e.HasKey(i => new { i.PlayerId, i.ItemId });
            e.Property(i => i.ItemId).HasMaxLength(40);
        });

        b.Entity<EquippedItem>(e =>
        {
            e.HasKey(q => new { q.PlayerId, q.Slot });
            e.Property(q => q.Slot).HasMaxLength(20);
            e.Property(q => q.ItemId).HasMaxLength(40);
        });

        b.Entity<LogEntry>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => new { l.PlayerId, l.Id });
            e.Property(l => l.Text).HasMaxLength(500);
            e.Property(l => l.Kind).HasMaxLength(10);
        });
    }
}

public static class SchemaUpgrade
{
    /// <summary>
    /// EnsureCreated không sửa DB đã có, nên tự thêm cột/bảng mới cho file vandao.db của bản cũ.
    /// </summary>
    public static void Apply(GameDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Players') WHERE name = 'AutoExplore'";
        if (Convert.ToInt64(cmd.ExecuteScalar()) == 0)
        {
            cmd.CommandText = "ALTER TABLE \"Players\" ADD COLUMN \"AutoExplore\" INTEGER NOT NULL DEFAULT 1";
            cmd.ExecuteNonQuery();
        }
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS "Equipment" (
                "PlayerId" TEXT NOT NULL,
                "Slot" TEXT NOT NULL,
                "ItemId" TEXT NOT NULL,
                CONSTRAINT "PK_Equipment" PRIMARY KEY ("PlayerId", "Slot"),
                CONSTRAINT "FK_Equipment_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id") ON DELETE CASCADE
            )
            """;
        cmd.ExecuteNonQuery();
        conn.Close();
    }
}
