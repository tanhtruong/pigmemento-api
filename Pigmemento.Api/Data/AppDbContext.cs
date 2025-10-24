using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Models;

namespace Pigmemento.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Existing
    public DbSet<WaitlistSubscriber> WaitlistSubscribers => Set<WaitlistSubscriber>();
    public DbSet<User> Users => Set<User>();

    // New for training app
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<TeachingPoint> TeachingPoints => Set<TeachingPoint>();
    public DbSet<UserCaseStats> UserCaseStats => Set<UserCaseStats>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Postgres extensions
        modelBuilder.HasPostgresExtension("citext");

        // --- Waitlist ---
        modelBuilder.Entity<WaitlistSubscriber>(e =>
        {
            e.ToTable("waitlist_subscribers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Email).HasColumnType("citext").IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.CreatedAtUtc).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        });

        // --- Users ---
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            b.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(128);

            b.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("citext")
                .HasColumnName("email");

            b.HasIndex(x => x.Email)
                .IsUnique();

            b.Property(x => x.PasswordHash)
                .IsRequired()
                .HasColumnName("password_hash");

            b.Property(x => x.Role)
                .IsRequired()
                .HasMaxLength(32)
                .HasDefaultValue("user")
                .HasColumnName("role");

            b.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            b.Property(x => x.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            b.Property(x => x.LastLoginUtc)
                .HasColumnName("last_login_utc");
        });

        // --- Cases ---
        modelBuilder.Entity<Case>(e =>
        {
            e.ToTable("cases");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedOnAdd();
            e.Property(c => c.ImageUrl).IsRequired().HasColumnName("image_url");
            e.Property(c => c.Label).IsRequired().HasMaxLength(16).HasColumnName("label");
            e.Property(c => c.Diagnosis2).HasMaxLength(64).HasColumnName("diagnosis_2");
            e.Property(c => c.Diagnosis3).HasMaxLength(64).HasColumnName("diagnosis_3");
            e.Property(c => c.Difficulty).IsRequired().HasMaxLength(8).HasColumnName("difficulty");
            e.Property(c => c.Metadata).HasColumnName("metadata");

            // Owned type → flattened columns
            e.OwnsOne(c => c.Patient, p =>
            {
                p.Property(pp => pp.Age).HasColumnName("patient_age");
                p.Property(pp => pp.Site).HasMaxLength(64).HasColumnName("patient_site");
                p.Property(pp => pp.Sex).HasMaxLength(16).HasColumnName("patient_sex");
                p.Property(pp => pp.FitzpatrickType).HasMaxLength(8).HasColumnName("patient_fitzpatrick");
            });

            e.HasIndex(c => c.Difficulty);
            e.HasIndex(c => c.Label);
        });

        // --- Attempts ---
        modelBuilder.Entity<Attempt>(e =>
        {
            e.ToTable("attempts");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedOnAdd();

            e.Property(a => a.UserId).HasColumnName("user_id");
            e.Property(a => a.CaseId).HasColumnName("case_id");
            e.Property(a => a.Answer).IsRequired().HasMaxLength(16).HasColumnName("answer");
            e.Property(a => a.Correct).IsRequired().HasColumnName("correct");
            e.Property(a => a.TimeToAnswerMs).IsRequired().HasColumnName("time_ms");
            e.Property(a => a.CreatedAt).IsRequired().HasColumnName("created_at")
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            e.HasOne(a => a.User)
            .WithMany(u => u.Attempts)
            .HasForeignKey(a => a.UserId)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Case)
            .WithMany(c => c.Attempts)
            .HasForeignKey(a => a.CaseId)
            .HasPrincipalKey(c => c.Id)
            .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(a => new { a.UserId, a.CreatedAt });
            e.HasIndex(a => new { a.UserId, a.CaseId, a.CreatedAt });
        });

        // --- TeachingPoints ---
        modelBuilder.Entity<TeachingPoint>(e =>
        {
            e.ToTable("teaching_points");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).ValueGeneratedOnAdd();
            e.Property(t => t.CaseId).HasColumnName("case_id");
            e.Property(t => t.Points).IsRequired().HasColumnName("points");

            e.HasOne(t => t.Case)
            .WithMany(c => c.TeachingPoints)
            .HasForeignKey(t => t.CaseId)
            .HasPrincipalKey(c => c.Id)
            .OnDelete(DeleteBehavior.Cascade);
        });

        // --- UserCaseStats ---
        modelBuilder.Entity<UserCaseStats>(e =>
        {
            e.ToTable("user_case_stats");
            e.HasKey(s => new { s.UserId, s.CaseId });

            // --- Columns / names ---
            e.Property(s => s.CorrectStreak).HasColumnName("correct_streak");

            // Prefer timestamptz for true-UTC storage
            e.Property(s => s.LastAttemptAt)
                .HasColumnName("last_attempt_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .IsRequired(false);                // nullable in the new model

            e.Property(s => s.NextDueAt)
                .HasColumnName("next_due_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            e.Property(s => s.EaseFactor)
                .HasColumnName("ease_factor")
                .HasDefaultValue(2.5);

            e.Property(s => s.IntervalDays)
                .HasColumnName("interval_days")
                .HasDefaultValue(0);

            e.Property(s => s.LastResult)
                .HasColumnName("last_result");     // bool? (nullable)

            e.Property(s => s.LastLatencyMs)
                .HasColumnName("last_latency_ms"); // bigint? (nullable)

            e.Property(s => s.LastSeenAt)
                .HasColumnName("last_seen_at")
                .HasColumnType("timestamp with time zone");

            e.Property(s => s.RecentlyWrongAt)
                .HasColumnName("recently_wrong_at")
                .HasColumnType("timestamp with time zone");

            // --- Relationships ---
            e.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Case)
                .WithMany()
                .HasForeignKey(s => s.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Indexes for feed queries ---
            e.HasIndex(s => s.NextDueAt).HasDatabaseName("ix_user_case_stats_next_due_at");

            // Fast “recently wrong” bucket (only rows that are set)
            e.HasIndex(s => s.RecentlyWrongAt)
                .HasDatabaseName("ix_user_case_stats_recently_wrong_at")
                .HasFilter("\"recently_wrong_at\" IS NOT NULL");

            // Cooldown / reservation lookups
            e.HasIndex(s => s.LastSeenAt)
                .HasDatabaseName("ix_user_case_stats_last_seen_at")
                .HasFilter("\"last_seen_at\" IS NOT NULL");

            // Helpful compound index if you often filter by (user, due)
            e.HasIndex(s => new { s.UserId, s.NextDueAt })
                .HasDatabaseName("ix_user_case_stats_user_due");

            // --- Concurrency token ---
            // Preferred for Postgres: use the system xmin column (no extra storage).
            // Requires package Npgsql.EntityFrameworkCore.PostgreSQL >= 6
            // e.UseXminAsConcurrencyToken();

            // If you already added a RowVersion bytea property instead, use:
            e.Property(s => s.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();
        });

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.ReplacedBy)
            .WithOne()
            .HasForeignKey<RefreshToken>(rt => rt.ReplacedById)
            .OnDelete(DeleteBehavior.Restrict);
    }


}