using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Models;
using Pigmemento.Api.Contracts;
using Pigmemento.Api.Core.Helpers;
using Pigmemento.Api.Core.Contants;
using Pigmemento.Api.Core.Interfaces;
using Pigmemento.Api.Auth.Core;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("answers")]
[Authorize] // (flip to [AllowAnonymous] for MVP if needed)
public class AnswersController : ControllerBase
{
    private readonly ISpacedRepetitionService _spacedRepService;
    private readonly AppDbContext _db;

    public AnswersController(AppDbContext db, ISpacedRepetitionService spacedRepService)
    {
        _db = db;
        _spacedRepService = spacedRepService;
    }

    // POST /answers
    [HttpPost]
    public async Task<ActionResult<AnswerResponseDto>> Post([FromBody] AnswerCreateDto dto, CancellationToken ct)
    {
        var role = User.GetUserRole();
        if (role is null) return Forbid();

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
            .CountAsync(ct);

        if (countToday >= Limits.DailyLimit && role is not "admin")
        {
            var noun = Limits.DailyLimit == 1 ? "attempt" : "attempts";
            return BadRequest(new { error = $"Daily limit of {Limits.DailyLimit} {noun} reached." });
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

        // 5) Spaced repetition update (delegate to service)  <<<<<<<<<<
        await _spacedRepService.UpdateUserCaseStatsAsync(
            userId: userId,
            caseId: dto.CaseId,
            correct: correct,
            latencyMs: attempt.TimeToAnswerMs,
            ct: ct
        );

        // 6) Return minimal feedback payload (truth is OK here because this is the feedback flow)
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