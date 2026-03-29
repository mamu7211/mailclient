namespace Feirb.Api.Data.Entities;

public class JobExecution
{
    public Guid Id { get; set; }
    public Guid JobSettingsId { get; set; }
    public JobSettings JobSettings { get; set; } = null!;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public JobExecutionStatus Status { get; set; }
    public string? Error { get; set; }
}
