namespace Pigmemento.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string Role { get; set; } = "user";
    
    // --- Lifecycle & audit fields ---
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }      // updated on successful login
    public DateTime? DeletedAt { get; set; }        // null = active


    public List<Attempt> Attempts { get; set; } = new();
}