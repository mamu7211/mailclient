using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Shared.Auth;
using Feirb.Shared.Dashboard;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Feirb.Api.Tests.Endpoints;

public class WidgetConfigEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WidgetConfigEndpointsTests()
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

    [Fact]
    public async Task CreateAndGetConfig_RoundTrip_ReturnsConfigAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/dashboard/widgets/mails-per-day-abc/config",
            new WidgetConfigRequest("30"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await _client.GetAsync("/api/dashboard/widgets/mails-per-day-abc/config");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await getResponse.Content.ReadFromJsonAsync<WidgetConfigResponse>();
        result!.ConfigValue.Should().Be("30");
    }

    [Fact]
    public async Task UpdateConfig_ExistingConfig_ReturnsUpdatedValueAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        await _client.PostAsJsonAsync(
            "/api/dashboard/widgets/mails-per-day-abc/config",
            new WidgetConfigRequest("7"));

        var putResponse = await _client.PutAsJsonAsync(
            "/api/dashboard/widgets/mails-per-day-abc/config",
            new WidgetConfigRequest("90"));
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync("/api/dashboard/widgets/mails-per-day-abc/config");
        var result = await getResponse.Content.ReadFromJsonAsync<WidgetConfigResponse>();
        result!.ConfigValue.Should().Be("90");
    }

    [Fact]
    public async Task DeleteConfig_ExistingConfig_ReturnsNoContentAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        await _client.PostAsJsonAsync(
            "/api/dashboard/widgets/mails-per-day-abc/config",
            new WidgetConfigRequest("7"));

        var deleteResponse = await _client.DeleteAsync("/api/dashboard/widgets/mails-per-day-abc/config");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync("/api/dashboard/widgets/mails-per-day-abc/config");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConfig_NonExistent_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/dashboard/widgets/does-not-exist/config");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateConfig_Duplicate_ReturnsConflictAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        await _client.PostAsJsonAsync(
            "/api/dashboard/widgets/mails-per-day-abc/config",
            new WidgetConfigRequest("7"));

        var duplicateResponse = await _client.PostAsJsonAsync(
            "/api/dashboard/widgets/mails-per-day-abc/config",
            new WidgetConfigRequest("30"));

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetConfig_UserIsolation_UsersCannotSeeEachOthersConfigAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);

        await _client.PostAsJsonAsync(
            "/api/dashboard/widgets/mails-per-day-abc/config",
            new WidgetConfigRequest("30"));

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user2", "user2@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user2", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var getResponse = await _client.GetAsync("/api/dashboard/widgets/mails-per-day-abc/config");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConfig_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync("/api/dashboard/widgets/test/config");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutConfig_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/dashboard/widgets/test/config",
            new WidgetConfigRequest("7"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

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
