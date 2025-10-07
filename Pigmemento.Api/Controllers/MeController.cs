using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pigmemento.Api.Auth.Core;
using Pigmemento.Api.Contracts;
using Pigmemento.Api.Core.Interfaces;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IProgressService _progressService;

    public MeController(IProgressService progressService)
    {
        _progressService = progressService;
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
}