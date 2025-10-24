using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Pigmemento.Api.Auth.Core;
using Pigmemento.Api.Models;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("cases")]
[Authorize]

public class CasesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CasesController(AppDbContext db) => _db = db;

    /// <summary>
    /// List training cases (no ground truth). Filter by difficulty. Default limit=20.
    /// GET /cases?limit=20&difficulty=med
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PageDto<CaseListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PageDto<CaseListItemDto>>> GetCases(
        [FromQuery] int limit = 20,
        [FromQuery] string? difficulty = null,
        CancellationToken ct = default)
    {
        // Clamp limit to sane bounds
        limit = Math.Clamp(limit, 1, 100);

        var userId = User.GetUserId();
        var now = DateTime.UtcNow;
        var cooldownCutoff = now.AddMinutes(-5);
        var recentlyWrongCutoff = now.AddDays(-7);

        // Base cases (with optional difficulty filter)
        IQueryable<Case> baseCases = _db.Cases.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            var d = difficulty.Trim().ToLowerInvariant();
            if (d is not ("easy" or "med" or "hard"))
                return BadRequest(new { message = "Invalid difficulty. Use 'easy' | 'med' | 'hard'." });

            baseCases = baseCases.Where(c => c.Difficulty == d);
        }

        // Stats for current user
        var statsForUser = _db.UserCaseStats.AsNoTracking().Where(s => s.UserId == userId);

        // 1) Due or Unseen (priority)
        var dueOrUnseenQuery =
            from c in baseCases
            join s in statsForUser on c.Id equals s.CaseId into gj
            from s in gj.DefaultIfEmpty()
            where
                // unseen OR due now
                (s == null) || (s.NextDueAt <= now)
            where
                // respect cooldown
                (s == null) || (s.LastSeenAt == null) || (s.LastSeenAt <= cooldownCutoff)
            select new
            {
                Case = c,
                IsUnseen = (s == null),
                IsDue = (s == null) || (s.NextDueAt <= now),
                RecentlyWrong = (s != null && s.RecentlyWrongAt != null && s.RecentlyWrongAt >= recentlyWrongCutoff),
                // For ordering
                NextDue = s != null ? s.NextDueAt : DateTime.MinValue
            };

        // Order: recently-wrong first, then due/unseen, then nearest due first, then a stable tiebreaker
        var dueOrUnseenOrdered = dueOrUnseenQuery
            .OrderByDescending(x => x.RecentlyWrong)        // boost “recently wrong”
            .ThenByDescending(x => x.IsDue)                 // due (incl. unseen) before anything else
            .ThenBy(x => x.NextDue)                         // earlier due first
            .ThenByDescending(x => x.Case.Id);              // stable tiebreaker (replace with CreatedAt if available)

        var dueOrUnseen = await dueOrUnseenOrdered
            .Select(x => x.Case.Id)
            .Take(limit)
            .ToListAsync(ct);

        // 2) Top-up with exploration if needed (not-yet-due + cooldown respected)
        List<Guid> finalIds = new(dueOrUnseen);
        if (finalIds.Count < limit)
        {
            var needed = limit - finalIds.Count;

            var explorationQuery =
                from c in baseCases
                join s in statsForUser on c.Id equals s.CaseId
                where s.NextDueAt > now                         // not yet due
                where s.LastSeenAt == null || s.LastSeenAt <= cooldownCutoff
                where !finalIds.Contains(c.Id)                  // avoid duplicates
                select new
                {
                    Case = c,
                    NextDue = s.NextDueAt
                };

            var explorationOrdered = explorationQuery
                .OrderBy(x => x.NextDue)                        // closest-to-due first
                .ThenByDescending(x => x.Case.Id);              // stable tiebreaker

            var explorationIds = await explorationOrdered
                .Select(x => x.Case.Id)
                .Take(needed)
                .ToListAsync(ct);

            finalIds.AddRange(explorationIds);
        }

        // Materialize DTOs in the same order as finalIds
        // (IN (...) loses order; re-order on client side)
        var casesMap = await _db.Cases
            .AsNoTracking()
            .Where(c => finalIds.Contains(c.Id))
            .ToListItemDto()
            .ToDictionaryAsync(c => c.Id, ct);

        var orderedDtos = finalIds
            .Where(id => casesMap.ContainsKey(id))
            .Select(id => casesMap[id])
            .ToList();

        return Ok(new PageDto<CaseListItemDto>(orderedDtos));
    }

    /// <summary>
    /// Get a single case including truth (label) and teaching points.
    /// GET /cases/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CaseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaseDetailDto>> GetCase(Guid id)
    {
        // Use projection to avoid split queries / lazy loading
        var dto = await _db.Cases
            .AsNoTracking()
            .Where(c => c.Id == id)
            .ToDetailDto()
            .FirstOrDefaultAsync();

        if (dto is null) return NotFound();
        return Ok(dto);
    }
}