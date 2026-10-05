using Microsoft.EntityFrameworkCore;

namespace VanDao.Data;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<LogEntry> Logs => Set<LogEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Player>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(64);
            e.HasMany(p => p.Inventory).WithOne().HasForeignKey(i => i.PlayerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Logs).WithOne().HasForeignKey(l => l.PlayerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<InventoryItem>(e =>
        {
            e.HasKey(i => new { i.PlayerId, i.ItemId });
            e.Property(i => i.ItemId).HasMaxLength(40);
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
