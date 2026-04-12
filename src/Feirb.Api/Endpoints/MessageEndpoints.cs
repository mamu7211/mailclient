using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Api.Resources;
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

        var mapped = parsedSenders.Select(p =>
        {
            var m = p.Item;
            var (name, email) = p.Parsed;
            var normalized = EmailNormalizer.Normalize(email);
            AddressStatus? senderStatus = normalized.Length > 0 && statusByEmail.TryGetValue(normalized, out var s)
                ? s
                : null;
            var labels = m.Labels.Select(l => new MessageLabelResponse(l.Name, l.Color)).ToList();
            return new MessageListItemResponse(m.Id, m.MailboxName, m.BadgeColor, name, email, senderStatus, m.Subject, summaryPlaceholder, m.Date, IsRead: false, m.HasAttachments, labels);
        }).ToList();

        return Results.Ok(new PaginatedResponse<MessageListItemResponse>(mapped, page, pageSize, totalCount));
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
        return addresses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(a => ToAddressResponse(ParseFromAddress(a)))
            .ToList();
    }

    private static MessageAddressResponse ToAddressResponse((string Name, string Email) parsed) =>
        new(parsed.Name, parsed.Email);

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing NameIdentifier claim. Ensure the endpoint requires authorization.");
        return Guid.Parse(claim.Value);
    }
}
