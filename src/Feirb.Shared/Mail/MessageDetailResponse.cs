namespace Feirb.Shared.Mail;

public record MessageDetailResponse(
    Guid Id,
    string MailboxName,
    string? MailboxBadgeColor,
    string From,
    string To,
    string? Cc,
    string? ReplyTo,
    MessageAddressResponse FromAddress,
    List<MessageAddressResponse> ToAddresses,
    List<MessageAddressResponse>? CcAddresses,
    MessageAddressResponse? ReplyToAddress,
    DateTimeOffset Date,
    string Subject,
    string? BodyHtml,
    string? BodyPlainText,
    List<AttachmentResponse> Attachments,
    List<MessageLabelResponse> Labels);
