using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Dtos;
using Pigmemento.Api.Models;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("cases/{caseId:guid}/teaching-points")]
[Authorize(Roles = "admin")]
public class TeachingPointsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TeachingPointsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<TeachingPointResponseDto>> CreateTeachingPoint(
        Guid caseId,
        [FromBody] TeachingPointRequestDto request)
    {
        if (caseId == Guid.Empty)
            return BadRequest("CaseId is required.");

        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text is required.");

        var exists = await _db.Cases
            .AnyAsync(c => c.Id == caseId);

        if (!exists)
            return NotFound($"Case {caseId} not found.");

        var nextOrder = await _db.TeachingPoints
            .Where(tp => tp.CaseId == caseId)
            .Select(tp => (int?)tp.Order)
            .MaxAsync() ?? 0;

        var teachingPoint = new TeachingPoint
        {
            CaseId = caseId,
            Order = nextOrder + 1,
            Text = request.Text,
        };

        _db.TeachingPoints.Add(teachingPoint);
        await _db.SaveChangesAsync();

        var dto = new TeachingPointResponseDto(teachingPoint.Id, teachingPoint.CaseId, teachingPoint.Text,
            teachingPoint.Order);

        return CreatedAtAction(nameof(GetTeachingPointForCase), new { id = teachingPoint.Id }, dto);
    }

    // Optional: list all teaching points for a case
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeachingPointResponseDto>>> GetTeachingPointsForCase(Guid caseId)
    {
        var exists = await _db.Cases.AnyAsync(c => c.Id == caseId);
        if (!exists)
            return NotFound($"Case {caseId} not found.");

        var items = await _db.TeachingPoints
            .Where(tp => tp.CaseId == caseId)
            .OrderBy(tp => tp.Order)
            .Select(tp => new TeachingPointResponseDto(tp.Id, tp.CaseId, tp.Text, tp.Order))
            .ToListAsync();

        return items;
    }

    // Optional: single teaching point under a case (used by CreatedAtRoute)
    [HttpGet("{id:guid}", Name = "GetTeachingPointForCase")]
    public async Task<ActionResult<TeachingPointResponseDto>> GetTeachingPointForCase(Guid caseId, Guid id)
    {
        var tp = await _db.TeachingPoints
            .FirstOrDefaultAsync(t => t.CaseId == caseId && t.Id == id);

        if (tp == null)
            return NotFound();

        return new TeachingPointResponseDto(tp.Id, tp.CaseId, tp.Text, tp.Order);
    }
}