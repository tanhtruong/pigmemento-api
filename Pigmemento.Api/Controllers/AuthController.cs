using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Dtos;
using Pigmemento.Api.Models;
using Pigmemento.Api.Services;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(
        AppDbContext db, 
        JwtTokenService jwt, 
        IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _jwt = jwt;
        _passwordHasher = passwordHasher;
    }
    
    // POST /auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto request)
    {
        var normalizedEmail = request.Email.TrimEnd().ToLowerInvariant();

        var exists = await _db.Users
            .AnyAsync(u => u.Email == normalizedEmail);

        if (exists)
            return Conflict("A user with this email already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = normalizedEmail,
            Role = "user"
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponseDto(token));
    }
    
    // POST /auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null)
            return Unauthorized("Invalid credentials.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid credentials.");
        
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponseDto(token));
    }
}