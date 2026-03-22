using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Settings;

public record CreateMailboxRequest(
    [Required, StringLength(256)]
    string Name,
    [Required, EmailAddress, StringLength(256)]
    string EmailAddress,
    [StringLength(256)]
    string? DisplayName,
    [Required, StringLength(256)]
    string ImapHost,
    [Required, Range(1, 65535)]
    int ImapPort,
    [Required, StringLength(256)]
    string ImapUsername,
    string? ImapPassword,
    bool ImapUseTls,
    [Required, StringLength(256)]
    string SmtpHost,
    [Required, Range(1, 65535)]
    int SmtpPort,
    [Required, StringLength(256)]
    string SmtpUsername,
    string? SmtpPassword,
    bool SmtpUseTls,
    bool SmtpRequiresAuth);
