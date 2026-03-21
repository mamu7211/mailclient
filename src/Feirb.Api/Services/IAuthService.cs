using Feirb.Api.Data.Entities;
using Feirb.Shared.Auth;

namespace Feirb.Api.Services;

public interface IAuthService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    TokenResponse GenerateTokens(User user);
    Guid? ValidateAccessTokenAsync(string token);
}
