namespace Feirb.Shared.Mail;

public record MailboxMailStats(Guid MailboxId, string MailboxName, string? BadgeColor, int Count);
