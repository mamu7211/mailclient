namespace Feirb.Api.Services;

public interface IImapSyncScheduler
{
    Task ScheduleMailboxAsync(Guid mailboxId, int pollIntervalMinutes, bool triggerImmediately = false);
    Task UnscheduleMailboxAsync(Guid mailboxId);
    Task RescheduleMailboxAsync(Guid mailboxId, int pollIntervalMinutes);
}
