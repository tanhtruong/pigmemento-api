using Pigmemento.Api.Models;

namespace Pigmemento.Api.Data.Seed;

public static class CaseCsvMapper
{
    public static Case FromDynamic(dynamic row)
    {
        // All CSV values are effectively strings here
        string sourceId = (string?)row.isic_id ?? throw new InvalidOperationException("Missing isic_id");

        var d1 = ((string?)row.diagnosis_1)?.Trim() ?? "";
        var d2 = ((string?)row.diagnosis_2)?.Trim();
        var d3 = ((string?)row.diagnosis_3)?.Trim();
        var d4 = ((string?)row.diagnosis_4)?.Trim();
        var d5 = ((string?)row.diagnosis_5)?.Trim();

        var label = d1.Equals("Malignant", StringComparison.OrdinalIgnoreCase)
            ? "malignant"
            : "benign";

        var additional = new[] { d2, d3, d4, d5 }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        // age_approx is a string → parse carefully
        int patientAge = 0;
        var rawAge = (string?)row.age_approx;
        if (!string.IsNullOrWhiteSpace(rawAge) && int.TryParse(rawAge, out var parsedAge))
        {
            patientAge = parsedAge;
        }

        // sex is string
        var rawSex = ((string?)row.sex)?.Trim().ToLowerInvariant();
        var sex = rawSex switch
        {
            "male" => "male",
            "female" => "female",
            _ => "patient"
        };

        // site from anatom_site_general (string)
        var site = ((string?)row.anatom_site_general)?.Trim();
        if (string.IsNullOrWhiteSpace(site))
        {
            site = "skin";
        }

        var imageUrl = $"https://cdn.pigmemento.app/cases/{sourceId}.jpg";
        var clinicalNote = BuildClinicalNote(patientAge, sex, site);

        return new Case
        {
            SourceId = sourceId,
            Source = "ISIC",
            ImageUrl = imageUrl,

            Label = label,
            PrimaryDiagnosis = d1,
            AdditionalDiagnoses = additional,

            Difficulty = "medium",
            PatientAge = patientAge,
            Site = site,
            ClinicalNote = clinicalNote
        };
    }

    private static string BuildClinicalNote(int age, string sex, string site)
    {
        // Keep it simple & synthetic
        if (age > 0)
            return $"{age}-year-old {sex} with a lesion on the {site}.";

        return $"Patient with a lesion on the {site}.";
    }
}