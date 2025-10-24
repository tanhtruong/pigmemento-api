using System.ComponentModel.DataAnnotations;

namespace Pigmemento.Api.Models;

public class UserCaseStats
{
    // Composite key (UserId, CaseId)
    public Guid UserId { get; set; }
    public Guid CaseId { get; set; }

    // Spaced repetition core
    public double EaseFactor { get; set; } = 2.5;
    public int IntervalDays { get; set; } = 0;
    public DateTime NextDueAt { get; set; }

    // Performance snapshot
    public int CorrectStreak { get; set; } = 0;
    public bool? LastResult { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public long? LastLatencyMs { get; set; }

    // Session routing helpers
    public DateTime? LastSeenAt { get; set; }
    public DateTime? RecentlyWrongAt { get; set; }

    // Concurrency/ETag
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    // Navs
    public User User { get; set; } = default!;
    public Case Case { get; set; } = default!;
}