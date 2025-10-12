using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Auth.Core;
using Pigmemento.Api.Contracts;
using Pigmemento.Api.Core.Interfaces;
using Pigmemento.Api.Data;
using Pigmemento.Api.Core.Contants;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IProgressService _progressService;
    private readonly AppDbContext _db;

    public MeController(IProgressService progressService, AppDbContext db)
    {
        _progressService = progressService;
        _db = db;
    }

    // GET /me/progress
    [HttpGet("progress")]
    public async Task<ActionResult<ProgressDto>> GetProgress(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _progressService.GetProgressAsync(userId, ct);
        return Ok(result);
    }

    // GET /me/recent-attempts?limit=5
    [HttpGet("recent-attempts")]
    public async Task<ActionResult<IEnumerable<RecentAttemptDto>>> GetRecentAttempts([FromQuery] int limit = 5, CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var result = await _progressService.GetRecentAttemptsAsync(userId, limit, ct);
        return Ok(result);
    }

    // GET /me/drills-due
    [HttpGet("drills-due")]
    public async Task<ActionResult<DrillsDueDto>> GetDrillsDue(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _progressService.GetDrillsDueAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /me/attempts/today?tzOffsetMinutes=120
    /// tzOffsetMinutes is the user's offset from UTC in minutes (e.g., CET summer is +120).
    /// Defaults to 0 (UTC) if omitted.
    /// </summary>
    [HttpGet("attempts/today")]
    public async Task<ActionResult<AttemptsTodayDto>> GetAttemptsToday([FromQuery] int? tzOffsetMinutes)
    {
        var userId = User.GetUserId();

        // Client-provided offset (e.g., Europe/Copenhagen is typically +120 summer, +60 winter)
        var offset = TimeSpan.FromMinutes(tzOffsetMinutes ?? 0);

        var nowUtc = DateTimeOffset.UtcNow;
        var nowLocal = nowUtc.ToOffset(offset);

        // Start of local day and next local midnight
        var startOfLocalDay = new DateTimeOffset(nowLocal.Date, offset);
        var nextLocalMidnight = startOfLocalDay.AddDays(1);

        // Convert the local boundaries back to UTC for querying
        var startUtc = startOfLocalDay.UtcDateTime;
        var endUtc = nextLocalMidnight.UtcDateTime;

        var used = await _db.Attempts
            .Where(a => a.UserId == userId && a.CreatedAt >= startUtc && a.CreatedAt < endUtc)
            .CountAsync();

        var remaining = Math.Max(0, Limits.DailyLimit - used);

        return Ok(new AttemptsTodayDto
        {
            Limit = Limits.DailyLimit,
            Used = used,
            Remaining = remaining,
            ResetAtLocal = nextLocalMidnight.ToString("o"),
            ResetAtUtc = nextLocalMidnight.UtcDateTime.ToString("o")
        });
    }
}