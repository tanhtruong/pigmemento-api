using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Models;

namespace Pigmemento.Api.Data;

public class AppDbContext : DbContext
{
    public DbSet<WaitlistSubscriber> WaitlistSubscribers => Set<WaitlistSubscriber>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<WaitlistSubscriber>(e =>
        {
            e.ToTable("waitlist_subscribers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();

            e.Property(x => x.Name).HasMaxLength(200);

            e.Property(x => x.Email)
                .HasColumnType("citext")
                .IsRequired();

            e.HasIndex(x => x.Email).IsUnique();

            e.Property(x => x.CreatedAtUtc)
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        });
    }
}
