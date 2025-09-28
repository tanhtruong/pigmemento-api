using System.ComponentModel.DataAnnotations;

namespace Pigmemento.Api.Contracts;

public class WaitlistCreate
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = default!;
}