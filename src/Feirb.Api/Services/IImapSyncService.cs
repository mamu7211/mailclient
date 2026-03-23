namespace Feirb.Api.Services;

public interface IImapSyncService
{
    Task SyncMailboxAsync(Guid mailboxId, CancellationToken cancellationToken = default);
}
