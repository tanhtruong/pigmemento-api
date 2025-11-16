namespace Pigmemento.Api.Dtos;

public record AttemptSummaryDto(
    bool Correct,
    string ChosenLabel,
    DateTime CreatedAt,
    int TotalAttempts
);

public record LatestAttemptDto(
    bool Correct,
    string ChosenLabel,
    string CorrectLabel,
    List<string> TeachingPoints,
    string Disclaimer
);