namespace Pigmemento.Api.Dtos;

public record RegisterRequestDto(
    string Email,
    string Name,
    string Password
);

public record LoginRequestDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    string Token
);