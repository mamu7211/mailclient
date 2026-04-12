namespace Feirb.Shared.Auth;

public record TokenResponse(
    string AccessToken,
    DateTime ExpiresAt);
