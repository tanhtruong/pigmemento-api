namespace Pigmemento.Api.Dtos;

public record AttemptResponseDto(
    bool Correct,
    string CorrectLabel,
    List<string> TeachingPoints,
    string Disclaimer,
    int TimeToAnswerMs
);

public record AttemptRequestDto(
    string ChosenLabel, // "benign" | "malignant" (frontend enforces)
    int TimeToAnswerMs
);

public record AttemptSummaryDto(
    bool Correct,
    string ChosenLabel,
    DateTime CreatedAt,
    int TotalAttempts,
    int TimeToAnswerMs
);

public record LatestAttemptDto(
    bool Correct,
    string ChosenLabel,
    string CorrectLabel,
    List<string> TeachingPoints,
    string Disclaimer,
    int TimeToAnswerMs
);