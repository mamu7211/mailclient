using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Shared.Auth;
using Feirb.Shared.Settings;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Feirb.Api.Tests.Endpoints;

public class LabelEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LabelEndpointsTests()
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

    // --- List Tests ---

    [Fact]
    public async Task ListLabels_Empty_ReturnsEmptyListAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/settings/labels");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var labels = await response.Content.ReadFromJsonAsync<List<LabelResponse>>();
        labels.Should().NotBeNull();
        labels.Should().BeEmpty();
    }

    [Fact]
    public async Task ListLabels_WithLabels_ReturnsSortedListAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        await CreateLabelAsync("Zebra", "#FF0000");
        await CreateLabelAsync("Alpha", "#00FF00");

        var response = await _client.GetAsync("/api/settings/labels");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var labels = await response.Content.ReadFromJsonAsync<List<LabelResponse>>();
        labels.Should().HaveCount(2);
        labels![0].Name.Should().Be("Alpha");
        labels[1].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task ListLabels_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync("/api/settings/labels");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListLabels_OnlyReturnsCurrentUserLabelsAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        await CreateLabelAsync("Admin Label", "#FF0000");

        // Register and login as another user
        var registerRequest = new RegisterRequest("otheruser", "other@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("otheruser", "Password123!"));
        var otherTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherTokens!.AccessToken);

        var response = await _client.GetAsync("/api/settings/labels");
        var labels = await response.Content.ReadFromJsonAsync<List<LabelResponse>>();
        labels.Should().BeEmpty();
    }

    // --- Create Tests ---

    [Fact]
    public async Task CreateLabel_ValidRequest_ReturnsCreatedAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new CreateLabelRequest("Work", "#FF0000", "Work emails");
        var response = await _client.PostAsJsonAsync("/api/settings/labels", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var label = await response.Content.ReadFromJsonAsync<LabelResponse>();
        label.Should().NotBeNull();
        label!.Name.Should().Be("Work");
        label.Color.Should().Be("#FF0000");
        label.Description.Should().Be("Work emails");
    }

    [Fact]
    public async Task CreateLabel_NoColor_GeneratesRandomColorAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new CreateLabelRequest("NoColor", null, null);
        var response = await _client.PostAsJsonAsync("/api/settings/labels", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var label = await response.Content.ReadFromJsonAsync<LabelResponse>();
        label!.Color.Should().NotBeNullOrEmpty();
        label.Color.Should().StartWith("#");
        label.Color.Should().HaveLength(7);
    }

    // --- Update Tests ---

    [Fact]
    public async Task UpdateLabel_ValidRequest_ReturnsOkAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var id = await CreateLabelAsync("Old Name", "#FF0000");

        var updateRequest = new UpdateLabelRequest("New Name", "#00FF00", "Updated description");
        var response = await _client.PutAsJsonAsync($"/api/settings/labels/{id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var label = await response.Content.ReadFromJsonAsync<LabelResponse>();
        label!.Name.Should().Be("New Name");
        label.Color.Should().Be("#00FF00");
        label.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateLabel_NonExistent_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var updateRequest = new UpdateLabelRequest("Name", "#FF0000", null);
        var response = await _client.PutAsJsonAsync($"/api/settings/labels/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateLabel_OtherUsersLabel_ReturnsNotFoundAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        var id = await CreateLabelAsync("Admin Label", "#FF0000");

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user2", "user2@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user2", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var updateRequest = new UpdateLabelRequest("Hacked", "#000000", null);
        var response = await _client.PutAsJsonAsync($"/api/settings/labels/{id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Delete Tests ---

    [Fact]
    public async Task DeleteLabel_ExistingOwned_ReturnsOkAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var id = await CreateLabelAsync("To Delete", "#FF0000");
        var response = await _client.DeleteAsync($"/api/settings/labels/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's gone
        var listResponse = await _client.GetAsync("/api/settings/labels");
        var labels = await listResponse.Content.ReadFromJsonAsync<List<LabelResponse>>();
        labels.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteLabel_NonExistent_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.DeleteAsync($"/api/settings/labels/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLabel_OtherUsersLabel_ReturnsNotFoundAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        var id = await CreateLabelAsync("Admin Label", "#FF0000");

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user3", "user3@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user3", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var response = await _client.DeleteAsync($"/api/settings/labels/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private async Task<Guid> CreateLabelAsync(string name, string color)
    {
        var request = new CreateLabelRequest(name, color, null);
        var response = await _client.PostAsJsonAsync("/api/settings/labels", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var label = await response.Content.ReadFromJsonAsync<LabelResponse>();
        return label!.Id;
    }
}
