using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Models;
using Pigmemento.Api.Contracts;
using Pigmemento.Api.Core.Helpers;
using Pigmemento.Api.Core.Contants;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("answers")]
[Authorize] // (flip to [AllowAnonymous] for MVP if needed)
public class AnswersController : ControllerBase
{
    private readonly AppDbContext _db;
    public AnswersController(AppDbContext db) => _db = db;

    // POST /answers
    [HttpPost]
    public async Task<ActionResult<AnswerResponseDto>> Post([FromBody] AnswerCreateDto dto, CancellationToken ct)
    {
        // 1) Validate case exists & get truth label
        var truth = await _db.Cases
            .Where(c => c.Id == dto.CaseId)
            .Select(c => c.Label) // "benign" | "malignant"
            .FirstOrDefaultAsync(ct);

        if (truth is null)
            return NotFound("Case not found.");

        // 2) Get userId from JWT (or forbid)
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var today = DateTime.UtcNow.Date;

        // Count today's attempts
        var countToday = await _db.Attempts
            .Where(a => a.UserId == userId && a.CreatedAt >= today)
            .CountAsync();

        if (countToday >= 10)
        {
            return BadRequest(new { error = $"Daily limit of {Limits.DailyLimit} {(Limits.DailyLimit == 1 ? "attempt" : "attempts")} reached." });
        }

        // 3) Normalize input & compute correctness
        var answerNorm = dto.Answer?.Trim().ToLowerInvariant();
        if (answerNorm is not ("benign" or "malignant"))
            return BadRequest("Answer must be 'benign' or 'malignant'.");

        var correct = string.Equals(truth, answerNorm, StringComparison.OrdinalIgnoreCase);

        // 4) Persist attempt (CreatedAt stored as UTC)
        var attempt = new Attempt
        {
            UserId = userId,
            CaseId = dto.CaseId,
            Answer = answerNorm!,
            Correct = correct,
            TimeToAnswerMs = Math.Max(0, dto.TimeToAnswerMs),
            CreatedAt = DateTime.UtcNow
        };

        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync(ct);

        // 5) Upsert UserCaseStats (keeps drills fast)
        //    Simple spacing schedule by correct streak:
        //    incorrect -> 0 days (due now)
        //    streak 1 -> 1 day, 2 -> 4 days, 3 -> 7 days, >=4 -> 14 days
        var stats = await _db.UserCaseStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.CaseId == dto.CaseId, ct);

        if (stats is null)
        {
            var streak = correct ? 1 : 0;
            var days = correct ? 1 : 0;

            stats = new UserCaseStats
            {
                UserId = userId,
                CaseId = dto.CaseId,
                CorrectStreak = streak,
                LastAttemptAt = attempt.CreatedAt,
                NextDueAt = attempt.CreatedAt.AddDays(days)
            };
            _db.UserCaseStats.Add(stats);
        }
        else
        {
            stats.CorrectStreak = correct ? stats.CorrectStreak + 1 : 0;
            stats.LastAttemptAt = attempt.CreatedAt;

            int days = correct switch
            {
                false => 0,
                true when stats.CorrectStreak >= 4 => 14,
                true when stats.CorrectStreak == 3 => 7,
                true when stats.CorrectStreak == 2 => 4,
                _ => 1
            };

            stats.NextDueAt = attempt.CreatedAt.AddDays(days);
            // no need to _db.Update(stats); tracked entity is modified
        }

        await _db.SaveChangesAsync(ct);

        // 6) Return minimal feedback payload
        var response = new AnswerListItemDto(
            attempt.Id,
            attempt.CaseId,
            truth,
            attempt.Correct,
            attempt.TimeToAnswerMs,
            attempt.CreatedAt
        );

        return Ok(response);
    }

    // GET /answers/my-recent?limit=50
    [HttpGet("my-recent")]
    public async Task<ActionResult<PageDto<AnswerListItemDto>>> GetMyRecent(
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null
    )
    {
        // auth
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        limit = Math.Clamp(limit, 1, 200);

        // decode cursor (CreatedAt ticks + Id), for keyset pagination
        (DateTime? afterCreatedAt, Guid? afterId) = CursorHelper.TryDecodeCursor(cursor);

        // base query
        var q = _db.Attempts
            .Where(a => a.UserId == userId);

        // apply keyset window if cursor provided
        if (afterCreatedAt is not null && afterId is not null)
        {
            // We are ordering DESC by CreatedAt then DESC by Id.
            // To fetch the "next page", we need records *older* than the cursor:
            // (CreatedAt < cursorCreatedAt) OR (CreatedAt == cursorCreatedAt AND Id < cursorId)
            var cAt = afterCreatedAt.Value;
            var cId = afterId.Value;
            q = q.Where(a => a.CreatedAt < cAt || (a.CreatedAt == cAt && a.Id.CompareTo(cId) < 0));
        }

        // fetch limit+1 to know if there’s a next page
        var rows = await q
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Take(limit + 1)
            .Select(a => new AnswerListItemDto(
                a.Id,
                a.CaseId,
                a.Answer,
                a.Correct,
                a.TimeToAnswerMs,
                a.CreatedAt
            ))
            .ToListAsync();

        // build page
        string? nextCursor = null;
        if (rows.Count > limit)
        {
            var last = rows[limit - 1];      // last item that will be returned
            nextCursor = CursorHelper.EncodeCursor(last.CreatedAt, last.Id);
            rows = rows.Take(limit).ToList(); // trim the extra
        }

        return Ok(new PageDto<AnswerListItemDto>(rows, nextCursor));
    }
}