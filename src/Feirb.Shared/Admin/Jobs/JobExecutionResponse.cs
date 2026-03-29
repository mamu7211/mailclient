namespace Feirb.Shared.Admin.Jobs;

public record JobExecutionResponse(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string Status,
    string? Error);
