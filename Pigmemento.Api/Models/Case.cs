namespace Pigmemento.Api.Models;

public class Case
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = default!;
    public string Label { get; set; } = default!;       // 'benign'|'malignant'
    public string Difficulty { get; set; } = "med";     // 'easy'|'med'|'hard'
    public PatientInfo Patient { get; set; } = new();
    public string? Metadata { get; set; }
}

public class PatientInfo {
    public int? Age { get; set; }
    public string? Site { get; set; }
}