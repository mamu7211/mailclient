namespace Feirb.Shared.Admin.Jobs;

public record PaginatedJobExecutionLogsResponse(
    List<JobExecutionLogResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
