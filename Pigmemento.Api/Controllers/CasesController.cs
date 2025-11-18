using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Pigmemento.Api.Data;
using Pigmemento.Api.Dtos;
using Pigmemento.Api.Models;
using System.Security.Claims;
using Pigmemento.Api.Auth;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CasesController : ControllerBase
{
    private readonly AppDbContext _db;

    private const string DisclaimerText =
        "Educational use only - not for diagnosis or patient management";

    public CasesController(AppDbContext db)
    {
        _db = db;
    }

    // GET /cases
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CaseListItemDto>>> GetCases(
        [FromQuery] string? difficulty = null,
        [FromQuery] int limit = 50)
    {
        var userId = User.GetUserId();

        IQueryable<Case> baseQuery = _db.Cases.AsNoTracking();

        // Base query for cases (difficulty filter etc.)
        if (!string.IsNullOrWhiteSpace(difficulty))
            baseQuery = baseQuery.Where(c => c.Difficulty == difficulty);

        // If not logged in → just return list as before
        if (userId is null)
        {
            var anonItems = await baseQuery
                .OrderBy(c => c.Id)
                .Take(limit)
                .Select(c => new CaseListItemDto(
                    c.Id,
                    c.ImageUrl,
                    c.Difficulty,
                    c.PatientAge,
                    c.Site,
                    null
                ))
                .ToListAsync();

            return Ok(anonItems);
        }

        // 1) Logged-in: first try UNATTEMPTED cases
        var unAttemptedQuery = baseQuery
            .Where(c => !_db.Attempts
                .Any(a => a.UserId == userId && a.CaseId == c.Id));

        var items = await unAttemptedQuery
            .OrderBy(c => c.Id)
            .Take(limit)
            .Select(c => new CaseListItemDto(
                c.Id,
                c.ImageUrl,
                c.Difficulty,
                c.PatientAge,
                c.Site,
                null
            ))
            .ToListAsync();

        // 2) If none left → RECYCLE: cases the user *has* attempted,
        // ordered by OLDEST last attempt (least recently seen first).
        if (items.Count == 0)
        {
            var attemptedCases = _db.Attempts
                .Where(a => a.UserId == userId)
                .GroupBy(a => a.CaseId)
                .Select(g => new
                {
                    CaseId = g.Key,
                    LastAttemptAt = g.Max(a => a.CreatedAt),
                });

            var recycledQuery =
                from c in baseQuery // still respects difficulty filter
                join ac in attemptedCases on c.Id equals ac.CaseId
                orderby ac.LastAttemptAt ascending
                select new CaseListItemDto(
                    c.Id,
                    c.ImageUrl,
                    c.Difficulty,
                    c.PatientAge,
                    c.Site,
                    null
                );

            items = await recycledQuery
                .Take(limit)
                .ToListAsync();
        }

        return Ok(items);
    }

    // GET /cases/{id}
    // Returns case detail WITHOUT the correct label.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CaseDetailDto>> GetCaseDetail(Guid id)
    {
        var c = await _db.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c is null)
            return NotFound();

        var dto = new CaseDetailDto(
            c.Id,
            c.ImageUrl,
            c.PatientAge,
            c.Site,
            c.ClinicalNote
        );

        return Ok(dto);
    }

    // POST /cases/{id}/answer
    [HttpPost("{id:guid}/answer")]
    public async Task<ActionResult<AttemptResponseDto>> AnswerCase(
        Guid id,
        [FromBody] AttemptRequestDto request)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var currentUserId = userId.Value;

        var c = await _db.Cases
            .Include(x => x.TeachingPoints)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c is null)
            return NotFound();

        var correct = string.Equals(
            request.ChosenLabel,
            c.Label,
            StringComparison.OrdinalIgnoreCase
        );

        var attempt = new Attempt
        {
            Id = Guid.NewGuid(),
            CaseId = c.Id,
            UserId = currentUserId,
            ChosenLabel = request.ChosenLabel,
            Correct = correct,
            CreatedAt = DateTime.UtcNow,
            TimeToAnswerMs = request.TimeToAnswerMs,
        };

        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync();

        var response = new AttemptResponseDto(
            correct,
            c.Label,
            c.TeachingPoints
                .OrderBy(tp => tp.Id)
                .Select(tp => tp.Text)
                .ToList(),
            DisclaimerText,
            request.TimeToAnswerMs
        );

        return Ok(response);
    }

    [HttpGet("random")]
    [Authorize]
    public async Task<ActionResult<CaseDetailDto>> GetRandomUnseenCase(CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        // 1) Random UNATTEMPTED case
        var randomUnseenQuery =
            from c in _db.Cases.AsNoTracking()
            where !_db.Attempts.Any(a => a.UserId == userId && a.CaseId == c.Id)
            orderby EF.Functions.Random()
            select new CaseDetailDto(
                c.Id,
                c.ImageUrl,
                c.PatientAge,
                c.Difficulty,
                c.Site
            );

        var randomUnseen = await randomUnseenQuery.FirstOrDefaultAsync(ct);

        if (randomUnseen is not null)
            return Ok(randomUnseen);

        // 2) If no unseen left -> random Attempted case (for this user)
        var randomAttemptedQuery =
            from c in _db.Cases.AsNoTracking()
            where _db.Attempts.Any(a => a.UserId == userId && a.CaseId == c.Id)
            orderby EF.Functions.Random()
            select new CaseDetailDto(
                c.Id,
                c.ImageUrl,
                c.PatientAge,
                c.Difficulty,
                c.Site
            );

        var randomAttempted = await randomAttemptedQuery.FirstOrDefaultAsync(ct);

        if (randomAttempted is null)
            return NotFound(new { error = "No cases available." });

        return Ok(randomAttempted);
    }

    [HttpGet("attempted")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CaseListItemDto>>> GetAttemptedCases(CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var query =
            from c in _db.Cases.AsNoTracking()
            join a in _db.Attempts.AsNoTracking().Where(a => a.UserId == userId)
                on c.Id equals a.CaseId into attemptGroup
            where attemptGroup.Any()
            let lastAttempt = attemptGroup
                .OrderByDescending(a => a.CreatedAt).FirstOrDefault()
            orderby lastAttempt.CreatedAt descending
            select new CaseListItemDto(
                c.Id,
                c.ImageUrl,
                c.Difficulty,
                c.PatientAge,
                c.Site,
                new AttemptSummaryDto(
                    lastAttempt.Correct,
                    lastAttempt.ChosenLabel,
                    lastAttempt.CreatedAt,
                    attemptGroup.Count(),
                    lastAttempt.TimeToAnswerMs
                )
            );
        var items = await query.ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}/attempts/latest")]
    [Authorize]
    public async Task<ActionResult<LatestAttemptDto>> GetLatestAttemptForCase(
        Guid id,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        // Make sure case exists & load teaching points + label
        var c = await _db.Cases
            .Include(x => x.TeachingPoints)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (c is null)
            return NotFound(new { error = "Case not found." });

        // Find latest attempt for this user & case
        var latestAttempt = await _db.Attempts
            .AsNoTracking()
            .Where(a => a.UserId == userId.Value && a.CaseId == id)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (latestAttempt is null)
            return NotFound(new { error = "No attempts for this case." });

        var dto = new LatestAttemptDto(
            Correct: latestAttempt.Correct,
            ChosenLabel: latestAttempt.ChosenLabel,
            CorrectLabel: c.Label,
            TeachingPoints: c.TeachingPoints
                .OrderBy(tp => tp.Id)
                .Select(tp => tp.Text)
                .ToList(),
            Disclaimer: DisclaimerText, // same constant you use in AnswerCase
            latestAttempt.TimeToAnswerMs
        );

        return Ok(dto);
    }
    
    
}