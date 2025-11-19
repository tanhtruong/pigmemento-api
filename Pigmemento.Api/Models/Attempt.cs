namespace Pigmemento.Api.Models;

public class Attempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid CaseId { get; set; }
    public Case Case { get; set; } = default!;
    
    public Guid? UserId { get; set; }
    public User User { get; set; } = default!;
    
    public string ChosenLabel { get; set; } = default!;
    public bool Correct { get; set; }
    
    public int TimeToAnswerMs { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}