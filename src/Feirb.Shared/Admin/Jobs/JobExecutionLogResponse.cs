namespace Feirb.Shared.Admin.Jobs;

public record JobExecutionLogResponse(
    Guid Id,
    DateTimeOffset Timestamp,
    string Level,
    string Message);
