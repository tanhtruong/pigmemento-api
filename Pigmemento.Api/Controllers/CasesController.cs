using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Contracts;
using Microsoft.AspNetCore.Authorization;

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
        [FromQuery] string? difficulty = null
    )
    {
        // Clamp limit to sane bounds
        limit = Math.Clamp(limit, 1, 100);

        var q = _db.Cases.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            // Accept "easy" | "med" | "hard" case-insensitively
            var d = difficulty.Trim().ToLowerInvariant();
            if (d is not ("easy" or "med" or "hard"))
                return BadRequest(new { message = "Invalid difficulty. Use 'easy' | 'med' | 'hard'." });

            q = q.Where(c => c.Difficulty == d);
        }

        // Simple deterministic order: newest first (by Id as Guid isn't perfect but OK for demo).
        // If you add CreatedAt, order by that.
        q = q.OrderByDescending(c => c.Id);

        var items = await q
            .ToListItemDto()
            .Take(limit)
            .ToListAsync();

        return Ok(new PageDto<CaseListItemDto>(items));
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