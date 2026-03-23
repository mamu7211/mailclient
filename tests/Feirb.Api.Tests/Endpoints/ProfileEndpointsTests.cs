using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Shared.Auth;
using Feirb.Shared.Settings;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Feirb.Api.Tests.Endpoints;

public class ProfileEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProfileEndpointsTests()
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

    // --- Get Profile Tests ---

    [Fact]
    public async Task GetProfile_Authenticated_ReturnsProfileAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/settings/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Username.Should().Be("admin");
        profile.Email.Should().Be("admin@example.com");
    }

    [Fact]
    public async Task GetProfile_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync("/api/settings/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Update Profile Tests ---

    [Fact]
    public async Task UpdateProfile_ChangeUsername_ReturnsUpdatedProfileAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new UpdateProfileRequest("newadmin", null);
        var response = await _client.PutAsJsonAsync("/api/settings/profile", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        profile!.Username.Should().Be("newadmin");
        profile.Email.Should().Be("admin@example.com");
    }

    [Fact]
    public async Task UpdateProfile_ChangeEmail_ReturnsUpdatedProfileAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new UpdateProfileRequest(null, "newemail@example.com");
        var response = await _client.PutAsJsonAsync("/api/settings/profile", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        profile!.Username.Should().Be("admin");
        profile.Email.Should().Be("newemail@example.com");
    }

    [Fact]
    public async Task UpdateProfile_DuplicateUsername_ReturnsConflictAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Register another user
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("otheruser", "other@example.com", "Password123!"));

        var request = new UpdateProfileRequest("otheruser", null);
        var response = await _client.PutAsJsonAsync("/api/settings/profile", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateProfile_DuplicateEmail_ReturnsConflictAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("otheruser", "other@example.com", "Password123!"));

        var request = new UpdateProfileRequest(null, "other@example.com");
        var response = await _client.PutAsJsonAsync("/api/settings/profile", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- Change Password Tests ---

    [Fact]
    public async Task ChangePassword_ValidCurrentPassword_ReturnsOkAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new ChangePasswordRequest("AdminPassword123!", "NewPassword456!");
        var response = await _client.PutAsJsonAsync("/api/settings/profile/password", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify can login with new password
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin", "NewPassword456!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequestAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new ChangePasswordRequest("WrongPassword!", "NewPassword456!");
        var response = await _client.PutAsJsonAsync("/api/settings/profile/password", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- Logout All Tests ---

    [Fact]
    public async Task LogoutAll_Authenticated_InvalidatesRefreshTokenAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.PostAsync("/api/settings/profile/logout-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify refresh token is invalidated
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(tokens.RefreshToken));
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAll_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.PostAsync("/api/settings/profile/logout-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Helper Methods ---

    private async Task<TokenResponse> SetupAndLoginAsAdminAsync()
    {
        var setupRequest = new CompleteSetupRequest(
            "admin", "admin@example.com", "AdminPassword123!",
            "smtp.example.com", 587, "smtp@example.com", "smtppass", true, true);
        await _client.PostAsJsonAsync("/api/setup/complete", setupRequest);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin", "AdminPassword123!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        tokens.Should().NotBeNull();
        return tokens!;
    }
}
