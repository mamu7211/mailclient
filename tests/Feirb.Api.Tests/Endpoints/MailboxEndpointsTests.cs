using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Api.Data;
using Feirb.Shared.Auth;
using Feirb.Shared.Settings;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Feirb.Api.Tests.Endpoints;

public class MailboxEndpointsTests : IDisposable
{
    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MailboxEndpointsTests()
    {
        var dbName = _dbName;
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var dbDescriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<FeirbDbContext>) ||
                    d.ServiceType.FullName?.Contains("FeirbDbContext") == true ||
                    d.ServiceType.FullName?.Contains("Npgsql") == true).ToList();
                foreach (var d in dbDescriptors)
                    services.Remove(d);

                services.AddDbContext<FeirbDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        });
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
    public async Task ListMailboxes_Empty_ReturnsEmptyListAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/settings/mailboxes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mailboxes = await response.Content.ReadFromJsonAsync<List<MailboxListResponse>>();
        mailboxes.Should().NotBeNull();
        mailboxes.Should().BeEmpty();
    }

    [Fact]
    public async Task ListMailboxes_WithMailboxes_ReturnsSortedListAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        await CreateMailboxAsync("Zebra Mail", "zebra@test.com");
        await CreateMailboxAsync("Alpha Mail", "alpha@test.com");

        var response = await _client.GetAsync("/api/settings/mailboxes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mailboxes = await response.Content.ReadFromJsonAsync<List<MailboxListResponse>>();
        mailboxes.Should().HaveCount(2);
        mailboxes![0].Name.Should().Be("Alpha Mail");
        mailboxes[1].Name.Should().Be("Zebra Mail");
    }

    [Fact]
    public async Task ListMailboxes_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync("/api/settings/mailboxes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListMailboxes_OnlyReturnsCurrentUserMailboxesAsync()
    {
        // Setup admin and create a mailbox
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        await CreateMailboxAsync("Admin Mailbox", "admin@test.com");

        // Register and login as another user
        var registerRequest = new RegisterRequest("otheruser", "other@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("otheruser", "Password123!"));
        var otherTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherTokens!.AccessToken);

        var response = await _client.GetAsync("/api/settings/mailboxes");
        var mailboxes = await response.Content.ReadFromJsonAsync<List<MailboxListResponse>>();
        mailboxes.Should().BeEmpty();
    }

    // --- Create Tests ---

    [Fact]
    public async Task CreateMailbox_ValidRequest_ReturnsCreatedAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = CreateTestRequest("Work Mail", "work@test.com");
        var response = await _client.PostAsJsonAsync("/api/settings/mailboxes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var mailbox = await response.Content.ReadFromJsonAsync<MailboxDetailResponse>();
        mailbox.Should().NotBeNull();
        mailbox!.Name.Should().Be("Work Mail");
        mailbox.EmailAddress.Should().Be("work@test.com");
        mailbox.ImapPort.Should().Be(993);
        mailbox.SmtpPort.Should().Be(587);
    }

    // --- Get Tests ---

    [Fact]
    public async Task GetMailbox_ExistingOwned_ReturnsDetailAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var id = await CreateMailboxAsync("Test", "test@test.com");
        var response = await _client.GetAsync($"/api/settings/mailboxes/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mailbox = await response.Content.ReadFromJsonAsync<MailboxDetailResponse>();
        mailbox.Should().NotBeNull();
        mailbox!.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetMailbox_NonExistent_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync($"/api/settings/mailboxes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMailbox_OtherUsersMailbox_ReturnsNotFoundAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        var id = await CreateMailboxAsync("Admin Box", "admin@test.com");

        // Login as another user
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user2", "user2@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user2", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var response = await _client.GetAsync($"/api/settings/mailboxes/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMailbox_ResponseDoesNotContainPasswordsAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var id = await CreateMailboxAsync("Test", "test@test.com");
        var response = await _client.GetAsync($"/api/settings/mailboxes/{id}");

        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("Password");
        json.Should().NotContain("password");
        json.Should().NotContain("Encrypted");
    }

    // --- Update Tests ---

    [Fact]
    public async Task UpdateMailbox_ValidRequest_ReturnsOkAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var id = await CreateMailboxAsync("Old Name", "old@test.com");

        var updateRequest = new UpdateMailboxRequest(
            "New Name", "new@test.com", "Display",
            "imap.new.com", 993, "user", null, true,
            "smtp.new.com", 587, "user", null, true, true);
        var response = await _client.PutAsJsonAsync($"/api/settings/mailboxes/{id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mailbox = await response.Content.ReadFromJsonAsync<MailboxDetailResponse>();
        mailbox!.Name.Should().Be("New Name");
        mailbox.EmailAddress.Should().Be("new@test.com");
    }

    [Fact]
    public async Task UpdateMailbox_OtherUsersMailbox_ReturnsNotFoundAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        var id = await CreateMailboxAsync("Admin Box", "admin@test.com");

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user3", "user3@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user3", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var updateRequest = new UpdateMailboxRequest(
            "Hacked", "hacked@test.com", null,
            "imap.hack.com", 993, "user", "pass", true,
            "smtp.hack.com", 587, "user", "pass", true, true);
        var response = await _client.PutAsJsonAsync($"/api/settings/mailboxes/{id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Delete Tests ---

    [Fact]
    public async Task DeleteMailbox_ExistingOwned_ReturnsOkAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var id = await CreateMailboxAsync("To Delete", "delete@test.com");
        var response = await _client.DeleteAsync($"/api/settings/mailboxes/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's gone
        var getResponse = await _client.GetAsync($"/api/settings/mailboxes/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMailbox_OtherUsersMailbox_ReturnsNotFoundAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        var id = await CreateMailboxAsync("Admin Box", "admin@test.com");

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user4", "user4@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user4", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var response = await _client.DeleteAsync($"/api/settings/mailboxes/{id}");

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

    private static CreateMailboxRequest CreateTestRequest(string name, string email) =>
        new(name, email, null,
            "imap.test.com", 993, "user@test.com", "imappass", true,
            "smtp.test.com", 587, "user@test.com", "smtppass", true, true);

    private async Task<Guid> CreateMailboxAsync(string name, string email)
    {
        var request = CreateTestRequest(name, email);
        var response = await _client.PostAsJsonAsync("/api/settings/mailboxes", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var mailbox = await response.Content.ReadFromJsonAsync<MailboxDetailResponse>();
        return mailbox!.Id;
    }
}
