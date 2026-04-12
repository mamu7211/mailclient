namespace Feirb.Shared.Settings;

public record MailboxDetailResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    string? DisplayName,
    string? BadgeColor,
    string ImapHost,
    int ImapPort,
    string ImapUsername,
    TlsMode ImapTlsMode,
    string SmtpHost,
    int SmtpPort,
    string SmtpUsername,
    TlsMode SmtpTlsMode,
    bool SmtpRequiresAuth,
    DateTime CreatedAt,
    DateTime UpdatedAt);
