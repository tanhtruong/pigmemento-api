// ------------------------------------------------------------
// File: Core/Services/ProgressService.cs
// ------------------------------------------------------------
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Contracts;
using Pigmemento.Api.Core.Interfaces;
using Pigmemento.Api.Models;

namespace Pigmemento.Api.Core.Services;

public class ProgressService : IProgressService
{
    private readonly AppDbContext _db;

    public ProgressService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProgressDto> GetProgressAsync(Guid userId, CancellationToken ct)
    {
        var q = _db.Attempts
            .AsNoTracking()
            .Where(a => a.UserId == userId);

        var total = await q.CountAsync(ct);
        if (total == 0)
        {
            return new ProgressDto
            {
                Accuracy = 0,
                Sensitivity = 0,
                Specificity = 0,
                AvgTimeMs = 0,
                TotalAttempts = 0,
                StreakDays = 0
            };
        }

        var avgTime = await q.AverageAsync(a => (double)a.TimeToAnswerMs, ct);

        // Join with Cases for sensitivity/specificity computation
        var joined = from a in _db.Attempts.AsNoTracking()
                     join c in _db.Cases.AsNoTracking() on a.CaseId equals c.Id
                     where a.UserId == userId
                     select new { a.Correct, a.Answer, c.Label, a.CreatedAt };

        int tp = await joined.CountAsync(x => x.Answer == "malignant" && x.Label == "malignant", ct);
        int fn = await joined.CountAsync(x => x.Answer == "benign" && x.Label == "malignant", ct);
        int tn = await joined.CountAsync(x => x.Answer == "benign" && x.Label == "benign", ct);
        int fp = await joined.CountAsync(x => x.Answer == "malignant" && x.Label == "benign", ct);

        double accuracy = (tp + tn) / Math.Max(1.0, tp + tn + fp + fn);
        double sensitivity = tp / Math.Max(1.0, tp + fn);
        double specificity = tn / Math.Max(1.0, tn + fp);

        // Compute daily streaks
        var days = await q
            .GroupBy(a => DateOnly.FromDateTime(a.CreatedAt.Date))
            .Select(g => g.Key)
            .OrderByDescending(d => d)
            .ToListAsync(ct);

        int streak = 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var cursor = today;

        foreach (var d in days)
        {
            if (d == cursor)
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }
            else if (d > cursor)
            {
                // skip future days
            }
            else break;
        }

        // Trend over the last 14 days
        var since = DateTime.UtcNow.Date.AddDays(-13);
        var trendData = await joined
            .Where(x => x.CreatedAt >= since)
            .ToListAsync(ct);

        var trend = Enumerable.Range(0, 14)
            .Select(i => DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-13 + i)))
            .Select(d =>
            {
                var items = trendData.Where(x => DateOnly.FromDateTime(x.CreatedAt.Date) == d).ToList();
                if (items.Count == 0)
                    return new TrendPoint { Date = d, Accuracy = 0, Sensitivity = 0 };

                double dayAcc = items.Count(x => x.Correct) / (double)items.Count;
                int dayTp = items.Count(x => x.Answer == "malignant" && x.Label == "malignant");
                int dayFn = items.Count(x => x.Answer == "benign" && x.Label == "malignant");
                double daySens = dayTp / Math.Max(1.0, dayTp + dayFn);

                return new TrendPoint { Date = d, Accuracy = dayAcc, Sensitivity = daySens };
            })
            .ToList();

        return new ProgressDto
        {
            Accuracy = accuracy,
            Sensitivity = sensitivity,
            Specificity = specificity,
            AvgTimeMs = avgTime,
            TotalAttempts = total,
            StreakDays = streak,
            Trend = trend
        };
    }

    public async Task<List<RecentAttemptDto>> GetRecentAttemptsAsync(Guid userId, int limit, CancellationToken ct)
    {
        var attempts = await _db.Attempts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new RecentAttemptDto
            {
                Id = a.Id,
                CaseId = a.CaseId,
                Correct = a.Correct,
                Answer = a.Answer,
                CreatedAt = a.CreatedAt,
                TimeToAnswerMs = a.TimeToAnswerMs
            })
            .ToListAsync(ct);

        return attempts;
    }

    public async Task<DrillsDueDto> GetDrillsDueAsync(Guid userId, CancellationToken ct)
    {
        // Simple placeholder: could use spaced repetition scheduling later
        var recent = await _db.Attempts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return new DrillsDueDto
        {
            Count = 5,
            NextDueAt = recent?.CreatedAt.AddDays(1)
        };
    }
}