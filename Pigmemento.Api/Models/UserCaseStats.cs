namespace Pigmemento.Api.Models;

public class UserCaseStats
{
    public Guid UserId { get; set; }
    public Guid CaseId { get; set; }

    public int CorrectStreak { get; set; } = 0;
    public DateTime LastAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime NextDueAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = default!;
    public Case Case { get; set; } = default!;
}