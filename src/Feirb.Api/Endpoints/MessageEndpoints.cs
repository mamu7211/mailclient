using System.Security.Claims;
using System.Text.Json;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Resources;
using Feirb.Api.Services;
using Feirb.Shared.AddressBook;
using Feirb.Shared.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class MessageEndpoints
{
    public static RouteGroupBuilder MapMessageEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/messages", ListMessagesAsync);
        group.MapGet("/messages/{id:guid}", GetMessageAsync);
        group.MapPost("/messages/{id:guid}/classify", ClassifyMessageAsync);
        return group;
    }

    private static async Task<IResult> ListMessagesAsync(
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer,
        int page = 1,
        int pageSize = 25)
    {
        var userId = GetCurrentUserId(httpContext);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var query = db.CachedMessages
            .Where(m => m.Mailbox.UserId == userId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(m => m.Date)
            .Skip(skip)
            .Take(pageSize)
            .Select(m => new { m.Id, MailboxName = m.Mailbox.Name, m.Mailbox.BadgeColor, m.From, m.Subject, m.Date, HasAttachments = m.Attachments.Any(), Labels = m.Labels.OrderBy(l => l.Name).Select(l => new { l.Name, l.Color }).ToList() })
            .ToListAsync();

        var summaryPlaceholder = localizer["SummaryPlaceholder"].Value;

        var parsedSenders = items
            .Select(m => new { Item = m, Parsed = ParseFromAddress(m.From) })
            .ToList();

        var senderEmails = parsedSenders
            .Select(p => EmailNormalizer.Normalize(p.Parsed.Email))
            .Where(e => e.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

        var statusByEmail = senderEmails.Count == 0
            ? new Dictionary<string, AddressStatus>(StringComparer.Ordinal)
            : await db.Addresses
                .AsNoTracking()
                .Where(a => a.UserId == userId && senderEmails.Contains(a.NormalizedEmail))
                .Select(a => new { a.NormalizedEmail, a.Status })
                .ToDictionaryAsync(x => x.NormalizedEmail, x => x.Status, StringComparer.Ordinal);

        var pageMessageIds = items.Select(m => m.Id).ToList();

        var queueStatusByMessageId = pageMessageIds.Count == 0
            ? new Dictionary<Guid, ClassificationQueueItemStatus>()
            : await db.ClassificationQueueItems
                .AsNoTracking()
                .Where(q => pageMessageIds.Contains(q.CachedMessageId))
                .Select(q => new { q.CachedMessageId, q.Status })
                .ToDictionaryAsync(x => x.CachedMessageId, x => x.Status);

        var classifiedMessageIds = pageMessageIds.Count == 0
            ? new HashSet<Guid>()
            : (await db.ClassificationResults
                .AsNoTracking()
                .Where(r => pageMessageIds.Contains(r.CachedMessageId))
                .Select(r => r.CachedMessageId)
                .ToListAsync()).ToHashSet();

        var mapped = parsedSenders.Select(p =>
        {
            var m = p.Item;
            var (name, email) = p.Parsed;
            var normalized = EmailNormalizer.Normalize(email);
            AddressStatus? senderStatus = normalized.Length > 0 && statusByEmail.TryGetValue(normalized, out var s)
                ? s
                : null;
            var labels = m.Labels.Select(l => new MessageLabelResponse(l.Name, l.Color)).ToList();
            var classificationStatus = ResolveClassificationStatus(m.Id, queueStatusByMessageId, classifiedMessageIds);
            return new MessageListItemResponse(m.Id, m.MailboxName, m.BadgeColor, name, email, senderStatus, m.Subject, summaryPlaceholder, m.Date, IsRead: false, m.HasAttachments, labels, classificationStatus);
        }).ToList();

        var pendingCount = await db.ClassificationQueueItems
            .AsNoTracking()
            .Where(q => q.CachedMessage.Mailbox.UserId == userId
                && (q.Status == ClassificationQueueItemStatus.Pending || q.Status == ClassificationQueueItemStatus.Processing))
            .CountAsync();

        var jobPaused = !await db.JobSettings
            .AsNoTracking()
            .Where(j => j.JobName == "Classification")
            .Select(j => j.Enabled)
            .FirstOrDefaultAsync();

        return Results.Ok(new MessageListResponse(mapped, page, pageSize, totalCount, pendingCount, jobPaused));
    }

    private static ClassificationStatus ResolveClassificationStatus(
        Guid messageId,
        Dictionary<Guid, ClassificationQueueItemStatus> queueStatusByMessageId,
        HashSet<Guid> classifiedMessageIds)
    {
        if (queueStatusByMessageId.TryGetValue(messageId, out var queueStatus))
        {
            return queueStatus switch
            {
                ClassificationQueueItemStatus.Pending => ClassificationStatus.Pending,
                ClassificationQueueItemStatus.Processing => ClassificationStatus.Processing,
                ClassificationQueueItemStatus.Failed => ClassificationStatus.Failed,
                ClassificationQueueItemStatus.Classified => ClassificationStatus.Classified,
                _ => ClassificationStatus.NotClassified,
            };
        }

        return classifiedMessageIds.Contains(messageId)
            ? ClassificationStatus.Classified
            : ClassificationStatus.NotClassified;
    }

    private static (string Name, string Email) ParseFromAddress(string from)
    {
        var match = System.Text.RegularExpressions.Regex.Match(from, @"^""?(.+?)""?\s*<(.+?)>\s*$");
        if (match.Success)
            return (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());

        return from.Contains('@') ? (from, from) : (from, string.Empty);
    }

    private static async Task<IResult> GetMessageAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);

        var message = await db.CachedMessages
            .Include(m => m.Mailbox)
            .Include(m => m.Attachments)
            .Include(m => m.Labels)
            .FirstOrDefaultAsync(m => m.Id == id && m.Mailbox.UserId == userId);

        if (message is null)
            return Results.NotFound(new { message = localizer["MessageNotFound"].Value });

        var (fromName, fromEmail) = ParseFromAddress(message.From);
        var normalizedFrom = EmailNormalizer.Normalize(fromEmail);

        AddressStatus? senderStatus = null;
        if (normalizedFrom.Length > 0)
        {
            senderStatus = await db.Addresses
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.NormalizedEmail == normalizedFrom)
                .Select(a => (AddressStatus?)a.Status)
                .FirstOrDefaultAsync();
        }

        var fromAddress = new MessageAddressResponse(fromName, fromEmail, senderStatus);
        var toAddresses = ParseAddressList(message.To);
        var ccAddresses = !string.IsNullOrEmpty(message.Cc) ? ParseAddressList(message.Cc) : null;
        var replyToAddress = !string.IsNullOrEmpty(message.ReplyTo) && message.ReplyTo != message.From
            ? ToAddressResponse(ParseFromAddress(message.ReplyTo))
            : null;

        var labels = message.Labels
            .OrderBy(l => l.Name)
            .Select(l => new MessageLabelResponse(l.Name, l.Color))
            .ToList();

        var response = new MessageDetailResponse(
            message.Id,
            message.Mailbox.Name,
            message.Mailbox.BadgeColor,
            message.From,
            message.To,
            message.Cc,
            message.ReplyTo,
            fromAddress,
            toAddresses,
            ccAddresses,
            replyToAddress,
            message.Date,
            message.Subject,
            message.BodyHtml,
            message.BodyPlainText,
            message.Attachments.Select(a => new AttachmentResponse(a.Id, a.Filename, a.Size, a.MimeType)).ToList(),
            labels);

        return Results.Ok(response);
    }

    private static List<MessageAddressResponse> ParseAddressList(string addresses)
    {
        if (MimeKit.InternetAddressList.TryParse(addresses, out var list))
        {
            return list.Mailboxes
                .Select(mb => new MessageAddressResponse(
                    string.IsNullOrWhiteSpace(mb.Name) ? mb.Address : mb.Name,
                    mb.Address))
                .ToList();
        }

        return addresses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(a => ToAddressResponse(ParseFromAddress(a)))
            .ToList();
    }

    private static MessageAddressResponse ToAddressResponse((string Name, string Email) parsed) =>
        new(parsed.Name, parsed.Email);

    private static async Task<IResult> ClassifyMessageAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IClassificationService classificationService,
        IStringLocalizer<ApiMessages> localizer,
        CancellationToken cancellationToken,
        bool dryRun = true)
    {
        var userId = GetCurrentUserId(httpContext);

        var message = await db.CachedMessages
            .Include(m => m.Mailbox)
            .Include(m => m.Labels)
            .FirstOrDefaultAsync(m => m.Id == id && m.Mailbox.UserId == userId, cancellationToken);

        if (message is null)
            return Results.NotFound(new { message = localizer["MessageNotFound"].Value });

        var detailed = await classificationService.ClassifyDetailedAsync(message, cancellationToken);

        // Empty rules/labels: skipped — return success with empty labels so the UI
        // can show the "No classification rules configured" help text.
        if (detailed.IsSkipped)
        {
            return Results.Ok(new ClassifyMessageResponse(
                Success: true,
                Labels: [],
                Applied: false,
                Error: null,
                Prompt: dryRun && detailed.Prompt is not null
                    ? new ClassifyPrompt(detailed.Prompt.System, detailed.Prompt.User)
                    : null,
                RawResponse: dryRun ? detailed.RawResponse : null));
        }

        // Hard failure: surface the error and skip applying.
        if (!detailed.Success)
        {
            return Results.Ok(new ClassifyMessageResponse(
                Success: false,
                Labels: [],
                Applied: false,
                Error: detailed.Error ?? localizer["ClassificationFailed"].Value,
                Prompt: dryRun && detailed.Prompt is not null
                    ? new ClassifyPrompt(detailed.Prompt.System, detailed.Prompt.User)
                    : null,
                RawResponse: dryRun ? detailed.RawResponse : null));
        }

        var labelNames = ParseLabelNames(detailed.Result);

        var applied = false;
        if (!dryRun && labelNames.Count > 0)
        {
            await ApplyLabelsAsync(db, message, labelNames, cancellationToken);
            applied = true;
        }

        if (!dryRun)
        {
            // Mirror the job: persist a ClassificationResult marker even for empty-label
            // outcomes so the message is considered classified.
            db.ClassificationResults.Add(new ClassificationResult
            {
                Id = Guid.NewGuid(),
                CachedMessageId = message.Id,
                Result = detailed.Result ?? "[]",
                ClassifiedAt = DateTimeOffset.UtcNow,
            });

            // If the message was queued (e.g. Pending/Failed), drop the queue item so it
            // won't be re-classified by the background job.
            var queueItem = await db.ClassificationQueueItems
                .FirstOrDefaultAsync(q => q.CachedMessageId == message.Id, cancellationToken);
            if (queueItem is not null)
                db.ClassificationQueueItems.Remove(queueItem);

            await db.SaveChangesAsync(cancellationToken);
            applied = true;
        }

        return Results.Ok(new ClassifyMessageResponse(
            Success: true,
            Labels: labelNames,
            Applied: applied,
            Error: null,
            Prompt: dryRun && detailed.Prompt is not null
                ? new ClassifyPrompt(detailed.Prompt.System, detailed.Prompt.User)
                : null,
            RawResponse: dryRun ? detailed.RawResponse : null));
    }

    private static IReadOnlyList<string> ParseLabelNames(string? resultJson)
    {
        if (string.IsNullOrWhiteSpace(resultJson))
            return [];

        try
        {
            var parsed = JsonSerializer.Deserialize<string[]>(resultJson) ?? [];
            return parsed.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static async Task ApplyLabelsAsync(
        FeirbDbContext db,
        CachedMessage message,
        IReadOnlyList<string> labelNames,
        CancellationToken cancellationToken)
    {
        var normalized = labelNames.Select(n => n.ToLowerInvariant()).ToArray();
        var matchingLabels = await db.Labels
            .Where(l => l.UserId == message.Mailbox.UserId && normalized.Contains(l.Name))
            .ToListAsync(cancellationToken);

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

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing NameIdentifier claim. Ensure the endpoint requires authorization.");
        return Guid.Parse(claim.Value);
    }
}
