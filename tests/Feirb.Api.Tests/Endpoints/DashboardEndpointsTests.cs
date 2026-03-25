using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Shared.Auth;
using Feirb.Shared.Dashboard;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Feirb.Api.Tests.Endpoints;

public class DashboardEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DashboardEndpointsTests()
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
    public async Task GetLayout_NoSavedLayout_ReturnsEmptyArrayAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/dashboard/layout");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardLayoutResponse>();
        result.Should().NotBeNull();
        result!.LayoutJson.Should().Be("[]");
    }

    [Fact]
    public async Task PutLayout_SavesAndReturnsLayout_RoundTripAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var layoutJson = """[{"id":"mail-count-abc","x":0,"y":0,"w":3,"h":2}]""";
        var putResponse = await _client.PutAsJsonAsync("/api/dashboard/layout", new DashboardLayoutRequest(layoutJson));
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync("/api/dashboard/layout");
        var result = await getResponse.Content.ReadFromJsonAsync<DashboardLayoutResponse>();
        result!.LayoutJson.Should().Be(layoutJson);
    }

    [Fact]
    public async Task PutLayout_UpdateExisting_OverwritesPreviousAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        await _client.PutAsJsonAsync("/api/dashboard/layout",
            new DashboardLayoutRequest("""[{"id":"old","x":0,"y":0,"w":3,"h":2}]"""));

        var newLayout = """[{"id":"new","x":1,"y":1,"w":4,"h":3}]""";
        await _client.PutAsJsonAsync("/api/dashboard/layout", new DashboardLayoutRequest(newLayout));

        var getResponse = await _client.GetAsync("/api/dashboard/layout");
        var result = await getResponse.Content.ReadFromJsonAsync<DashboardLayoutResponse>();
        result!.LayoutJson.Should().Be(newLayout);
    }

    [Fact]
    public async Task GetLayout_UserIsolation_UsersCannotSeeEachOthersLayoutAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);

        var adminLayout = """[{"id":"admin-widget","x":0,"y":0,"w":3,"h":2}]""";
        await _client.PutAsJsonAsync("/api/dashboard/layout", new DashboardLayoutRequest(adminLayout));

        // Register and login as another user
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user2", "user2@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user2", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var getResponse = await _client.GetAsync("/api/dashboard/layout");
        var result = await getResponse.Content.ReadFromJsonAsync<DashboardLayoutResponse>();
        result!.LayoutJson.Should().Be("[]");
    }

    [Fact]
    public async Task GetLayout_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync("/api/dashboard/layout");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutLayout_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.PutAsJsonAsync("/api/dashboard/layout",
            new DashboardLayoutRequest("[]"));

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
