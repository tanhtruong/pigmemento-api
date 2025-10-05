using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Auth.Contracts;
using Pigmemento.Api.Auth.Jwt;
using Pigmemento.Api.Models;
using Pigmemento.Api.Data;

namespace Pigmemento.Api.Auth.Core;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IJwtTokenService _jwt;

    public AuthService(AppDbContext db, IPasswordHasher<User> hasher, IJwtTokenService jwt)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists) throw new InvalidOperationException("Email is already registered.");

        var user = new User
        {
            Email = email,
            Name = request.Name
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var (token, exp) = _jwt.CreateAccessToken(user);
        return new AuthResponse(user.Id, user.Email, user.Name, token, exp);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);

        // Avoid user enumeration timing differences
        if (user is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var (token, exp) = _jwt.CreateAccessToken(user);
        return new AuthResponse(user.Id, user.Email, user.Name, token, exp);
    }
}