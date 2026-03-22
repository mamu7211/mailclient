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

public class MailTestEndpointsTests : IDisposable
{
    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MailTestEndpointsTests()
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

    [Fact]
    public async Task TestImap_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var request = new TestImapRequest("imap.test.com", 993, "user", "pass", true);
        var response = await _client.PostAsJsonAsync("/api/mail/test/imap", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TestSmtp_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var request = new TestSmtpMailboxRequest("smtp.test.com", 587, "user", "pass", true, true);
        var response = await _client.PostAsJsonAsync("/api/mail/test/smtp", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TestImap_InvalidHost_ReturnsFailureAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new TestImapRequest("nonexistent.invalid.host.test", 993, null, null, false);
        var response = await _client.PostAsJsonAsync("/api/mail/test/imap", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TestConnectionResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task TestSmtp_InvalidHost_ReturnsFailureAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new TestSmtpMailboxRequest("nonexistent.invalid.host.test", 587, null, null, false, false);
        var response = await _client.PostAsJsonAsync("/api/mail/test/smtp", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TestConnectionResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
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
