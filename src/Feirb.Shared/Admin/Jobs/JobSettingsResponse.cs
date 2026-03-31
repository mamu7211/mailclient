namespace Feirb.Shared.Admin.Jobs;

public record JobSettingsResponse(
    Guid Id,
    string JobName,
    string? JobType,
    string Description,
    string Cron,
    bool Enabled,
    DateTimeOffset? LastRunAt,
    string? LastStatus,
    Guid? ResourceId,
    string? ResourceType,
    Guid RowVersion,
    List<JobExecutionResponse> RecentExecutions);
