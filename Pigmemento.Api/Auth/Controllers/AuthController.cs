using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pigmemento.Api.Auth.Contracts;
using Pigmemento.Api.Auth.Core;

namespace Pigmemento.Api.Auth.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        try
        {
            var resp = await _auth.RegisterAsync(request, ct);
            return Ok(resp);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var resp = await _auth.LoginAsync(request, ct);
            return Ok(resp);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // Handy endpoint to test tokens
    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        // minimal identity echo
        return Ok(new
        {
            userId = User.FindFirst("sub")?.Value ?? User.Identity?.Name,
            email = User.FindFirst("email")?.Value,
            name = User.Identity?.Name
        });
    }
}