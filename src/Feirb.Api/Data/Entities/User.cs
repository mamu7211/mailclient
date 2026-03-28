namespace Feirb.Api.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public bool IsAdmin { get; set; }
    public string? TimeZone { get; set; }
    public string? Theme { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
