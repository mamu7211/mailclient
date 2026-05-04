using System.Text.Json;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Services;

public class ClassificationJob(IServiceScopeFactory scopeFactory, ILogger<ClassificationJob> logger)
    : ManagedJob(scopeFactory, logger)
{
    private const int _defaultBatchSize = 10;

    protected override async Task<JobRunResult> RunAsync(
        IServiceProvider serviceProvider, JobSettings jobSettings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobSettings);

        var batchSize = GetBatchSize(jobSettings);
        var db = serviceProvider.GetRequiredService<FeirbDbContext>();
        var classificationService = serviceProvider.GetRequiredService<IClassificationService>();

        // Recover items stuck in Processing from a previous failed/crashed run.
        // ManagedJob persists the JobExecution record before calling RunAsync, so the
        // already-running check reliably prevents concurrent execution. Any Processing
        // items at this point are from a prior run that didn't complete.
        var stuckItems = await db.ClassificationQueueItems
            .Where(q => q.Status == ClassificationQueueItemStatus.Processing)
            .ToListAsync(cancellationToken);

        if (stuckItems.Count > 0)
        {
            logger.LogWarning(
                "Recovered {Count} classification queue items stuck in Processing status", stuckItems.Count);
            foreach (var item in stuckItems)
            {
                item.Status = ClassificationQueueItemStatus.Pending;
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        var pendingItems = await db.ClassificationQueueItems
            .Include(q => q.CachedMessage)
                .ThenInclude(m => m.Mailbox)
            .Where(q => q.Status == ClassificationQueueItemStatus.Pending)
            .OrderBy(q => q.Ordinal).ThenBy(q => q.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pendingItems.Count == 0)
        {
            logger.LogDebug("No pending classification queue items found");
            return JobRunResult.Succeeded;
        }

        logger.LogInformation("Processing {Count} classification queue items", pendingItems.Count);
        await LogAsync(JobExecutionLogLevel.Info, $"Classification started (batch: {pendingItems.Count} items)", cancellationToken: cancellationToken);

        foreach (var item in pendingItems)
        {
            item.Status = ClassificationQueueItemStatus.Processing;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var item in pendingItems)
        {
            try
            {
                var result = await classificationService.ClassifyAsync(item.CachedMessage, cancellationToken);

                if (result.IsSkipped)
                {
                    // No rules or labels configured, or Ollama unavailable — revert to Pending
                    item.Status = ClassificationQueueItemStatus.Pending;
                    await LogAsync(JobExecutionLogLevel.Warning, $"Mail '{item.CachedMessage.Subject}' -> skipped (classifier not configured or unavailable)", cancellationToken: cancellationToken);
                    continue;
                }

                if (result.Success && result.Result is not null)
                {
                    await ApplyLabelsAsync(db, item.CachedMessage, result.Result, cancellationToken);

                    db.ClassificationResults.Add(new ClassificationResult
                    {
                        Id = Guid.NewGuid(),
                        CachedMessageId = item.CachedMessageId,
                        Result = result.Result,
                        ClassifiedAt = DateTimeOffset.UtcNow,
                    });

                    db.ClassificationQueueItems.Remove(item);
                    await LogAsync(JobExecutionLogLevel.Info, $"Mail '{item.CachedMessage.Subject}' -> labels: {result.Result}", cancellationToken: cancellationToken);
                }
                else if (result.Success)
                {
                    // Empty array — no labels, but still a successful classification
                    db.ClassificationResults.Add(new ClassificationResult
                    {
                        Id = Guid.NewGuid(),
                        CachedMessageId = item.CachedMessageId,
                        Result = "[]",
                        ClassifiedAt = DateTimeOffset.UtcNow,
                    });

                    db.ClassificationQueueItems.Remove(item);
                    await LogAsync(JobExecutionLogLevel.Info, $"Mail '{item.CachedMessage.Subject}' -> labels: []", cancellationToken: cancellationToken);
                }
                else
                {
                    item.Status = ClassificationQueueItemStatus.Failed;
                    item.Error = result.Error;
                    await LogAsync(JobExecutionLogLevel.Error, $"Mail '{item.CachedMessage.Subject}' -> failed: {result.Error}", cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Classification failed for queue item {QueueItemId}", item.Id);
                item.Status = ClassificationQueueItemStatus.Failed;
                item.Error = ex.Message.Length > 4096 ? ex.Message[..4096] : ex.Message;
                await LogAsync(JobExecutionLogLevel.Error, $"Mail '{item.CachedMessage.Subject}' -> failed: {ex.Message}");
            }
        }

        var failed = pendingItems.Count(i => i.Status == ClassificationQueueItemStatus.Failed);
        var skipped = pendingItems.Count(i => i.Status == ClassificationQueueItemStatus.Pending);
        var classified = pendingItems.Count - failed - skipped;

        await db.SaveChangesAsync(cancellationToken);

        await LogAsync(JobExecutionLogLevel.Info, $"Batch complete: {classified} classified, {skipped} skipped, {failed} failed", cancellationToken: cancellationToken);

        logger.LogInformation(
            "Classification complete: {Classified} classified, {Failed} failed, {Skipped} skipped",
            classified, failed, skipped);

        if (classified == 0 && failed > 0)
            return JobRunResult.Failure($"All {failed} item(s) failed classification");

        return JobRunResult.Succeeded;
    }

    private static async Task ApplyLabelsAsync(
        FeirbDbContext db, CachedMessage message, string resultJson, CancellationToken cancellationToken)
    {
        var rawLabelNames = JsonSerializer.Deserialize<string[]>(resultJson);
        if (rawLabelNames is null || rawLabelNames.Length == 0)
            return;

        await MessageLabelApplier.ApplyAsync(db, message, rawLabelNames, cancellationToken);
    }

    private int GetBatchSize(JobSettings jobSettings)
    {
        if (string.IsNullOrEmpty(jobSettings.Configuration))
            return _defaultBatchSize;

        try
        {
            using var doc = JsonDocument.Parse(jobSettings.Configuration);
            if (doc.RootElement.TryGetProperty("batchSize", out var batchSizeElement)
                && batchSizeElement.TryGetInt32(out var batchSize)
                && batchSize > 0)
            {
                return batchSize;
            }
        }
        catch (JsonException)
        {
            logger.LogWarning(
                "Job '{JobName}' has invalid JSON in Configuration, using default batch size {BatchSize}",
                jobSettings.JobName, _defaultBatchSize);
        }

        return _defaultBatchSize;
    }
}
