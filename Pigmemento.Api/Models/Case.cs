namespace Pigmemento.Api.Models;

public class Case
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = default!;
    public string Label { get; set; } = "unknown"; // "benign" | "malignant" | "unknown"
    public string Difficulty { get; set; } = "med"!; // "easy" | "med" | "hard"
    public string? Metadata { get; set; }

    public Patient Patient { get; set; } = new();


    public ICollection<TeachingPoint> TeachingPoints { get; set; } = [];
    public ICollection<Attempt> Attempts { get; set; } = [];
}