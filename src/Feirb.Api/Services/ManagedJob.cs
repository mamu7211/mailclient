using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Feirb.Api.Services;

public abstract class ManagedJob(IServiceScopeFactory scopeFactory, ILogger logger) : IJob
{
    public const string JobNameKey = "ManagedJobName";
    private const int _consecutiveFailureThreshold = 10;
    private static readonly TimeSpan _staleExecutionCutoff = TimeSpan.FromHours(1);

    private Guid? _currentExecutionId;

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var jobName = context.MergedJobDataMap.GetString(JobNameKey)
            ?? throw new InvalidOperationException("ManagedJobName not set in job data map.");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        var jobSettings = await db.JobSettings
            .FirstOrDefaultAsync(j => j.JobName == jobName, context.CancellationToken);

        if (jobSettings is null)
        {
            logger.LogWarning("JobSettings not found for '{JobName}', skipping execution", jobName);
            return;
        }

        var staleCutoff = DateTimeOffset.UtcNow - _staleExecutionCutoff;
        var alreadyRunning = await db.JobExecutions
            .AnyAsync(je => je.JobSettingsId == jobSettings.Id
                && je.FinishedAt == null
                && je.StartedAt > staleCutoff, context.CancellationToken);

        if (alreadyRunning)
        {
            logger.LogDebug("Job '{JobName}' is already running, skipping this execution", jobName);

            var now = DateTimeOffset.UtcNow;
            db.JobExecutions.Add(new JobExecution
            {
                Id = Guid.NewGuid(),
                JobSettingsId = jobSettings.Id,
                StartedAt = now,
                FinishedAt = now,
                Status = JobExecutionStatus.Skipped,
            });

            jobSettings.LastRunAt = now;
            jobSettings.LastStatus = JobExecutionStatus.Skipped;

            await db.SaveChangesAsync(context.CancellationToken);
            return;
        }

        var execution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobSettingsId = jobSettings.Id,
            StartedAt = DateTimeOffset.UtcNow,
            Status = JobExecutionStatus.Success,
        };
        db.JobExecutions.Add(execution);

        // Persist the execution record before running the job so the already-running
        // check reliably prevents concurrent executions across overlapping triggers.
        await db.SaveChangesAsync(context.CancellationToken);

        _currentExecutionId = execution.Id;

        try
        {
            await RunAsync(scope.ServiceProvider, jobSettings, context.CancellationToken);
            execution.Status = JobExecutionStatus.Success;
            execution.FinishedAt = DateTimeOffset.UtcNow;

            jobSettings.LastRunAt = execution.FinishedAt;
            jobSettings.LastStatus = JobExecutionStatus.Success;

            await db.SaveChangesAsync(context.CancellationToken);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Job '{JobName}' was cancelled", jobName);
            execution.Status = JobExecutionStatus.Cancelled;
            execution.FinishedAt = DateTimeOffset.UtcNow;

            jobSettings.LastRunAt = execution.FinishedAt;
            jobSettings.LastStatus = JobExecutionStatus.Cancelled;

            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job '{JobName}' failed", jobName);
            execution.Status = JobExecutionStatus.Failed;
            execution.Error = ex.Message.Length > 4096 ? ex.Message[..4096] : ex.Message;
            execution.FinishedAt = DateTimeOffset.UtcNow;

            jobSettings.LastRunAt = execution.FinishedAt;
            jobSettings.LastStatus = JobExecutionStatus.Failed;

            await db.SaveChangesAsync(CancellationToken.None);
            await CheckConsecutiveFailuresAsync(db, jobSettings, scope.ServiceProvider);
        }
        finally
        {
            _currentExecutionId = null;
        }
    }

    protected abstract Task RunAsync(IServiceProvider serviceProvider, JobSettings jobSettings, CancellationToken cancellationToken);

    protected async Task LogAsync(
        JobExecutionLogLevel level, string message, string? metadata = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_currentExecutionId is null)
            return;

        using var logScope = scopeFactory.CreateScope();
        var logDb = logScope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        logDb.JobExecutionLogs.Add(new JobExecutionLog
        {
            Id = Guid.NewGuid(),
            JobExecutionId = _currentExecutionId.Value,
            Timestamp = DateTimeOffset.UtcNow,
            Level = level,
            Message = message.Length > 4096 ? message[..4096] : message,
            Metadata = metadata is not null && metadata.Length > 4096 ? metadata[..4096] : metadata,
        });

        await logDb.SaveChangesAsync(cancellationToken);
    }

    private async Task CheckConsecutiveFailuresAsync(
        FeirbDbContext db, JobSettings jobSettings, IServiceProvider serviceProvider)
    {
        var recentStatuses = await db.JobExecutions
            .Where(e => e.JobSettingsId == jobSettings.Id)
            .OrderByDescending(e => e.StartedAt)
            .Take(_consecutiveFailureThreshold)
            .Select(e => e.Status)
            .ToListAsync();

        if (recentStatuses.Count == _consecutiveFailureThreshold
            && recentStatuses.All(s => s == JobExecutionStatus.Failed))
        {
            jobSettings.Enabled = false;
            jobSettings.RowVersion = Guid.NewGuid();
            await db.SaveChangesAsync();

            var scheduler = serviceProvider.GetRequiredService<IJobSettingsScheduler>();
            await scheduler.UnscheduleJobAsync(jobSettings.JobName);

            logger.LogWarning(
                "Job '{JobName}' auto-disabled after {Threshold} consecutive failures",
                jobSettings.JobName, _consecutiveFailureThreshold);
        }
    }
}
