using Feirb.Shared.Settings;

namespace Feirb.Shared.Admin;

public record SystemSmtpSettingsResponse(
    string Host,
    int Port,
    TlsMode TlsMode,
    bool RequiresAuth,
    string? Username,
    string? FromAddress,
    string? FromName);
