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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Feirb.Api.Tests.Endpoints;

public class MessageEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MessageEndpointsTests()
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
    public async Task ListMessages_Empty_ReturnsEmptyPaginatedResponseAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/mail/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MessageListItemResponse>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListMessages_WithMessages_ReturnsOrderedByDateDescAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        await SeedMessagesAsync(mailboxId, [
            ("Old Message", "old@test.com", DateTimeOffset.UtcNow.AddDays(-2)),
            ("New Message", "new@test.com", DateTimeOffset.UtcNow),
            ("Mid Message", "mid@test.com", DateTimeOffset.UtcNow.AddDays(-1)),
        ]);

        var response = await _client.GetAsync("/api/mail/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MessageListItemResponse>>();
        result!.Items.Should().HaveCount(3);
        result.Items[0].Subject.Should().Be("New Message");
        result.Items[1].Subject.Should().Be("Mid Message");
        result.Items[2].Subject.Should().Be("Old Message");
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task ListMessages_Pagination_ReturnsCorrectPageAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var messages = Enumerable.Range(1, 5)
            .Select(i => ($"Message {i}", $"sender{i}@test.com", DateTimeOffset.UtcNow.AddMinutes(-i)))
            .ToArray();
        await SeedMessagesAsync(mailboxId, messages);

        var response = await _client.GetAsync("/api/mail/messages?page=2&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MessageListItemResponse>>();
        result!.Items.Should().HaveCount(2);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task ListMessages_OnlyReturnsCurrentUserMessagesAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Admin Box", "admin@test.com");
        await SeedMessagesAsync(mailboxId, [("Admin Message", "someone@test.com", DateTimeOffset.UtcNow)]);

        // Login as another user
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user2", "user2@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user2", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var response = await _client.GetAsync("/api/mail/messages");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MessageListItemResponse>>();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListMessages_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync("/api/mail/messages");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMessage_Exists_ReturnsDetailWithAttachmentsAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var messageId = await SeedMessageWithAttachmentAsync(mailboxId);

        var response = await _client.GetAsync($"/api/mail/messages/{messageId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MessageDetailResponse>();
        result.Should().NotBeNull();
        result!.Subject.Should().Be("Test Subject");
        result.From.Should().Be("sender@test.com");
        result.To.Should().Be("recipient@test.com");
        result.MailboxName.Should().Be("Test");
        result.Attachments.Should().HaveCount(1);
        result.Attachments[0].Filename.Should().Be("document.pdf");
        result.Attachments[0].Size.Should().Be(1024);
        result.Attachments[0].MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task GetMessage_NotFound_Returns404Async()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync($"/api/mail/messages/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMessage_OtherUserMessage_Returns404Async()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Admin Box", "admin@test.com");
        var messageIds = await SeedMessagesAsync(mailboxId, [("Secret", "secret@test.com", DateTimeOffset.UtcNow)]);

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user3", "user3@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user3", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var response = await _client.GetAsync($"/api/mail/messages/{messageIds[0]}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMessage_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync($"/api/mail/messages/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListMessages_WithLabels_ReturnsLabelsOrderedByNameAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var messageIds = await SeedMessagesAsync(mailboxId, [("Labeled Message", "sender@test.com", DateTimeOffset.UtcNow)]);
        await SeedLabelsOnMessageAsync(messageIds[0], [("Work", "#2196F3"), ("Newsletter", "#4CAF50")]);

        var response = await _client.GetAsync("/api/mail/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MessageListItemResponse>>();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Labels.Should().HaveCount(2);
        result.Items[0].Labels[0].Name.Should().Be("Newsletter");
        result.Items[0].Labels[1].Name.Should().Be("Work");
        result.Items[0].Labels[0].Color.Should().Be("#4CAF50");
    }

    [Fact]
    public async Task ListMessages_WithoutLabels_ReturnsEmptyLabelsListAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        await SeedMessagesAsync(mailboxId, [("No Labels", "sender@test.com", DateTimeOffset.UtcNow)]);

        var response = await _client.GetAsync("/api/mail/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MessageListItemResponse>>();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Labels.Should().BeEmpty();
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

    private async Task<List<Guid>> SeedMessagesAsync(
        Guid mailboxId,
        (string Subject, string From, DateTimeOffset Date)[] messages)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        var ids = new List<Guid>();
        foreach (var (subject, from, date) in messages)
        {
            var msg = new CachedMessage
            {
                Id = Guid.NewGuid(),
                MailboxId = mailboxId,
                MessageId = $"<{Guid.NewGuid()}@test.com>",
                Subject = subject,
                From = from,
                To = "recipient@test.com",
                Date = date,
                SyncedAt = DateTimeOffset.UtcNow,
            };
            db.CachedMessages.Add(msg);
            ids.Add(msg.Id);
        }

        await db.SaveChangesAsync();
        return ids;
    }

    private async Task SeedLabelsOnMessageAsync(Guid messageId, (string Name, string Color)[] labels)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        var message = await db.CachedMessages.Include(m => m.Labels).FirstAsync(m => m.Id == messageId);
        var user = await db.Users.FirstAsync();

        foreach (var (name, color) in labels)
        {
            var label = new Label
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = name,
                Color = color,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Labels.Add(label);
            message.Labels.Add(label);
        }

        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedMessageWithAttachmentAsync(Guid mailboxId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        var msg = new CachedMessage
        {
            Id = Guid.NewGuid(),
            MailboxId = mailboxId,
            MessageId = $"<{Guid.NewGuid()}@test.com>",
            Subject = "Test Subject",
            From = "sender@test.com",
            To = "recipient@test.com",
            Date = DateTimeOffset.UtcNow,
            BodyHtml = "<p>Hello</p>",
            BodyPlainText = "Hello",
            SyncedAt = DateTimeOffset.UtcNow,
        };

        msg.Attachments.Add(new CachedAttachment
        {
            Id = Guid.NewGuid(),
            CachedMessageId = msg.Id,
            Filename = "document.pdf",
            Size = 1024,
            MimeType = "application/pdf",
        });

        db.CachedMessages.Add(msg);
        await db.SaveChangesAsync();

        return msg.Id;
    }
}
