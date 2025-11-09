namespace Pigmemento.Api.Models;

public class TeachingPoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public Case Case { get; set; } = default!;
    public string Text { get; set; } = default!;
}