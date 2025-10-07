namespace Pigmemento.Api.Models;
public class TeachingPoint {
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public string Points { get; set; } = default!; // store bullet text or JSON
}