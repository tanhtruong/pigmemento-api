namespace Pigmemento.Api.Models;

public class Case
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ImageUrl { get; set; } = default!;
    public string Label { get; set; } = default!;

    public string Difficulty { get; set; } = "medium";
    public int PatientAge { get; set; }
    public string Site { get; set; } = default!;
    public string ClinicalNote { get; set; } = default!;

    public List<TeachingPoint> TeachingPoints { get; set; } = new();
}