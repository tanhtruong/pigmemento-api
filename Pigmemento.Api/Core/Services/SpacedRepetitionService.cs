using Pigmemento.Api.Data;
using Pigmemento.Api.Models;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Core.Interfaces;

namespace Pigmemento.Api.Core.Services;

public class SpacedRepetitionService : ISpacedRepetitionService
{
    private readonly AppDbContext _db;

    public SpacedRepetitionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task UpdateUserCaseStatsAsync(Guid userId, Guid caseId, bool correct, long latencyMs, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var stats = await _db.UserCaseStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.CaseId == caseId, ct);

        if (stats == null)
        {
            stats = new UserCaseStats
            {
                UserId = userId,
                CaseId = caseId,
                EaseFactor = 2.5,
                IntervalDays = 0,
                NextDueAt = now
            };
            _db.UserCaseStats.Add(stats);
        }

        // --- spaced repetition update ---
        stats.LastAttemptAt = now;
        stats.LastLatencyMs = latencyMs;
        stats.LastResult = correct;
        if (!correct)
            stats.RecentlyWrongAt = now;

        if (correct)
        {
            stats.CorrectStreak++;
            stats.IntervalDays = stats.IntervalDays switch
            {
                0 => 1,
                1 => 3,
                _ => (int)Math.Round(stats.IntervalDays * stats.EaseFactor)
            };

            // small adjustment by latency (fast = slightly higher EF)
            if (latencyMs < 2500)
                stats.EaseFactor += 0.05;
            else
                stats.EaseFactor += 0.02;

            stats.EaseFactor = Math.Clamp(stats.EaseFactor, 1.3, 3.0);
        }
        else
        {
            stats.CorrectStreak = 0;
            stats.IntervalDays = 1;
            stats.EaseFactor = Math.Max(1.3, stats.EaseFactor - 0.2);
        }

        stats.NextDueAt = now.AddDays(stats.IntervalDays);
        stats.LastSeenAt = now; // optional, helps avoid duplicates

        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkSeenAsync(Guid userId, IEnumerable<Guid> caseIds, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var statsList = await _db.UserCaseStats
            .Where(s => s.UserId == userId && caseIds.Contains(s.CaseId))
            .ToListAsync(ct);

        foreach (var id in caseIds)
        {
            var s = statsList.FirstOrDefault(x => x.CaseId == id);
            if (s == null)
            {
                s = new UserCaseStats { UserId = userId, CaseId = id, LastSeenAt = now };
                _db.UserCaseStats.Add(s);
            }
            else
            {
                s.LastSeenAt = now;
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}