using System.ComponentModel.DataAnnotations;

namespace Pigmemento.Api.Models;

public class WaitlistEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(120)]
    public string Name { get; set; } = default!;

    // Keep this if you want to be able to actually contact people.
    [Required, MaxLength(320)]
    public string EmailNormalized { get; set; } = default!;

    // Unique/dedupe key
    [Required, MaxLength(64)] // hex SHA-256
    public string EmailHash { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}