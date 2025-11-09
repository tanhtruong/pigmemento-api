namespace Pigmemento.Api.Dtos;

public record CaseListItemDto(
    Guid Id,
    string ImageUrl,
    string Difficulty,
    int PatientAge,
    string Site
);

// Used when viewing a single case prior to answering.
// NOTE: no Label here (answer is checked via /answer)
public record CaseDetailDto(
    Guid Id,
    string ImageUrl,
    int PatientAge,
    string Site,
    string ClinicalNote
);