using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Api.Data;
using Feirb.Shared.Admin;
using Feirb.Shared.Auth;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Feirb.Api.Tests.Endpoints;

public class SystemSettingsEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SystemSettingsEndpointsTests()
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

    // --- GET Tests ---

    [Fact]
    public async Task GetSmtpSettings_AsAdmin_ReturnsSettingsWithoutPasswordAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/admin/system-settings/smtp");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<SystemSmtpSettingsResponse>();
        settings.Should().NotBeNull();
        settings!.Host.Should().Be("smtp.example.com");
        settings.Port.Should().Be(587);
        settings.UseTls.Should().BeTrue();
        settings.RequiresAuth.Should().BeTrue();
        settings.Username.Should().Be("smtp@example.com");
        settings.FromAddress.Should().Be("admin@example.com");
        settings.FromName.Should().Be("admin");
    }

    [Fact]
    public async Task GetSmtpSettings_AsNonAdmin_ReturnsForbiddenAsync()
    {
        await SetupAndLoginAsAdminAsync();

        var registerRequest = new RegisterRequest("regularuser", "regular@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("regularuser", "Password123!"));
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var response = await _client.GetAsync("/api/admin/system-settings/smtp");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSmtpSettings_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync("/api/admin/system-settings/smtp");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- PUT Tests ---

    [Fact]
    public async Task UpdateSmtpSettings_AsAdmin_UpdatesSettingsAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new UpdateSystemSmtpSettingsRequest(
            "new-smtp.example.com", 465, true, false,
            "newuser", "newpass",
            "system@example.com", "Feirb System");

        var response = await _client.PutAsJsonAsync("/api/admin/system-settings/smtp", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<SystemSmtpSettingsResponse>();
        settings.Should().NotBeNull();
        settings!.Host.Should().Be("new-smtp.example.com");
        settings.Port.Should().Be(465);
        settings.UseTls.Should().BeTrue();
        settings.RequiresAuth.Should().BeFalse();
        settings.Username.Should().Be("newuser");
        settings.FromAddress.Should().Be("system@example.com");
        settings.FromName.Should().Be("Feirb System");
    }

    [Fact]
    public async Task UpdateSmtpSettings_EmptyPassword_KeepsExistingAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Update without password (should keep existing)
        var request = new UpdateSystemSmtpSettingsRequest(
            "smtp.example.com", 587, true, true,
            "smtp@example.com", null,
            "admin@example.com", "admin");

        var response = await _client.PutAsJsonAsync("/api/admin/system-settings/smtp", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify password was not cleared by checking we can still read settings
        var getResponse = await _client.GetAsync("/api/admin/system-settings/smtp");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify that encrypted password is still set in the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        var dbSettings = await db.SmtpSettings.FirstOrDefaultAsync();
        dbSettings.Should().NotBeNull();
        dbSettings!.EncryptedPassword.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateSmtpSettings_AsNonAdmin_ReturnsForbiddenAsync()
    {
        await SetupAndLoginAsAdminAsync();

        var registerRequest = new RegisterRequest("regularuser", "regular@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("regularuser", "Password123!"));
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var request = new UpdateSystemSmtpSettingsRequest(
            "hacked.example.com", 25, false, false,
            null, null, null, null);

        var response = await _client.PutAsJsonAsync("/api/admin/system-settings/smtp", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
