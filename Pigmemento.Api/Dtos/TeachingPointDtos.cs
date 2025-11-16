namespace Pigmemento.Api.Dtos;

public record TeachingPointRequestDto(
    string Text
);

public record TeachingPointResponseDto(
    Guid Id,
    Guid CaseId,
    string Text,
    int Order
);