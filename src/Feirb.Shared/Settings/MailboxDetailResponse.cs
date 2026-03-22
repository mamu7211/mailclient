namespace Feirb.Shared.Settings;

public record MailboxDetailResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    string? DisplayName,
    string ImapHost,
    int ImapPort,
    string ImapUsername,
    bool ImapUseTls,
    string SmtpHost,
    int SmtpPort,
    string SmtpUsername,
    bool SmtpUseTls,
    bool SmtpRequiresAuth,
    DateTime CreatedAt,
    DateTime UpdatedAt);
