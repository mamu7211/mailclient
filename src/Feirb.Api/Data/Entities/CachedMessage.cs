namespace Feirb.Api.Data.Entities;

public class CachedMessage
{
    public Guid Id { get; set; }
    public Guid MailboxId { get; set; }
    public Mailbox Mailbox { get; set; } = null!;
    public required string MessageId { get; set; }
    public uint? ImapUid { get; set; }
    public required string Subject { get; set; }
    public required string From { get; set; }
    public string? ReplyTo { get; set; }
    public required string To { get; set; }
    public string? Cc { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? BodyPlainText { get; set; }
    public string? BodyHtml { get; set; }
    public DateTimeOffset SyncedAt { get; set; }

    public ICollection<CachedAttachment> Attachments { get; set; } = [];
    public ICollection<Label> Labels { get; set; } = [];
}
