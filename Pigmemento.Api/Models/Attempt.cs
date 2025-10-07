namespace Pigmemento.Api.Models;

public class Attempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid CaseId { get; set; }
    public string Answer { get; set; } = default!; // 'benign' | 'malignant'
    public bool Correct { get; set; }
    public int TimeToAnswerMs { get; set; }
    public DateTime CreatedAt { get; set; }
}