namespace Pigmemento.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string Role { get; set; } = "user"; // "user", "admin", "educator", etc.
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginUtc { get; set; }
}
