namespace Feirb.Api.Data.Entities;

public class JobSettings
{
    public Guid Id { get; set; }
    public required string JobName { get; set; }
    public string? JobType { get; set; }
    public required string Description { get; set; }
    public required string Cron { get; set; }
    public bool Enabled { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public JobExecutionStatus? LastStatus { get; set; }
    public Guid RowVersion { get; set; } = Guid.NewGuid();
    public List<JobExecution> Executions { get; set; } = [];
}
