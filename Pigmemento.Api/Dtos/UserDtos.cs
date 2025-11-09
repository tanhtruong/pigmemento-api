namespace Pigmemento.Api.Dtos;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record UpdateUserDto(
    string? Name
);