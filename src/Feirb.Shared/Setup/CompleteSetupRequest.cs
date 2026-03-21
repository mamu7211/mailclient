using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Setup;

public record CompleteSetupRequest(
    [Required, StringLength(100, MinimumLength = 3)]
    string Username,
    [Required, EmailAddress, StringLength(256)]
    string Email,
    [Required, MinLength(8)]
    string Password,
    [Required, StringLength(256)]
    string SmtpHost,
    [Required, Range(1, 65535)]
    int SmtpPort,
    [StringLength(256)]
    string? SmtpUsername,
    string? SmtpPassword,
    bool SmtpUseTls,
    bool SmtpRequiresAuth);
