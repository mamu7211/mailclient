using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Shared.Admin.Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Feirb.Api.Services;

public class JobService(
    FeirbDbContext db,
    IJobSettingsScheduler scheduler,
    ISchedulerFactory schedulerFactory,
    ManagedJobRegistry registry,
    ILogger<JobService> logger) : IJobService
{
    public async Task<List<JobSettingsResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await db.JobSettings
            .Include(j => j.Executions
                .OrderByDescending(e => e.StartedAt)
                .Take(5))
            .OrderBy(j => j.JobName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return jobs.Select(MapToResponse).ToList();
    }

    public async Task<List<JobSettingsResponse>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var jobs = await db.JobSettings
            .Include(j => j.Executions
                .OrderByDescending(e => e.StartedAt)
                .Take(5))
            .Where(j => j.UserId == userId || j.UserId == null)
            .OrderBy(j => j.JobName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return jobs.Select(MapToResponse).ToList();
    }

    public async Task<List<JobSettingsResponse>> GetByResourceAsync(
        string resourceType, Guid resourceId, Guid userId, bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var query = db.JobSettings
            .Include(j => j.Executions
                .OrderByDescending(e => e.StartedAt)
                .Take(5))
            .Where(j => j.ResourceType == resourceType && j.ResourceId == resourceId);

        if (!isAdmin)
            query = query.Where(j => j.UserId == userId || j.UserId == null);

        var jobs = await query
            .OrderBy(j => j.JobName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return jobs.Select(MapToResponse).ToList();
    }

    public async Task<JobSettingsResponse?> GetByIdAsync(
        Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var job = await db.JobSettings
            .Include(j => j.Executions
                .OrderByDescending(e => e.StartedAt)
                .Take(5))
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (job is null)
            return null;

        if (!isAdmin && job.UserId != userId && job.UserId != null)
            return null;

        return MapToResponse(job);
    }

    public async Task<PaginatedJobExecutionsResponse?> GetExecutionsAsync(
        Guid jobId, Guid userId, bool isAdmin, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var job = await db.JobSettings.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job is null)
            return null;

        if (!isAdmin && job.UserId != userId && job.UserId != null)
            return null;

        var query = db.JobExecutions
            .Where(e => e.JobSettingsId == jobId)
            .OrderByDescending(e => e.StartedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .Select(e => new JobExecutionResponse(
                e.Id,
                e.StartedAt,
                e.FinishedAt,
                e.Status.ToString(),
                e.Error))
            .ToListAsync(cancellationToken);

        return new PaginatedJobExecutionsResponse(items, totalCount, page, pageSize);
    }

    public async Task<JobSettingsResponse?> UpdateAsync(
        Guid id, UpdateJobSettingsRequest request, Guid userId, bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var job = await db.JobSettings
            .Include(j => j.Executions
                .OrderByDescending(e => e.StartedAt)
                .Take(5))
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (job is null)
            return null;

        if (!isAdmin && job.UserId != userId)
            return null;

        if (!CronExpression.IsValidExpression(request.Cron))
            throw new ArgumentException("Invalid cron expression.");

        db.Entry(job).Property(j => j.RowVersion).OriginalValue = request.RowVersion;

        var wasEnabled = job.Enabled;
        var oldCron = job.Cron;

        job.Cron = request.Cron;
        job.Enabled = request.Enabled;
        job.RowVersion = Guid.NewGuid();

        await db.SaveChangesAsync(cancellationToken);

        if (wasEnabled && !job.Enabled)
        {
            await scheduler.UnscheduleJobAsync(job.JobName);
            logger.LogInformation("Job '{JobName}' disabled", job.JobName);
        }
        else if (job.JobType is null || !registry.HasJobType(job.JobType))
        {
            logger.LogWarning("No managed job registered for JobType '{JobType}', skipping schedule update", job.JobType);
        }
        else if (!wasEnabled && job.Enabled)
        {
            await scheduler.ScheduleJobAsync(job.JobName, job.JobType, job.Cron);
            logger.LogInformation("Job '{JobName}' enabled with cron '{Cron}'", job.JobName, job.Cron);
        }
        else if (wasEnabled && job.Enabled && oldCron != job.Cron)
        {
            await scheduler.RescheduleJobAsync(job.JobName, job.JobType, job.Cron);
            logger.LogInformation("Job '{JobName}' rescheduled with cron '{Cron}'", job.JobName, job.Cron);
        }

        return MapToResponse(job);
    }

    public async Task<bool> TriggerRunAsync(
        Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var job = await db.JobSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (job is null)
            return false;

        if (!isAdmin && job.UserId != userId)
            return false;

        if (job.JobType is null || !registry.HasJobType(job.JobType))
        {
            logger.LogWarning("No managed job registered for JobType '{JobType}', cannot trigger", job.JobType);
            return false;
        }

        var quartzScheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey($"managed-{job.JobName}", "managed-jobs");

        if (await quartzScheduler.CheckExists(jobKey, cancellationToken))
        {
            await quartzScheduler.TriggerJob(jobKey, cancellationToken);
        }
        else
        {
            var jobType = registry.GetClrType(job.JobType);
            var adhocKey = new JobKey($"managed-adhoc-{job.JobName}-{Guid.NewGuid():N}", "managed-jobs");
            var quartzJob = JobBuilder.Create(jobType)
                .WithIdentity(adhocKey)
                .UsingJobData(ManagedJob.JobNameKey, job.JobName)
                .StoreDurably(false)
                .Build();
            var trigger = TriggerBuilder.Create()
                .ForJob(adhocKey)
                .StartNow()
                .Build();
            await quartzScheduler.ScheduleJob(quartzJob, trigger, cancellationToken);
        }

        logger.LogInformation("Job '{JobName}' triggered manually", job.JobName);
        return true;
    }

    private static JobSettingsResponse MapToResponse(JobSettings job) =>
        new(
            job.Id,
            job.JobName,
            job.JobType,
            job.Description,
            job.Cron,
            job.Enabled,
            job.LastRunAt,
            job.LastStatus?.ToString(),
            job.ResourceId,
            job.ResourceType,
            job.RowVersion,
            job.Executions
                .OrderByDescending(e => e.StartedAt)
                .Take(5)
                .Select(e => new JobExecutionResponse(
                    e.Id,
                    e.StartedAt,
                    e.FinishedAt,
                    e.Status.ToString(),
                    e.Error))
                .ToList());
}
