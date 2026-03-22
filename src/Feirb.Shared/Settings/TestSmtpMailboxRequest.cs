using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Settings;

public record TestSmtpMailboxRequest(
    [Required, StringLength(256)]
    string Host,
    [Required, Range(1, 65535)]
    int Port,
    [StringLength(256)]
    string? Username,
    string? Password,
    bool UseTls,
    bool RequiresAuth);
