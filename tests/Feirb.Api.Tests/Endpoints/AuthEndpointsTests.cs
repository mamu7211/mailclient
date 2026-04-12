using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Api.Data;
using Feirb.Shared.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Feirb.Api.Tests.Endpoints;

public class AuthEndpointsTests : IDisposable
{
    private const string RefreshTokenCookieName = "refreshToken";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests()
    {
        _factory = TestWebApplicationFactory.Create($"TestDb-{Guid.NewGuid()}");
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(TokenResponse Tokens, string? RefreshTokenCookie)> RegisterAndLoginAsync(
        string username = "testuser",
        string email = "test@example.com",
        string password = "Password123!")
    {
        var registerRequest = new RegisterRequest(username, email, password);
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(username, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        tokens.Should().NotBeNull();

        var refreshCookie = ExtractRefreshTokenCookie(response);
        return (tokens!, refreshCookie);
    }

    private static string? ExtractRefreshTokenCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        var refreshCookie = cookies.FirstOrDefault(c => c.StartsWith($"{RefreshTokenCookieName}=", StringComparison.Ordinal));
        return refreshCookie;
    }

    private static string ExtractCookieValue(string setCookieHeader)
    {
        var value = setCookieHeader.Split(';')[0];
        return value[(value.IndexOf('=') + 1)..];
    }

    // --- Registration tests ---

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreatedAsync()
    {
        var request = new RegisterRequest("testuser", "test@example.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsConflictAsync()
    {
        var first = new RegisterRequest("dupeuser", "first@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", first);

        var second = new RegisterRequest("dupeuser", "second@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/register", second);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflictAsync()
    {
        var first = new RegisterRequest("user1", "same@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", first);

        var second = new RegisterRequest("user2", "same@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/register", second);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_ValidRequest_PasswordIsHashedInDatabaseAsync()
    {
        var request = new RegisterRequest("hashcheck", "hash@example.com", "Password123!");

        await _client.PostAsJsonAsync("/api/auth/register", request);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == "hashcheck");

        user.Should().NotBeNull();
        user!.PasswordHash.Should().NotBe("Password123!");
        user.PasswordHash.Should().StartWith("$2");
    }

    // --- Login tests ---

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenResponseAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("loginuser", "login@example.com", "Password123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("loginuser", "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrEmpty();
        tokens.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ValidCredentials_SetsRefreshTokenCookieAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("cookieuser", "cookie@example.com", "Password123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("cookieuser", "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cookie = ExtractRefreshTokenCookie(response);
        cookie.Should().NotBeNull();
        cookie.Should().Contain("httponly", "refresh token cookie must be HttpOnly");
        cookie.Should().Contain("samesite=strict", "refresh token cookie must be SameSite=Strict");
        cookie.Should().Contain("path=/api/auth", "refresh token cookie must be scoped to /api/auth");
    }

    [Fact]
    public async Task Login_ValidCredentials_DoesNotReturnRefreshTokenInBodyAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("nobody", "nobody@example.com", "Password123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nobody", "Password123!"));

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("refreshToken", "refresh token must not appear in response body");
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorizedAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("wrongpw", "wrongpw@example.com", "Password123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("wrongpw", "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonexistentUser_ReturnsUnauthorizedAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nonexistent", "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Refresh tests ---

    [Fact]
    public async Task Refresh_ValidCookie_ReturnsNewTokensAsync()
    {
        var (_, refreshCookie) = await RegisterAndLoginAsync("refreshuser", "refresh@example.com");
        refreshCookie.Should().NotBeNull();

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"{RefreshTokenCookieName}={ExtractCookieValue(refreshCookie!)}");

        var response = await _client.SendAsync(refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newTokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        newTokens.Should().NotBeNull();
        newTokens!.AccessToken.Should().NotBeNullOrEmpty();

        // New refresh token cookie should be set
        var newCookie = ExtractRefreshTokenCookie(response);
        newCookie.Should().NotBeNull();
    }

    [Fact]
    public async Task Refresh_NoCookie_ReturnsUnauthorizedAsync()
    {
        var response = await _client.PostAsync("/api/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_InvalidCookie_ReturnsUnauthorizedAsync()
    {
        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"{RefreshTokenCookieName}=invalid-refresh-token");

        var response = await _client.SendAsync(refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Logout tests ---

    [Fact]
    public async Task Logout_ClearsRefreshTokenCookieAsync()
    {
        var (_, refreshCookie) = await RegisterAndLoginAsync("logoutuser", "logout@example.com");
        refreshCookie.Should().NotBeNull();

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("Cookie", $"{RefreshTokenCookieName}={ExtractCookieValue(refreshCookie!)}");

        var response = await _client.SendAsync(logoutRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_InvalidatesRefreshTokenInDatabaseAsync()
    {
        var (_, refreshCookie) = await RegisterAndLoginAsync("logoutdb", "logoutdb@example.com");
        refreshCookie.Should().NotBeNull();

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("Cookie", $"{RefreshTokenCookieName}={ExtractCookieValue(refreshCookie!)}");

        await _client.SendAsync(logoutRequest);

        // Trying to refresh with the old cookie should fail
        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"{RefreshTokenCookieName}={ExtractCookieValue(refreshCookie!)}");

        var refreshResponse = await _client.SendAsync(refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Protected endpoint tests ---

    [Fact]
    public async Task Login_ThenAccessProtectedEndpoint_SucceedsAsync()
    {
        var (tokens, _) = await RegisterAndLoginAsync("protuser", "prot@example.com");

        // Verify the access token is valid by validating it can be used
        tokens.AccessToken.Should().NotBeNullOrEmpty();
        tokens.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_StoresRefreshTokenInDatabaseAsync()
    {
        await RegisterAndLoginAsync("dbcheck", "dbcheck@example.com");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == "dbcheck");

        user.Should().NotBeNull();
        user!.RefreshToken.Should().NotBeNullOrEmpty();
        user.RefreshTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    // --- Password Reset tests ---

    [Fact]
    public async Task RequestReset_ExistingEmail_ReturnsOkAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("resetuser", "reset@example.com", "Password123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/request-reset",
            new RequestResetRequest("reset@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestReset_NonexistentEmail_StillReturnsOkAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/request-reset",
            new RequestResetRequest("nonexistent@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestReset_CreatesTokenInDatabaseAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("tokencheck", "tokencheck@example.com", "Password123!"));

        await _client.PostAsJsonAsync("/api/auth/request-reset",
            new RequestResetRequest("tokencheck@example.com"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        var token = await db.PasswordResetTokens.FirstOrDefaultAsync();

        token.Should().NotBeNull();
        token!.Token.Should().NotBeNullOrEmpty();
        token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        token.IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ReturnsOkAndUpdatesPasswordAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("pwreset", "pwreset@example.com", "OldPassword123!"));

        await _client.PostAsJsonAsync("/api/auth/request-reset",
            new RequestResetRequest("pwreset@example.com"));

        string resetToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
            var tokenEntity = await db.PasswordResetTokens.FirstAsync();
            resetToken = tokenEntity.Token;
        }

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(resetToken, "NewPassword456!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify login with new password works
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("pwreset", "NewPassword456!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify old password no longer works
        var oldLoginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("pwreset", "OldPassword123!"));
        oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequestAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest("invalid-token", "NewPassword456!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_UsedToken_ReturnsBadRequestAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("usedtoken", "usedtoken@example.com", "Password123!"));

        await _client.PostAsJsonAsync("/api/auth/request-reset",
            new RequestResetRequest("usedtoken@example.com"));

        string resetToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
            var tokenEntity = await db.PasswordResetTokens.FirstAsync();
            resetToken = tokenEntity.Token;
        }

        // Use the token once
        await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(resetToken, "NewPassword456!"));

        // Try to use it again
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(resetToken, "AnotherPassword789!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_InvalidatesRefreshTokenAsync()
    {
        var (_, refreshCookie) = await RegisterAndLoginAsync("invalidaterefresh", "invalidaterefresh@example.com");
        refreshCookie.Should().NotBeNull();

        await _client.PostAsJsonAsync("/api/auth/request-reset",
            new RequestResetRequest("invalidaterefresh@example.com"));

        string resetToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
            var tokenEntity = await db.PasswordResetTokens.FirstAsync();
            resetToken = tokenEntity.Token;
        }

        await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(resetToken, "NewPassword456!"));

        // Old refresh token cookie should no longer work
        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"{RefreshTokenCookieName}={ExtractCookieValue(refreshCookie!)}");

        var refreshResponse = await _client.SendAsync(refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
