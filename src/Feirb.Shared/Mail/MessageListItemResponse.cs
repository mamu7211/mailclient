using Feirb.Shared.AddressBook;

namespace Feirb.Shared.Mail;

public record MessageListItemResponse(
    Guid Id,
    string MailboxName,
    string? MailboxBadgeColor,
    string FromName,
    string FromEmail,
    AddressStatus? SenderStatus,
    string Subject,
    string? Summary,
    DateTimeOffset Date,
    bool IsRead,
    bool HasAttachments,
    List<MessageLabelResponse> Labels);
