namespace Feirb.Shared.Mail;

public record MessageDetailResponse(
    Guid Id,
    string MailboxName,
    string? MailboxBadgeColor,
    string From,
    string To,
    string? Cc,
    string? ReplyTo,
    DateTimeOffset Date,
    string Subject,
    string? BodyHtml,
    string? BodyPlainText,
    List<AttachmentResponse> Attachments);
