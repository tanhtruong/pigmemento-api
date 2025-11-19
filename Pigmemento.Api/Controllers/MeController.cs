using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Dtos;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Pigmemento.Api.Auth;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly AppDbContext _db;

    public MeController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Basic profile info for the authenticated user.
    /// </summary>
    // GET /me
    [HttpGet]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

        if (user is null)
            return NotFound();

        return Ok(new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.CreatedAt,
            user.LastLoginAt
        ));
    }

    // PATCH /me
    [HttpPatch]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateUserDto dto)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

        if (user == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Name))
            user.Name = dto.Name.Trim();

        await _db.SaveChangesAsync();

        return Ok(new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.CreatedAt,
            user.LastLoginAt
        ));
    }

    // DELETE /me
    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound();

        // Scrub PII on user
        user.Name = null;
        user.Email = null;
        user.PasswordHash = null;
        user.Role = null;
        user.DeletedAt = DateTime.UtcNow;

        // Anonymize attempts: keep behavior, drop identity
        var attempts = await _db.Attempts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        foreach (var attempt in attempts)
        {
            attempt.UserId = null;
        }
        
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Aggregated training stats for the authenticated user.
    /// Educational only — not for diagnosis.
    /// </summary>
    [HttpGet("progress")]
    public async Task<ActionResult<TrainingStatsDto>> GetProgress()
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var attemptsQuery = _db.Attempts
            .AsNoTracking()
            .Include(a => a.Case)
            .Where(a => a.UserId == userId);

        var attempts = await attemptsQuery.ToListAsync();

        if (!attempts.Any())
        {
            return Ok(new TrainingStatsDto(
                0,
                0,
                null,
                null,
                null,
                null,
                null)
            );
        }

        var totalAttempts = attempts.Count();
        var uniqueAttempts = attempts.Select(a => a.CaseId).Distinct().Count();
        var correct = attempts.Count(a => a.Correct);

        double? accuracy = totalAttempts > 0
            ? (double)correct / totalAttempts
            : null;

        // Sensitivity: correctly identified melanoma / all melanoma cases attempted
        var malignantAttempts = attempts.Where(a => a.Case.Label == "malignant").ToList();
        var malignantTotal = malignantAttempts.Count;
        var malignantCorrect = malignantAttempts.Count(a => a.Correct);

        double? sensitivity = malignantTotal > 0
            ? (double)malignantCorrect / malignantTotal
            : null;

        // Specificity: correctly identified benign / all benign cases attempted
        var benignAttempts = attempts.Where(a => a.Case.Label == "benign").ToList();
        var benignTotal = benignAttempts.Count;
        var benignCorrect = benignAttempts.Count(a => a.Correct);

        double? specificity = benignTotal > 0
            ? (double)benignCorrect / benignTotal
            : null;

        var firstAttemptAt = attempts.Min(a => a.CreatedAt);
        var lastAttemptAt = attempts.Max(a => a.CreatedAt);

        var dto = new TrainingStatsDto
        (
            totalAttempts,
            uniqueAttempts,
            accuracy,
            sensitivity,
            specificity,
            firstAttemptAt,
            lastAttemptAt
        );

        return Ok(dto);
    }
}