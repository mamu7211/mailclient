using Feirb.Api.Data.Entities;

namespace Feirb.Api.Services;

public record GeneratedTokens(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public interface IAuthService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    GeneratedTokens GenerateTokens(User user);
    Guid? ValidateAccessTokenAsync(string token);
    string GenerateResetToken();
}
