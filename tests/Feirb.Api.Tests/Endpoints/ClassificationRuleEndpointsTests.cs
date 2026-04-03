using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Shared.Auth;
using Feirb.Shared.Settings;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Feirb.Api.Tests.Endpoints;

public class ClassificationRuleEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ClassificationRuleEndpointsTests()
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
    public async Task ListRules_Empty_ReturnsEmptyListAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/settings/rules");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rules = await response.Content.ReadFromJsonAsync<List<ClassificationRuleResponse>>();
        rules.Should().NotBeNull();
        rules.Should().BeEmpty();
    }

    [Fact]
    public async Task ListRules_WithRules_ReturnsSortedByCreatedAtAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        await CreateRuleAsync("Rule A");
        await CreateRuleAsync("Rule B");

        var response = await _client.GetAsync("/api/settings/rules");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rules = await response.Content.ReadFromJsonAsync<List<ClassificationRuleResponse>>();
        rules.Should().HaveCount(2);
        rules![0].CreatedAt.Should().BeOnOrBefore(rules[1].CreatedAt);
    }

    [Fact]
    public async Task ListRules_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync("/api/settings/rules");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListRules_OnlyReturnsCurrentUserRulesAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        await CreateRuleAsync("Admin rule");

        // Register and login as another user
        var registerRequest = new RegisterRequest("otheruser", "other@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("otheruser", "Password123!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var otherTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherTokens!.AccessToken);

        var response = await _client.GetAsync("/api/settings/rules");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rules = await response.Content.ReadFromJsonAsync<List<ClassificationRuleResponse>>();
        rules.Should().NotBeNull();
        rules.Should().BeEmpty();
    }

    // --- Create Tests ---

    [Fact]
    public async Task CreateRule_ValidRequest_ReturnsCreatedAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new CreateClassificationRuleRequest("Emails from newsletters should be labeled 'Newsletter'");
        var response = await _client.PostAsJsonAsync("/api/settings/rules", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rule = await response.Content.ReadFromJsonAsync<ClassificationRuleResponse>();
        rule.Should().NotBeNull();
        rule!.Instruction.Should().Be("Emails from newsletters should be labeled 'Newsletter'");
    }

    [Fact]
    public async Task CreateRule_InstructionTooLong_ReturnsBadRequestAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var longInstruction = new string('a', 501);
        var request = new CreateClassificationRuleRequest(longInstruction);
        var response = await _client.PostAsJsonAsync("/api/settings/rules", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateRule_EmptyOrWhitespaceInstruction_ReturnsBadRequestAsync(string instruction)
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new CreateClassificationRuleRequest(instruction);
        var response = await _client.PostAsJsonAsync("/api/settings/rules", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- Update Tests ---

    [Fact]
    public async Task UpdateRule_ValidRequest_ReturnsOkAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var id = await CreateRuleAsync("Old instruction");

        var updateRequest = new UpdateClassificationRuleRequest("Updated instruction");
        var response = await _client.PutAsJsonAsync($"/api/settings/rules/{id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rule = await response.Content.ReadFromJsonAsync<ClassificationRuleResponse>();
        rule!.Instruction.Should().Be("Updated instruction");
    }

    [Fact]
    public async Task UpdateRule_NonExistent_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var updateRequest = new UpdateClassificationRuleRequest("Some instruction");
        var response = await _client.PutAsJsonAsync($"/api/settings/rules/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRule_OtherUsersRule_ReturnsNotFoundAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        var id = await CreateRuleAsync("Admin rule");

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user2", "user2@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user2", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var updateRequest = new UpdateClassificationRuleRequest("Hacked");
        var response = await _client.PutAsJsonAsync($"/api/settings/rules/{id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Delete Tests ---

    [Fact]
    public async Task DeleteRule_ExistingOwned_ReturnsOkAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var id = await CreateRuleAsync("To delete");
        var response = await _client.DeleteAsync($"/api/settings/rules/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's gone
        var listResponse = await _client.GetAsync("/api/settings/rules");
        var rules = await listResponse.Content.ReadFromJsonAsync<List<ClassificationRuleResponse>>();
        rules.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteRule_NonExistent_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.DeleteAsync($"/api/settings/rules/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRule_OtherUsersRule_ReturnsNotFoundAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        var id = await CreateRuleAsync("Admin rule");

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user3", "user3@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user3", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var response = await _client.DeleteAsync($"/api/settings/rules/{id}");

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

    private async Task<Guid> CreateRuleAsync(string instruction)
    {
        var request = new CreateClassificationRuleRequest(instruction);
        var response = await _client.PostAsJsonAsync("/api/settings/rules", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rule = await response.Content.ReadFromJsonAsync<ClassificationRuleResponse>();
        return rule!.Id;
    }
}
