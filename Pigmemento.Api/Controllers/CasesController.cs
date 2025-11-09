using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Pigmemento.Api.Data;
using Pigmemento.Api.Dtos;
using Pigmemento.Api.Models;
using System.Security.Claims;

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
    [AllowAnonymous] // Allows browsing
    public async Task<ActionResult<IEnumerable<CaseListItemDto>>> GetCases(
        [FromQuery] string? difficulty = null,
        [FromQuery] int limit = 50)
    {
        var query = _db.Cases.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(difficulty))
            query = query.Where(c => c.Difficulty == difficulty);

        var items = await query
            .OrderBy(c => c.Id)
            .Take(limit)
            .Select(c => new CaseListItemDto(
                c.Id,
                c.ImageUrl,
                c.Difficulty,
                c.PatientAge,
                c.Site
            ))
            .ToListAsync();

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
    public async Task<ActionResult<AnswerResponseDto>> AnswerCase(
        Guid id,
        [FromBody] AnswerRequestDto request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == Guid.Empty)
            return Unauthorized();

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
            UserId = userId,
            ChosenLabel = request.ChosenLabel,
            Correct = correct,
            CreatedAt = DateTime.UtcNow
        };

        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync();

        var response = new AnswerResponseDto(
            correct,
            c.Label,
            c.TeachingPoints
                .OrderBy(tp => tp.Id)
                .Select(tp => tp.Text)
                .ToList(),
            DisclaimerText
        );
        
        return Ok(response);
    }
    
    private Guid GetUserIdFromClaims()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

}