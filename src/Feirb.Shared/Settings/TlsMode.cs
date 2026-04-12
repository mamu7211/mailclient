using System.Text.Json.Serialization;

namespace Feirb.Shared.Settings;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TlsMode
{
    None = 0,
    Auto = 1,
    SslOnConnect = 2,
    StartTls = 3,
}
