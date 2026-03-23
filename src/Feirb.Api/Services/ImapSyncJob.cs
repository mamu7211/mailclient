using Quartz;

namespace Feirb.Api.Services;

public class ImapSyncJob(IImapSyncService syncService) : IJob
{
    public const string MailboxIdKey = "MailboxId";

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var mailboxId = Guid.Parse(context.MergedJobDataMap.GetString(MailboxIdKey)!);
        await syncService.SyncMailboxAsync(mailboxId, context.CancellationToken);
    }

    public static JobKey GetJobKey(Guid mailboxId) => new($"imap-sync-{mailboxId}", "imap-sync");
    public static TriggerKey GetTriggerKey(Guid mailboxId) => new($"imap-trigger-{mailboxId}", "imap-sync");
}
