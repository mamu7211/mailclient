using Feirb.Api.Data.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Feirb.Api.Services;

public class ImapSyncJob(IServiceScopeFactory scopeFactory, ILogger<ImapSyncJob> logger)
    : ManagedJob(scopeFactory, logger)
{
    protected override async Task RunAsync(
        IServiceProvider serviceProvider, JobSettings jobSettings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobSettings);

        if (jobSettings.ResourceId is not { } mailboxId)
        {
            logger.LogWarning("ImapSyncJob '{JobName}' has no ResourceId, skipping", jobSettings.JobName);
            return;
        }

        var syncService = serviceProvider.GetRequiredService<IImapSyncService>();
        await syncService.SyncMailboxAsync(mailboxId, cancellationToken);
    }
}
