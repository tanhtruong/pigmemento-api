namespace Pigmemento.Api.Models;

public class TeachingPoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string Points { get; set; } = default!; // bullet-style content

    public Case Case { get; set; } = default!;
}