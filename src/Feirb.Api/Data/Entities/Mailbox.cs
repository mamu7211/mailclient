namespace Feirb.Api.Data.Entities;

public class Mailbox
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public required string Name { get; set; }
    public required string EmailAddress { get; set; }
    public string? DisplayName { get; set; }

    // IMAP settings
    public required string ImapHost { get; set; }
    public int ImapPort { get; set; } = 993;
    public required string ImapUsername { get; set; }
    public string? ImapEncryptedPassword { get; set; }
    public bool ImapUseTls { get; set; } = true;

    // SMTP settings
    public required string SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public required string SmtpUsername { get; set; }
    public string? SmtpEncryptedPassword { get; set; }
    public bool SmtpUseTls { get; set; } = true;
    public bool SmtpRequiresAuth { get; set; } = true;

    // Sync settings
    public string? BadgeColor { get; set; }
    public int InitialSyncDays { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<CachedMessage> CachedMessages { get; set; } = [];
}
