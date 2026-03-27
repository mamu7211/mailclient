namespace Feirb.Shared.Settings;

public record LabelResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);
