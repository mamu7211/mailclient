namespace Feirb.Shared.Admin.Jobs;

public record PaginatedJobExecutionsResponse(
    List<JobExecutionResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
