using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Dtos;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

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

    // GET /me
    [HttpGet]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
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
        var userId = GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
        if (user == null) return NotFound();

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
        var userId = GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return NotFound();

        user.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}