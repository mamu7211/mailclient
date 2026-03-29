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

    public async Task<JobSettingsResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await db.JobSettings
            .Include(j => j.Executions
                .OrderByDescending(e => e.StartedAt)
                .Take(5))
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        return job is null ? null : MapToResponse(job);
    }

    public async Task<PaginatedJobExecutionsResponse?> GetExecutionsAsync(
        Guid jobId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var jobExists = await db.JobSettings.AnyAsync(j => j.Id == jobId, cancellationToken);
        if (!jobExists)
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
        Guid id, UpdateJobSettingsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var job = await db.JobSettings
            .Include(j => j.Executions
                .OrderByDescending(e => e.StartedAt)
                .Take(5))
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (job is null)
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
        else if (!wasEnabled && job.Enabled)
        {
            await scheduler.ScheduleJobAsync(job.JobName, job.Cron);
            logger.LogInformation("Job '{JobName}' enabled with cron '{Cron}'", job.JobName, job.Cron);
        }
        else if (wasEnabled && job.Enabled && oldCron != job.Cron)
        {
            await scheduler.RescheduleJobAsync(job.JobName, job.Cron);
            logger.LogInformation("Job '{JobName}' rescheduled with cron '{Cron}'", job.JobName, job.Cron);
        }

        return MapToResponse(job);
    }

    public async Task<bool> TriggerRunAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await db.JobSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (job is null)
            return false;

        if (!registry.HasJob(job.JobName))
        {
            logger.LogWarning("No managed job registered for '{JobName}', cannot trigger", job.JobName);
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
            var jobType = registry.GetJobType(job.JobName);
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
            job.Description,
            job.Cron,
            job.Enabled,
            job.LastRunAt,
            job.LastStatus?.ToString(),
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
