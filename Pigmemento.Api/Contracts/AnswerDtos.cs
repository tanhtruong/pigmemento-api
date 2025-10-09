namespace Pigmemento.Api.Contracts;

public record AnswerCreateDto(Guid CaseId, string Answer, int TimeToAnswerMs);

public record AnswerResponseDto(
    Guid AttemptId,
    bool Correct,
    string TruthLabel,
    int TimeToAnswerMs,
    DateTime CreatedAt // UTC
);

public record AnswerListItemDto(
    Guid Id,
    Guid CaseId,
    string Answer,
    bool Correct,
    int TimeToAnswerMs,
    DateTime CreatedAt // UTC
);