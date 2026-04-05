using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Api.Services;
using Feirb.Shared.Auth;
using Feirb.Shared.Mail;
using Feirb.Shared.Settings;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Feirb.Api.Tests.Endpoints;

public class ComposeEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IMailSendingService _mailSendingService;

    public ComposeEndpointsTests()
    {
        _mailSendingService = Substitute.For<IMailSendingService>();
        _mailSendingService
            .SendMailAsync(Arg.Any<Guid>(), Arg.Any<SendMailRequest>(), Arg.Any<CancellationToken>())
            .Returns("<test-message-id@test.com>");

        _factory = TestWebApplicationFactory.Create($"TestDb-{Guid.NewGuid()}")
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IMailSendingService>();
                    services.AddScoped(_ => _mailSendingService);
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
    public async Task SendMail_Unauthenticated_Returns401Async()
    {
        var request = CreateValidRequest(Guid.NewGuid());

        var response = await _client.PostAsJsonAsync("/api/mail/send", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendMail_InvalidMailbox_Returns404Async()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        _mailSendingService
            .SendMailAsync(Arg.Any<Guid>(), Arg.Any<SendMailRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Mailbox not found"));

        var request = CreateValidRequest(Guid.NewGuid());
        var response = await _client.PostAsJsonAsync("/api/mail/send", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendMail_ValidRequest_Returns200WithMessageIdAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var request = CreateValidRequest(mailboxId);

        var response = await _client.PostAsJsonAsync("/api/mail/send", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SendMailResponse>();
        result.Should().NotBeNull();
        result!.MessageId.Should().Be("<test-message-id@test.com>");
    }

    [Fact]
    public async Task SendMail_InvalidEmailFormat_Returns400Async()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var request = new SendMailRequest(
            mailboxId,
            ["not-an-email"],
            null,
            null,
            "Test Subject",
            "<p>Test body</p>",
            "html");

        var response = await _client.PostAsJsonAsync("/api/mail/send", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendMail_WithCcAndBcc_CallsServiceAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var request = new SendMailRequest(
            mailboxId,
            ["to@test.com"],
            ["cc@test.com"],
            ["bcc@test.com"],
            "Test Subject",
            "<p>Hello</p>",
            "html");

        var response = await _client.PostAsJsonAsync("/api/mail/send", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await _mailSendingService.Received(1)
            .SendMailAsync(Arg.Any<Guid>(), Arg.Any<SendMailRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMail_PlainText_Returns200Async()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var request = new SendMailRequest(
            mailboxId,
            ["to@test.com"],
            null,
            null,
            "Test Subject",
            "Plain text body",
            "plain");

        var response = await _client.PostAsJsonAsync("/api/mail/send", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendMail_EmptyToArray_Returns400Async()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var request = new SendMailRequest(
            mailboxId,
            [],
            null,
            null,
            "Test Subject",
            "<p>Body</p>",
            "html");

        var response = await _client.PostAsJsonAsync("/api/mail/send", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static SendMailRequest CreateValidRequest(Guid mailboxId) =>
        new(mailboxId, ["recipient@test.com"], null, null, "Test Subject", "<p>Test body</p>", "html");

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
        var request = new CreateMailboxRequest(name, email, null, null,
            "imap.test.com", 993, "user@test.com", "imappass", true,
            "smtp.test.com", 587, "user@test.com", "smtppass", true, true);
        var response = await _client.PostAsJsonAsync("/api/settings/mailboxes", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var mailbox = await response.Content.ReadFromJsonAsync<MailboxDetailResponse>();
        return mailbox!.Id;
    }
}
