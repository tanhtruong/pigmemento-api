namespace Pigmemento.Api.Dtos;

public record AnswerRequestDto(
    string ChosenLabel // "benign" | "malignant" (frontend enforces)
);

public record AnswerResponseDto(
    bool Correct,
    string CorrectLabel,
    List<string> TeachingPoints,
    string Disclaimer
);