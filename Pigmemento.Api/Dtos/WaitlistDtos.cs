using System.ComponentModel.DataAnnotations;

namespace Pigmemento.Api.Dtos;

public record WaitlistSignupDto(
    [Required, MaxLength(120)] string Name,
    [Required, EmailAddress, MaxLength(320)] string Email
);

public record WaitlistSignupResponseDto(
    bool Ok,
    bool AlreadySignedUp
);