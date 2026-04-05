using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Mail;

public record SendMailRequest(
    [Required]
    Guid MailboxId,
    [Required, MinLength(1)]
    string[] To,
    string[]? Cc,
    string[]? Bcc,
    [Required, StringLength(998)]
    string Subject,
    [Required, MaxLength(1_048_576)]
    string Body,
    [Required, RegularExpression("^(html|plain)$")]
    string ContentType);
