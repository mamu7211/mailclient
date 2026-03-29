using Feirb.Api.Data;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Feirb.Api.Services;

public class JobSettingsScheduler(
    ISchedulerFactory schedulerFactory,
    IServiceScopeFactory scopeFactory,
    ManagedJobRegistry registry,
    ILogger<JobSettingsScheduler> logger) : BackgroundService, IJobSettingsScheduler
{
    private const string _groupName = "managed-jobs";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var scheduler = await schedulerFactory.GetScheduler(stoppingToken);

            while (!scheduler.IsStarted && !stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(100, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested)
                return;

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

            var jobs = await db.JobSettings
                .Where(j => j.Enabled)
                .ToListAsync(stoppingToken);

            var scheduled = 0;
            foreach (var job in jobs)
            {
                if (registry.HasJob(job.JobName))
                {
                    await ScheduleQuartzJobAsync(scheduler, job.JobName, job.Cron, stoppingToken);
                    scheduled++;
                }
                else
                {
                    logger.LogWarning("No managed job registered for '{JobName}', skipping", job.JobName);
                }
            }

            logger.LogInformation("Scheduled {Count} managed jobs on startup", scheduled);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown requested
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize managed job schedules");
        }
    }

    public async Task ScheduleJobAsync(string jobName, string cronExpression)
    {
        if (!registry.HasJob(jobName))
        {
            logger.LogWarning("No managed job registered for '{JobName}', cannot schedule", jobName);
            return;
        }

        var scheduler = await schedulerFactory.GetScheduler();
        await ScheduleQuartzJobAsync(scheduler, jobName, cronExpression);
    }

    public async Task UnscheduleJobAsync(string jobName)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = GetJobKey(jobName);

        if (await scheduler.CheckExists(jobKey))
        {
            await scheduler.DeleteJob(jobKey);
            logger.LogInformation("Unscheduled managed job '{JobName}'", jobName);
        }
    }

    public async Task RescheduleJobAsync(string jobName, string cronExpression)
    {
        if (!registry.HasJob(jobName))
        {
            logger.LogWarning("No managed job registered for '{JobName}', cannot reschedule", jobName);
            return;
        }

        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = GetJobKey(jobName);

        if (await scheduler.CheckExists(jobKey))
        {
            await scheduler.DeleteJob(jobKey);
        }

        await ScheduleQuartzJobAsync(scheduler, jobName, cronExpression);
    }

    private async Task ScheduleQuartzJobAsync(
        IScheduler scheduler, string jobName, string cronExpression, CancellationToken cancellationToken = default)
    {
        var jobType = registry.GetJobType(jobName);
        var jobKey = GetJobKey(jobName);
        var triggerKey = GetTriggerKey(jobName);

        var job = JobBuilder.Create(jobType)
            .WithIdentity(jobKey)
            .UsingJobData(ManagedJob.JobNameKey, jobName)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .WithCronSchedule(cronExpression)
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);
        logger.LogDebug("Scheduled managed job '{JobName}' with cron '{Cron}'", jobName, cronExpression);
    }

    private static JobKey GetJobKey(string jobName) => new($"managed-{jobName}", _groupName);
    private static TriggerKey GetTriggerKey(string jobName) => new($"managed-trigger-{jobName}", _groupName);
}
