using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Feirb.Api.Services;

public class ImapSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<ImapSyncService> logger,
    IOptions<ImapSyncSettings> syncSettings) : IImapSyncService
{
    private const string _imapPasswordPurpose = "MailboxImapPassword";
    private readonly int _saveBatchSize = syncSettings.Value.SaveBatchSize;

    public async Task SyncMailboxAsync(Guid mailboxId, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        var dataProtection = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();

        var mailbox = await db.Mailboxes
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == mailboxId, cancellationToken);

        if (mailbox is null)
        {
            logger.LogWarning("Mailbox {MailboxId} not found, skipping sync", mailboxId);
            return;
        }

        if (string.IsNullOrEmpty(mailbox.ImapEncryptedPassword))
        {
            logger.LogWarning("Mailbox {MailboxId} has no IMAP password configured, skipping sync", mailboxId);
            return;
        }

        try
        {
            var protector = dataProtection.CreateProtector(_imapPasswordPurpose);
            var password = protector.Unprotect(mailbox.ImapEncryptedPassword);

            using var client = new ImapClient();
            await client.ConnectAsync(mailbox.ImapHost, mailbox.ImapPort, mailbox.ImapUseTls, cancellationToken);
            await client.AuthenticateAsync(mailbox.ImapUsername, password, cancellationToken);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            var lastSeenUid = await GetLastSeenUidAsync(db, mailboxId, cancellationToken);

            IList<UniqueId> uids;
            if (lastSeenUid.HasValue)
            {
                uids = await FetchIncrementalUidsAsync(inbox, lastSeenUid.Value, cancellationToken);
            }
            else
            {
                uids = await FetchInitialUidsAsync(inbox, mailbox, cancellationToken);
            }

            if (uids.Count == 0)
            {
                logger.LogDebug("No new messages for mailbox {MailboxId}", mailboxId);
                await client.DisconnectAsync(quit: true, cancellationToken);
                return;
            }

            logger.LogInformation("Fetching {Count} messages for mailbox {MailboxId}", uids.Count, mailboxId);

            var existingMessageIds = await db.CachedMessages
                .Where(cm => cm.MailboxId == mailboxId)
                .Select(cm => cm.MessageId)
                .ToHashSetAsync(cancellationToken);

            var pendingCount = 0;
            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid, cancellationToken);
                if (message.MessageId is not null && existingMessageIds.Contains(message.MessageId))
                {
                    logger.LogDebug("Skipping duplicate message {MessageId} in mailbox {MailboxId}",
                        message.MessageId, mailboxId);
                    continue;
                }

                var cached = MapToCachedMessage(message, mailboxId, uid);
                db.CachedMessages.Add(cached);
                existingMessageIds.Add(cached.MessageId);
                pendingCount++;

                if (pendingCount >= _saveBatchSize)
                {
                    await db.SaveChangesAsync(cancellationToken);
                    pendingCount = 0;
                    logger.LogInformation("Saved batch of {BatchSize} messages for mailbox {MailboxId}",
                        _saveBatchSize, mailboxId);
                }
            }

            if (pendingCount > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            await client.DisconnectAsync(quit: true, cancellationToken);

            logger.LogInformation("Sync completed for mailbox {MailboxId}", mailboxId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync failed for mailbox {MailboxId}", mailboxId);
        }
    }

    private static async Task<uint?> GetLastSeenUidAsync(
        FeirbDbContext db, Guid mailboxId, CancellationToken cancellationToken) =>
        await db.CachedMessages
            .Where(cm => cm.MailboxId == mailboxId && cm.ImapUid != null)
            .MaxAsync(cm => (uint?)cm.ImapUid, cancellationToken);

    private static async Task<IList<UniqueId>> FetchIncrementalUidsAsync(
        IMailFolder inbox, uint lastSeenUid, CancellationToken cancellationToken)
    {
        var query = SearchQuery.Uids(new UniqueIdRange(new UniqueId(lastSeenUid + 1), UniqueId.MaxValue));
        return await inbox.SearchAsync(query, cancellationToken);
    }

    private static async Task<IList<UniqueId>> FetchInitialUidsAsync(
        IMailFolder inbox, Mailbox mailbox, CancellationToken cancellationToken)
    {
        if (mailbox.InitialSyncDays <= 0)
            return await inbox.SearchAsync(SearchQuery.All, cancellationToken);

        var timeZone = GetTimeZone(mailbox.User?.TimeZone);
        var cutoff = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date
            .AddDays(-mailbox.InitialSyncDays);
        var cutoffUtc = TimeZoneInfo.ConvertTimeToUtc(cutoff, timeZone);

        var query = SearchQuery.DeliveredAfter(cutoffUtc.Date);
        return await inbox.SearchAsync(query, cancellationToken);
    }

    private static TimeZoneInfo GetTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrEmpty(timeZoneId))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    internal static CachedMessage MapToCachedMessage(MimeMessage message, Guid mailboxId, UniqueId uid) =>
        new()
        {
            Id = Guid.NewGuid(),
            MailboxId = mailboxId,
            MessageId = message.MessageId ?? $"<no-id-{uid.Id}@sync>",
            ImapUid = uid.Id,
            Subject = message.Subject ?? string.Empty,
            From = message.From.ToString(),
            ReplyTo = message.ReplyTo.Count > 0 ? message.ReplyTo.ToString() : null,
            To = message.To.ToString(),
            Cc = message.Cc.Count > 0 ? message.Cc.ToString() : null,
            Date = message.Date.ToUniversalTime(),
            BodyPlainText = message.TextBody,
            BodyHtml = message.HtmlBody,
            SyncedAt = DateTimeOffset.UtcNow,
            Attachments = message.Attachments
                .Select(a => new CachedAttachment
                {
                    Id = Guid.NewGuid(),
                    Filename = a is MimePart part ? part.FileName ?? "unnamed" : "unnamed",
                    Size = a is MimePart mp ? EstimateAttachmentSize(mp) : 0,
                    MimeType = a.ContentType.MimeType,
                })
                .ToList(),
        };

    private static long EstimateAttachmentSize(MimePart part)
    {
        var stream = part.Content?.Stream;
        if (stream is { CanSeek: true })
            return stream.Length;

        return 0;
    }
}
