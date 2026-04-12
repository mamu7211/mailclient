namespace Feirb.Api.Data.Entities;

public class JobExecutionLog
{
    public Guid Id { get; set; }
    public Guid JobExecutionId { get; set; }
    public JobExecution JobExecution { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public JobExecutionLogLevel Level { get; set; }
    public required string Message { get; set; }
    public string? Metadata { get; set; }
}
