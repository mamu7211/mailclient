using Feirb.Shared.Settings;
using MailKit.Security;

namespace Feirb.Api.Services;

public static class TlsModeConverter
{
    public static SecureSocketOptions ToSecureSocketOptions(TlsMode mode) =>
        mode switch
        {
            TlsMode.None => SecureSocketOptions.None,
            TlsMode.Auto => SecureSocketOptions.Auto,
            TlsMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
            TlsMode.StartTls => SecureSocketOptions.StartTls,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported TLS mode."),
        };
}
