namespace Feirb.Shared.Mail;

public record MessageListItemResponse(
    Guid Id,
    string MailboxName,
    string? MailboxBadgeColor,
    string From,
    string Subject,
    DateTimeOffset Date,
    bool HasAttachments);
