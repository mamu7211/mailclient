using Feirb.Api.Data;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Feirb.Api.Services;

public class ImapSyncScheduler(
    ISchedulerFactory schedulerFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<ImapSyncScheduler> logger) : BackgroundService, IImapSyncScheduler
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var scheduler = await schedulerFactory.GetScheduler(stoppingToken);

            // Wait for the scheduler to be started by QuartzHostedService
            while (!scheduler.IsStarted && !stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(100, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested)
                return;

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

            var mailboxes = await db.Mailboxes
                .Select(m => new { m.Id, m.PollIntervalMinutes })
                .ToListAsync(stoppingToken);

            foreach (var mailbox in mailboxes)
            {
                await ScheduleJobAsync(scheduler, mailbox.Id, mailbox.PollIntervalMinutes, stoppingToken);
            }

            logger.LogInformation("Scheduled IMAP sync for {Count} mailboxes", mailboxes.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown requested, ignore
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize IMAP sync schedules");
        }
    }

    public async Task ScheduleMailboxAsync(Guid mailboxId, int pollIntervalMinutes, bool triggerImmediately = false)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        await ScheduleJobAsync(scheduler, mailboxId, pollIntervalMinutes);

        if (triggerImmediately)
        {
            await scheduler.TriggerJob(ImapSyncJob.GetJobKey(mailboxId));
        }
    }

    public async Task UnscheduleMailboxAsync(Guid mailboxId)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = ImapSyncJob.GetJobKey(mailboxId);

        if (await scheduler.CheckExists(jobKey))
        {
            await scheduler.DeleteJob(jobKey);
            logger.LogInformation("Unscheduled IMAP sync for mailbox {MailboxId}", mailboxId);
        }
    }

    public async Task RescheduleMailboxAsync(Guid mailboxId, int pollIntervalMinutes)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = ImapSyncJob.GetJobKey(mailboxId);

        if (await scheduler.CheckExists(jobKey))
        {
            await scheduler.DeleteJob(jobKey);
        }

        await ScheduleJobAsync(scheduler, mailboxId, pollIntervalMinutes);
    }

    private async Task ScheduleJobAsync(
        IScheduler scheduler, Guid mailboxId, int pollIntervalMinutes, CancellationToken cancellationToken = default)
    {
        var jobKey = ImapSyncJob.GetJobKey(mailboxId);
        var triggerKey = ImapSyncJob.GetTriggerKey(mailboxId);

        var job = JobBuilder.Create<ImapSyncJob>()
            .WithIdentity(jobKey)
            .UsingJobData(ImapSyncJob.MailboxIdKey, mailboxId.ToString())
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(pollIntervalMinutes)
                .RepeatForever())
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);
        logger.LogDebug("Scheduled IMAP sync for mailbox {MailboxId} every {Interval} minutes",
            mailboxId, pollIntervalMinutes);
    }
}
