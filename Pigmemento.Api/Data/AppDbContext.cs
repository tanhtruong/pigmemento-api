using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Models;

namespace Pigmemento.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<TeachingPoint> TeachingPoints => Set<TeachingPoint>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Case>()
            .HasMany(c => c.TeachingPoints)
            .WithOne(tp => tp.Case)
            .HasForeignKey(tp => tp.CaseId);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Attempts)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId);
        
        modelBuilder.Entity<Attempt>()
            .HasOne(a => a.User)
            .WithMany(u => u.Attempts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<Case>()
            .Property(c => c.AdditionalDiagnoses)
            .HasColumnType("text[]");
        
        modelBuilder.Entity<TeachingPoint>()
            .HasOne(tp => tp.Case)
            .WithMany(c => c.TeachingPoints)
            .HasForeignKey(tp => tp.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<WaitlistEntry>()
            .HasIndex(x => x.EmailHash)
            .IsUnique();
    }
    
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}