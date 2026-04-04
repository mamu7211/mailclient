using System.Text.Json;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Services;

public class ClassificationJob(IServiceScopeFactory scopeFactory, ILogger<ClassificationJob> logger)
    : ManagedJob(scopeFactory, logger)
{
    private const int _defaultBatchSize = 10;

    protected override async Task RunAsync(
        IServiceProvider serviceProvider, JobSettings jobSettings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobSettings);

        var batchSize = GetBatchSize(jobSettings);
        var db = serviceProvider.GetRequiredService<FeirbDbContext>();
        var classificationService = serviceProvider.GetRequiredService<IClassificationService>();

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
            return;
        }

        logger.LogInformation("Processing {Count} classification queue items", pendingItems.Count);

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
                }
                else
                {
                    item.Status = ClassificationQueueItemStatus.Failed;
                    item.Error = result.Error;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Classification failed for queue item {QueueItemId}", item.Id);
                item.Status = ClassificationQueueItemStatus.Failed;
                item.Error = ex.Message.Length > 4096 ? ex.Message[..4096] : ex.Message;
            }
        }

        var failed = pendingItems.Count(i => i.Status == ClassificationQueueItemStatus.Failed);
        var skipped = pendingItems.Count(i => i.Status == ClassificationQueueItemStatus.Pending);
        var classified = pendingItems.Count - failed - skipped;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Classification complete: {Classified} classified, {Failed} failed, {Skipped} skipped",
            classified, failed, skipped);
    }

    private static async Task ApplyLabelsAsync(
        FeirbDbContext db, CachedMessage message, string resultJson, CancellationToken cancellationToken)
    {
        var labelNames = JsonSerializer.Deserialize<string[]>(resultJson);
        if (labelNames is null || labelNames.Length == 0)
            return;

        var mailbox = message.Mailbox ?? await db.Mailboxes
            .AsNoTracking()
            .FirstAsync(m => m.Id == message.MailboxId, cancellationToken);

        var matchingLabels = await db.Labels
            .Where(l => l.UserId == mailbox.UserId && labelNames.Contains(l.Name))
            .ToListAsync(cancellationToken);

        // Ensure the Labels collection is loaded
        if (!db.Entry(message).Collection(m => m.Labels).IsLoaded)
        {
            await db.Entry(message).Collection(m => m.Labels).LoadAsync(cancellationToken);
        }

        foreach (var label in matchingLabels)
        {
            if (!message.Labels.Any(l => l.Id == label.Id))
            {
                message.Labels.Add(label);
            }
        }
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
