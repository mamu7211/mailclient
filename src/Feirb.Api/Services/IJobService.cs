using Feirb.Shared.Admin.Jobs;

namespace Feirb.Api.Services;

public interface IJobService
{
    Task<List<JobSettingsResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<JobSettingsResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaginatedJobExecutionsResponse?> GetExecutionsAsync(Guid jobId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<JobSettingsResponse?> UpdateAsync(Guid id, UpdateJobSettingsRequest request, CancellationToken cancellationToken = default);
    Task<bool> TriggerRunAsync(Guid id, CancellationToken cancellationToken = default);
}
