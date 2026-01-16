namespace Pigmemento.Api.Dtos;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record UpdateUserDto(
    string? Name
);

public record TrainingStatsDto(
    int TotalAttempts,
    int UniqueCasesAttempted,
    
    double? Accuracy,
    double? Sensitivity, // melanoma:    P(pred malignant | true malignant)
    double? Specificity, // benign:      P(pred benign | true benign)
    
    // Optional extras for UI later
    DateTime? FirstAttemptAt,
    DateTime? LastAttemptAt
);