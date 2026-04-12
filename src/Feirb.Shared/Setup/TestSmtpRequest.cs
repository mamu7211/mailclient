using System.ComponentModel.DataAnnotations;
using Feirb.Shared.Settings;

namespace Feirb.Shared.Setup;

public record TestSmtpRequest(
    [Required, StringLength(256)]
    string Host,
    [Required, Range(1, 65535)]
    int Port,
    [StringLength(256)]
    string? Username,
    string? Password,
    TlsMode TlsMode,
    bool RequiresAuth);
