namespace Feirb.Shared.Auth;

public record RegisterResponse(
    Guid Id,
    string Username,
    string Email);
