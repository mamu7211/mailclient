using Feirb.Shared.Admin.Jobs;

namespace Feirb.Api.Services;

public interface IJobService
{
    Task<List<JobSettingsResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<JobSettingsResponse>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<JobSettingsResponse>> GetByResourceAsync(string resourceType, Guid resourceId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<JobSettingsResponse?> GetByIdAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<PaginatedJobExecutionsResponse?> GetExecutionsAsync(Guid jobId, Guid userId, bool isAdmin, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<JobSettingsResponse?> UpdateAsync(Guid id, UpdateJobSettingsRequest request, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<bool> TriggerRunAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
}
