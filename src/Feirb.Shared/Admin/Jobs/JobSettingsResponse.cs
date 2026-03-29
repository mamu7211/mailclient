namespace Feirb.Shared.Admin.Jobs;

public record JobSettingsResponse(
    Guid Id,
    string JobName,
    string Description,
    string Cron,
    bool Enabled,
    DateTimeOffset? LastRunAt,
    string? LastStatus,
    Guid RowVersion,
    List<JobExecutionResponse> RecentExecutions);
