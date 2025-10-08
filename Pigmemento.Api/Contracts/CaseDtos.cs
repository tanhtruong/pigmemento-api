namespace Pigmemento.Api.Contracts;

public record PatientDto(
    int? Age,
    string? Site,
    string? Sex,
    string? FitzpatrickType
);

public record TeachingPointDto(
    Guid Id,
    Guid CaseId,
    string Points
);

// List item (NO label)
public record CaseListItemDto(
    Guid Id,
    string ImageUrl,
    string Difficulty,
    PatientDto Patient
);

// Detail (includes label + teaching points)
public record CaseDetailDto(
    Guid Id,
    string ImageUrl,
    string Label,        // "benign" | "malignant"
    string Difficulty,   // "easy" | "med" | "hard"
    PatientDto Patient,
    string? Metadata,
    List<TeachingPointDto> TeachingPoints
);

// Optional simple page wrapper (if you want to add cursor later)
public record PageDto<T>(IReadOnlyList<T> Items, string? NextCursor = null);