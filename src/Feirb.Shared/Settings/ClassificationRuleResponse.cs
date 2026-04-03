namespace Feirb.Shared.Settings;

public record ClassificationRuleResponse(
    Guid Id,
    string Instruction,
    DateTime CreatedAt,
    DateTime UpdatedAt);
