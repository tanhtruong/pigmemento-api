using System.ComponentModel.DataAnnotations;

namespace Pigmemento.Api.Auth.Contracts;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [MaxLength(128)] string? Name
);

public record LoginRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password
);

public record AuthResponse(
    Guid UserId,
    string Email,
    string? Name,
    string AccessToken,
    DateTime ExpiresUtc
);

// public record AuthResponse(
//     Guid UserId,
//     string Email,
//     string? Name,
//     string AccessToken,
//     DateTime AccessTokenExpiresAt,
//     string RefreshToken,
//     DateTime RefreshTokenExpiresAt);

// public record RefreshRequest(string RefreshToken);