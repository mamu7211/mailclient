using Feirb.Api.Data.Entities;

namespace Feirb.Api.Services;

public record JobRunResult(JobExecutionStatus Status, string? Error = null)
{
    public static readonly JobRunResult Succeeded = new(JobExecutionStatus.Success);

    public static JobRunResult Failure(string error) => new(JobExecutionStatus.Failed, error);
}
