namespace Feirb.Shared.Mail;

public record MessageListItemResponse(
    Guid Id,
    string MailboxName,
    string? MailboxBadgeColor,
    string FromName,
    string FromEmail,
    string Subject,
    string? Summary,
    DateTimeOffset Date,
    bool IsRead,
    bool HasAttachments,
    List<MessageLabelResponse> Labels);
