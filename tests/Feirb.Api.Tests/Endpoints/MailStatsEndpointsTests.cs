using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Shared.Auth;
using Feirb.Shared.Mail;
using Feirb.Shared.Settings;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Feirb.Api.Tests.Endpoints;

public class MailStatsEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MailStatsEndpointsTests()
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
    public async Task GetStats_NoMessages_ReturnsZeroTotalAndSevenDaysAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/mail/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MailStatsResponse>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(0);
        result.MailsPerDay.Should().HaveCount(7);
        result.MailsPerDay.Should().OnlyContain(d => d.Count == 0);
    }

    [Fact]
    public async Task GetStats_WithMessages_ReturnsCorrectTotalCountAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        await SeedMessagesAsync(mailboxId, [
            ("Message 1", "a@test.com", DateTimeOffset.UtcNow),
            ("Message 2", "b@test.com", DateTimeOffset.UtcNow.AddDays(-1)),
            ("Message 3", "c@test.com", DateTimeOffset.UtcNow.AddDays(-10)),
        ]);

        var response = await _client.GetAsync("/api/mail/stats");

        var result = await response.Content.ReadFromJsonAsync<MailStatsResponse>();
        result!.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetStats_WithMessages_ReturnsSevenDaysOfDataAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        await SeedMessagesAsync(mailboxId, [
            ("Today 1", "a@test.com", DateTimeOffset.UtcNow),
            ("Today 2", "b@test.com", DateTimeOffset.UtcNow),
            ("Yesterday", "c@test.com", DateTimeOffset.UtcNow.AddDays(-1)),
        ]);

        var response = await _client.GetAsync("/api/mail/stats");

        var result = await response.Content.ReadFromJsonAsync<MailStatsResponse>();
        result!.MailsPerDay.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetStats_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync("/api/mail/stats");

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

    private async Task<Guid> CreateMailboxAsync(string name, string email)
    {
        var request = new CreateMailboxRequest(name, email, null,
            "imap.test.com", 993, "user@test.com", "imappass", true,
            "smtp.test.com", 587, "user@test.com", "smtppass", true, true);
        var response = await _client.PostAsJsonAsync("/api/settings/mailboxes", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var mailbox = await response.Content.ReadFromJsonAsync<MailboxDetailResponse>();
        return mailbox!.Id;
    }

    private async Task SeedMessagesAsync(
        Guid mailboxId,
        (string Subject, string From, DateTimeOffset Date)[] messages)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        foreach (var (subject, from, date) in messages)
        {
            db.CachedMessages.Add(new CachedMessage
            {
                Id = Guid.NewGuid(),
                MailboxId = mailboxId,
                MessageId = $"<{Guid.NewGuid()}@test.com>",
                Subject = subject,
                From = from,
                To = "recipient@test.com",
                Date = date,
                SyncedAt = DateTimeOffset.UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }
}
