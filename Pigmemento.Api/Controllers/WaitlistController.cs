using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Dtos;
using Pigmemento.Api.Models;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("waitlist")]
public class WaitlistController : ControllerBase
{
    private readonly AppDbContext _db;

    public WaitlistController(AppDbContext db) => _db = db;

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<WaitlistSignupResponseDto>> Signup([FromBody] WaitlistSignupDto dto)
    {
        var name = dto.Name.Trim();
        var emailNorm = NormalizeEmail(dto.Email);
        var emailHash = Sha256Hex(emailNorm);

        var exists = await _db.WaitlistEntries.AnyAsync(x => x.EmailHash == emailHash);
        if (exists)
            return Ok(new WaitlistSignupResponseDto(true, true));

        _db.WaitlistEntries.Add(new WaitlistEntry
        {
            Name = name,
            EmailNormalized = emailNorm,
            EmailHash = emailHash,
            CreatedAtUtc = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // race on unique index
            return Ok(new WaitlistSignupResponseDto(true, true));
        }

        return Ok(new WaitlistSignupResponseDto(true, false));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}