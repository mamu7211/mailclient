using System.Text.Json;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Services;

public class LogRetentionCleanupJob(IServiceScopeFactory scopeFactory, ILogger<LogRetentionCleanupJob> logger)
    : ManagedJob(scopeFactory, logger)
{
    private const int _defaultRetentionDays = 30;

    protected override async Task<JobRunResult> RunAsync(
        IServiceProvider serviceProvider, JobSettings jobSettings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobSettings);

        var retentionDays = GetRetentionDays(jobSettings);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        var db = serviceProvider.GetRequiredService<FeirbDbContext>();

        await LogAsync(JobExecutionLogLevel.Info,
            $"Starting cleanup: deleting executions older than {retentionDays} days (before {cutoff:u})",
            cancellationToken: cancellationToken);

        // StartedAt is indexed; include it in the predicate so Postgres can use the index
        // to narrow the delete set. Safe because FinishedAt >= StartedAt for any completed run.
        var deletedCount = await db.JobExecutions
            .Where(e => e.StartedAt < cutoff
                && e.FinishedAt != null
                && e.FinishedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        await LogAsync(JobExecutionLogLevel.Info,
            $"Cleanup complete: deleted {deletedCount} execution(s)",
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Log retention cleanup complete: deleted {Count} execution(s) older than {Days} days",
            deletedCount, retentionDays);

        return JobRunResult.Succeeded;
    }

    private int GetRetentionDays(JobSettings jobSettings)
    {
        if (string.IsNullOrEmpty(jobSettings.Configuration))
            return _defaultRetentionDays;

        try
        {
            using var doc = JsonDocument.Parse(jobSettings.Configuration);
            if (doc.RootElement.TryGetProperty("retentionDays", out var element)
                && element.TryGetInt32(out var days)
                && days > 0)
            {
                return days;
            }
        }
        catch (JsonException)
        {
            logger.LogWarning(
                "Job '{JobName}' has invalid JSON in Configuration, using default retention of {Days} days",
                jobSettings.JobName, _defaultRetentionDays);
        }

        return _defaultRetentionDays;
    }
}
