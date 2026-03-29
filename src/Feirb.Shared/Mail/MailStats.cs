namespace Feirb.Shared.Mail;

public record MailStats(string Label, List<MailboxMailStats> MailboxCounts);
