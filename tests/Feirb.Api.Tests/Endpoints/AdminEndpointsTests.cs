using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Shared.Admin;
using Feirb.Shared.Auth;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Feirb.Api.Tests.Endpoints;

public class AdminEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AdminEndpointsTests()
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
    public async Task GetUsers_AsAdmin_ReturnsOkAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<AdminUserResponse>>();
        users.Should().NotBeNull();
        users.Should().ContainSingle(u => u.IsAdmin);
    }

    [Fact]
    public async Task GetUsers_AsNonAdmin_ReturnsForbiddenAsync()
    {
        // First create admin via setup so registration works
        await SetupAndLoginAsAdminAsync();

        // Register and login as non-admin user
        var registerRequest = new RegisterRequest("regularuser", "regular@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("regularuser", "Password123!"));
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var response = await _client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Create User Tests ---

    [Fact]
    public async Task CreateUser_AsAdmin_ReturnsCreatedAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new CreateUserRequest("newuser", "newuser@example.com", "Password123!", false);
        var response = await _client.PostAsJsonAsync("/api/admin/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<AdminUserResponse>();
        user.Should().NotBeNull();
        user!.Username.Should().Be("newuser");
        user.Email.Should().Be("newuser@example.com");
        user.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ReturnsConflictAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new CreateUserRequest("admin", "other@example.com", "Password123!", false);
        var response = await _client.PostAsJsonAsync("/api/admin/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ReturnsConflictAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new CreateUserRequest("uniqueuser", "admin@example.com", "Password123!", false);
        var response = await _client.PostAsJsonAsync("/api/admin/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateUser_AsNonAdmin_ReturnsForbiddenAsync()
    {
        await SetupAndLoginAsAdminAsync();

        var registerRequest = new RegisterRequest("regularuser", "regular@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("regularuser", "Password123!"));
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var request = new CreateUserRequest("anotheruser", "another@example.com", "Password123!", false);
        var response = await _client.PostAsJsonAsync("/api/admin/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Update User Tests ---

    [Fact]
    public async Task UpdateUser_AsAdmin_ReturnsOkAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Create a user to update
        var createRequest = new CreateUserRequest("edituser", "edituser@example.com", "Password123!", false);
        var createResponse = await _client.PostAsJsonAsync("/api/admin/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<AdminUserResponse>();

        var updateRequest = new UpdateUserRequest("newemail@example.com", true);
        var response = await _client.PutAsJsonAsync($"/api/admin/users/{createdUser!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedUser = await response.Content.ReadFromJsonAsync<AdminUserResponse>();
        updatedUser!.Email.Should().Be("newemail@example.com");
        updatedUser.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUser_DemoteSelf_ReturnsBadRequestAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Get the admin user's ID
        var usersResponse = await _client.GetAsync("/api/admin/users");
        var users = await usersResponse.Content.ReadFromJsonAsync<List<AdminUserResponse>>();
        var adminUser = users!.First(u => u.IsAdmin);

        var updateRequest = new UpdateUserRequest(adminUser.Email, false);
        var response = await _client.PutAsJsonAsync($"/api/admin/users/{adminUser.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUser_DemoteLastAdmin_ReturnsBadRequestAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Create a second admin, login as them, try to demote the first admin
        var createRequest = new CreateUserRequest("admin2", "admin2@example.com", "Password123!", true);
        await _client.PostAsJsonAsync("/api/admin/users", createRequest);

        // Login as admin2
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin2", "Password123!"));
        var admin2Tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin2Tokens!.AccessToken);

        // Demote admin (the first one) - should succeed since there are 2 admins
        var usersResponse = await _client.GetAsync("/api/admin/users");
        var users = await usersResponse.Content.ReadFromJsonAsync<List<AdminUserResponse>>();
        var firstAdmin = users!.First(u => u.Username == "admin");

        var updateRequest = new UpdateUserRequest(firstAdmin.Email, false);
        var response = await _client.PutAsJsonAsync($"/api/admin/users/{firstAdmin.Id}", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Now try to demote admin2 (last remaining admin) via the demoted admin1 - can't, they're not admin
        // Instead: try self-demotion as admin2 (last admin)
        var selfDemoteRequest = new UpdateUserRequest("admin2@example.com", false);
        var admin2User = users!.First(u => u.Username == "admin2");
        var selfResponse = await _client.PutAsJsonAsync($"/api/admin/users/{admin2User.Id}", selfDemoteRequest);

        selfResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUser_NonExistentUser_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var updateRequest = new UpdateUserRequest("email@example.com", false);
        var response = await _client.PutAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Delete User Tests ---

    [Fact]
    public async Task DeleteUser_AsAdmin_ReturnsOkAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Create a user to delete
        var createRequest = new CreateUserRequest("deleteuser", "deleteuser@example.com", "Password123!", false);
        var createResponse = await _client.PostAsJsonAsync("/api/admin/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<AdminUserResponse>();

        var response = await _client.DeleteAsync($"/api/admin/users/{createdUser!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user is gone
        var usersResponse = await _client.GetAsync("/api/admin/users");
        var users = await usersResponse.Content.ReadFromJsonAsync<List<AdminUserResponse>>();
        users.Should().NotContain(u => u.Username == "deleteuser");
    }

    [Fact]
    public async Task DeleteUser_Self_ReturnsBadRequestAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Get the admin user's ID
        var usersResponse = await _client.GetAsync("/api/admin/users");
        var users = await usersResponse.Content.ReadFromJsonAsync<List<AdminUserResponse>>();
        var adminUser = users!.First(u => u.IsAdmin);

        var response = await _client.DeleteAsync($"/api/admin/users/{adminUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteUser_NonExistentUser_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.DeleteAsync($"/api/admin/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Reset Password Tests ---

    [Fact]
    public async Task ResetPassword_AsAdmin_ReturnsOkAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Create a user to reset
        var createRequest = new CreateUserRequest("resetuser", "resetuser@example.com", "Password123!", false);
        var createResponse = await _client.PostAsJsonAsync("/api/admin/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<AdminUserResponse>();

        var response = await _client.PostAsync($"/api/admin/users/{createdUser!.Id}/reset-password", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResetPasswordResponse>();
        result.Should().NotBeNull();
        result!.ResetToken.Should().NotBeNullOrEmpty();
        result.ResetLink.Should().Contain("/reset-password/");
        result.ResetLink.Should().Contain(result.ResetToken);
    }

    [Fact]
    public async Task ResetPassword_NonExistentUser_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.PostAsync($"/api/admin/users/{Guid.NewGuid()}/reset-password", null);

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
}
