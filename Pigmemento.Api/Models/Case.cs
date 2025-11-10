namespace Pigmemento.Api.Models;

public class Case
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ImageUrl { get; set; } = default!;

    // Binary label for quiz logic (from diagnosis_1)
    public string Label { get; set; } = default!; // "benign" | "malignant"

    // Optional: store more detailed diagnoses from 1 to 5
    public string PrimaryDiagnosis { get; set; } = default!;                    // diagnosis_1
    public string[] AdditionalDiagnoses { get; set; } = Array.Empty<string>();  // 2–5

    public string Difficulty { get; set; } = "medium";
    public int PatientAge { get; set; }
    public string Site { get; set; } = default!;
    public string ClinicalNote { get; set; } = default!;

    public string SourceId { get; set; } = default!;  // isic_id
    public string Source { get; set; } = "ISIC";

    public List<TeachingPoint> TeachingPoints { get; set; } = new();
}