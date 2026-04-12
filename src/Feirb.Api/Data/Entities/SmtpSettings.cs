using Feirb.Shared.Settings;

namespace Feirb.Api.Data.Entities;

public class SmtpSettings
{
    public Guid Id { get; set; }
    public required string Host { get; set; }
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? EncryptedPassword { get; set; }
    public TlsMode TlsMode { get; set; }
    public bool RequiresAuth { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
