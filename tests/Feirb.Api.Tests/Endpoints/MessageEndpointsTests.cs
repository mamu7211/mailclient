using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Services;
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
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public MessageEndpointsTests()
    {
        _factory = TestWebApplicationFactory.Create($"TestDb-{Guid.NewGuid()}");
        _client = _factory.CreateClient();
    }

    private void RecreateFactoryWith(IClassificationService overrideService)
    {
        _client.Dispose();
        _factory.Dispose();
        _factory = TestWebApplicationFactory.Create($"TestDb-{Guid.NewGuid()}", overrideService);
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
        var result = await response.Content.ReadFromJsonAsync<MessageListResponse>();
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
        var result = await response.Content.ReadFromJsonAsync<MessageListResponse>();
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
        var result = await response.Content.ReadFromJsonAsync<MessageListResponse>();
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
        var result = await response.Content.ReadFromJsonAsync<MessageListResponse>();
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
        var result = await response.Content.ReadFromJsonAsync<MessageListResponse>();
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
        var result = await response.Content.ReadFromJsonAsync<MessageListResponse>();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Labels.Should().BeEmpty();
    }

    [Fact]
    public async Task ListMessages_NoQueueOrResult_ReturnsNotClassifiedAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        await SeedMessagesAsync(mailboxId, [("Message", "sender@test.com", DateTimeOffset.UtcNow)]);

        var response = await _client.GetAsync("/api/mail/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MessageListResponse>();
        result!.Items[0].ClassificationStatus.Should().Be(ClassificationStatus.NotClassified);
        result.PendingCount.Should().Be(0);
        result.JobPaused.Should().BeTrue();
    }

    [Fact]
    public async Task ListMessages_WithQueueAndResult_ReturnsCorrectStatusesAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var messageIds = await SeedMessagesAsync(mailboxId, [
            ("Pending", "sender@test.com", DateTimeOffset.UtcNow.AddMinutes(-5)),
            ("Processing", "sender@test.com", DateTimeOffset.UtcNow.AddMinutes(-4)),
            ("Failed", "sender@test.com", DateTimeOffset.UtcNow.AddMinutes(-3)),
            ("Classified", "sender@test.com", DateTimeOffset.UtcNow.AddMinutes(-2)),
            ("NotClassified", "sender@test.com", DateTimeOffset.UtcNow.AddMinutes(-1)),
        ]);

        await SeedQueueItemAsync(messageIds[0], ClassificationQueueItemStatus.Pending);
        await SeedQueueItemAsync(messageIds[1], ClassificationQueueItemStatus.Processing);
        await SeedQueueItemAsync(messageIds[2], ClassificationQueueItemStatus.Failed);
        await SeedClassificationResultAsync(messageIds[3]);

        var response = await _client.GetAsync("/api/mail/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MessageListResponse>();
        var bySubject = result!.Items.ToDictionary(i => i.Subject, i => i.ClassificationStatus);
        bySubject["Pending"].Should().Be(ClassificationStatus.Pending);
        bySubject["Processing"].Should().Be(ClassificationStatus.Processing);
        bySubject["Failed"].Should().Be(ClassificationStatus.Failed);
        bySubject["Classified"].Should().Be(ClassificationStatus.Classified);
        bySubject["NotClassified"].Should().Be(ClassificationStatus.NotClassified);
        result.PendingCount.Should().Be(2);
    }

    [Fact]
    public async Task ListMessages_PendingCount_ExcludesOtherUsersAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);

        var adminMailboxId = await CreateMailboxAsync("Admin", "admin@test.com");
        var adminMessageIds = await SeedMessagesAsync(adminMailboxId, [("Admin", "admin@test.com", DateTimeOffset.UtcNow)]);
        await SeedQueueItemAsync(adminMessageIds[0], ClassificationQueueItemStatus.Pending);

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user2", "user2@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user2", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var response = await _client.GetAsync("/api/mail/messages");
        var result = await response.Content.ReadFromJsonAsync<MessageListResponse>();
        result!.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task ListMessages_JobEnabled_ReturnsJobPausedFalseAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
            var job = await db.JobSettings.FirstAsync(j => j.JobName == "Classification");
            job.Enabled = true;
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/mail/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MessageListResponse>();
        result!.JobPaused.Should().BeFalse();
    }

    // --- Helper Methods ---

    private async Task<TokenResponse> SetupAndLoginAsAdminAsync()
    {
        var setupRequest = new CompleteSetupRequest(
            "admin", "admin@example.com", "AdminPassword123!",
            "smtp.example.com", 587, "smtp@example.com", "smtppass", TlsMode.Auto, true);
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
            "imap.test.com", 993, "user@test.com", "imappass", TlsMode.Auto,
            "smtp.test.com", 587, "user@test.com", "smtppass", TlsMode.Auto, true);
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

    private async Task SeedQueueItemAsync(Guid messageId, ClassificationQueueItemStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        db.ClassificationQueueItems.Add(new CachedMessageClassificationQueueItem
        {
            Id = Guid.NewGuid(),
            CachedMessageId = messageId,
            Status = status,
            Ordinal = 0,
            AttemptNumber = 0,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedClassificationResultAsync(Guid messageId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        db.ClassificationResults.Add(new ClassificationResult
        {
            Id = Guid.NewGuid(),
            CachedMessageId = messageId,
            Result = "{}",
            ClassifiedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    // --- Classify endpoint tests ---

    [Fact]
    public async Task ClassifyMessage_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.PostAsync($"/api/mail/messages/{Guid.NewGuid()}/classify", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClassifyMessage_NotFound_Returns404Async()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.PostAsync($"/api/mail/messages/{Guid.NewGuid()}/classify?dryRun=true", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClassifyMessage_OtherUserMessage_Returns404Async()
    {
        var stub = new StubClassificationService(
            new ClassificationDetailedResult(true, """["work"]""", null,
                new ClassificationPrompt("sys", "user"), """["work"]"""));
        RecreateFactoryWith(stub);

        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Admin Box", "admin@test.com");
        var ids = await SeedMessagesAsync(mailboxId, [("Hello", "from@test.com", DateTimeOffset.UtcNow)]);

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("user4", "user4@test.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user4", "Password123!"));
        var userTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens!.AccessToken);

        var response = await _client.PostAsync($"/api/mail/messages/{ids[0]}/classify?dryRun=true", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClassifyMessage_DryRunDefault_ReturnsPreviewWithPromptAndRawResponseAsync()
    {
        var stub = new StubClassificationService(
            new ClassificationDetailedResult(
                Success: true,
                Result: """["work","newsletter"]""",
                Error: null,
                Prompt: new ClassificationPrompt("system-prompt", "user-prompt"),
                RawResponse: """["work","newsletter"]"""));
        RecreateFactoryWith(stub);

        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var ids = await SeedMessagesAsync(mailboxId, [("Subject", "from@test.com", DateTimeOffset.UtcNow)]);

        var response = await _client.PostAsync($"/api/mail/messages/{ids[0]}/classify", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClassifyMessageResponse>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Applied.Should().BeFalse();
        body.Labels.Should().BeEquivalentTo(new[] { "work", "newsletter" });
        body.Error.Should().BeNull();
        body.Prompt.Should().NotBeNull();
        body.Prompt!.System.Should().Be("system-prompt");
        body.Prompt.User.Should().Be("user-prompt");
        body.RawResponse.Should().Be("""["work","newsletter"]""");

        // No ClassificationResult should have been persisted
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        (await db.ClassificationResults.AnyAsync(r => r.CachedMessageId == ids[0])).Should().BeFalse();
    }

    [Fact]
    public async Task ClassifyMessage_ApplyMode_AppliesLabelsAndPersistsResultAsync()
    {
        var stub = new StubClassificationService(
            new ClassificationDetailedResult(
                Success: true,
                Result: """["work"]""",
                Error: null,
                Prompt: new ClassificationPrompt("sys", "user"),
                RawResponse: """["work"]"""));
        RecreateFactoryWith(stub);

        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var ids = await SeedMessagesAsync(mailboxId, [("Subject", "from@test.com", DateTimeOffset.UtcNow)]);
        await SeedLabelsForCurrentUserAsync(("work", "#FF0000"), ("personal", "#00FF00"));

        var response = await _client.PostAsync($"/api/mail/messages/{ids[0]}/classify?dryRun=false", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClassifyMessageResponse>();
        body!.Success.Should().BeTrue();
        body.Applied.Should().BeTrue();
        body.Labels.Should().BeEquivalentTo(new[] { "work" });
        body.Prompt.Should().BeNull();
        body.RawResponse.Should().BeNull();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        var msg = await db.CachedMessages.Include(m => m.Labels).FirstAsync(m => m.Id == ids[0]);
        msg.Labels.Should().ContainSingle().Which.Name.Should().Be("work");
        (await db.ClassificationResults.AnyAsync(r => r.CachedMessageId == ids[0])).Should().BeTrue();
    }

    [Fact]
    public async Task ClassifyMessage_ApplyMode_RemovesQueueItemAsync()
    {
        var stub = new StubClassificationService(
            new ClassificationDetailedResult(true, """["work"]""", null,
                new ClassificationPrompt("sys", "user"), """["work"]"""));
        RecreateFactoryWith(stub);

        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var ids = await SeedMessagesAsync(mailboxId, [("Subject", "from@test.com", DateTimeOffset.UtcNow)]);
        await SeedLabelsForCurrentUserAsync(("work", "#FF0000"));
        await SeedQueueItemAsync(ids[0], ClassificationQueueItemStatus.Failed);

        var response = await _client.PostAsync($"/api/mail/messages/{ids[0]}/classify?dryRun=false", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        (await db.ClassificationQueueItems.AnyAsync(q => q.CachedMessageId == ids[0])).Should().BeFalse();
    }

    [Fact]
    public async Task ClassifyMessage_NoRulesOrLabels_ReturnsEmptyLabelsAsync()
    {
        var stub = new StubClassificationService(ClassificationDetailedResult.Skipped);
        RecreateFactoryWith(stub);

        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var ids = await SeedMessagesAsync(mailboxId, [("Subject", "from@test.com", DateTimeOffset.UtcNow)]);

        var response = await _client.PostAsync($"/api/mail/messages/{ids[0]}/classify?dryRun=true", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClassifyMessageResponse>();
        body!.Success.Should().BeTrue();
        body.Labels.Should().BeEmpty();
        body.Applied.Should().BeFalse();
    }

    [Fact]
    public async Task ClassifyMessage_AiUnavailable_ReturnsFailureAsync()
    {
        // Backend unavailable (HttpRequestException / TaskCanceledException) must be
        // distinguishable from "no rules configured" — surfaced as success=false with
        // a localized error so the UI can show an error instead of help text.
        var stub = new StubClassificationService(
            ClassificationDetailedResult.BackendUnavailable("Classification service unavailable."));
        RecreateFactoryWith(stub);

        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var ids = await SeedMessagesAsync(mailboxId, [("Subject", "from@test.com", DateTimeOffset.UtcNow)]);

        var response = await _client.PostAsync($"/api/mail/messages/{ids[0]}/classify?dryRun=false", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClassifyMessageResponse>();
        body!.Success.Should().BeFalse();
        body.Labels.Should().BeEmpty();
        body.Applied.Should().BeFalse();
        body.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ClassifyMessage_HardFailure_ReturnsErrorAsync()
    {
        var stub = new StubClassificationService(
            new ClassificationDetailedResult(
                Success: false,
                Result: null,
                Error: "Failed to parse LLM response",
                Prompt: new ClassificationPrompt("sys", "user"),
                RawResponse: "garbage"));
        RecreateFactoryWith(stub);

        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var ids = await SeedMessagesAsync(mailboxId, [("Subject", "from@test.com", DateTimeOffset.UtcNow)]);

        var response = await _client.PostAsync($"/api/mail/messages/{ids[0]}/classify?dryRun=true", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClassifyMessageResponse>();
        body!.Success.Should().BeFalse();
        body.Error.Should().Contain("Failed to parse");
        body.Labels.Should().BeEmpty();
        body.Prompt.Should().NotBeNull();
        body.RawResponse.Should().Be("garbage");
    }

    [Fact]
    public async Task ClassifyMessage_AppliesLabelsAdditively_WhenAlreadyLabeledAsync()
    {
        var stub = new StubClassificationService(
            new ClassificationDetailedResult(true, """["work"]""", null,
                new ClassificationPrompt("sys", "user"), """["work"]"""));
        RecreateFactoryWith(stub);

        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var mailboxId = await CreateMailboxAsync("Test", "test@test.com");
        var ids = await SeedMessagesAsync(mailboxId, [("Subject", "from@test.com", DateTimeOffset.UtcNow)]);
        await SeedLabelsForCurrentUserAsync(("work", "#FF0000"), ("existing", "#000000"));
        await SeedLabelsOnMessageByNameAsync(ids[0], "existing");

        var response = await _client.PostAsync($"/api/mail/messages/{ids[0]}/classify?dryRun=false", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        var msg = await db.CachedMessages.Include(m => m.Labels).FirstAsync(m => m.Id == ids[0]);
        msg.Labels.Select(l => l.Name).Should().BeEquivalentTo(new[] { "work", "existing" });
    }

    private async Task SeedLabelsForCurrentUserAsync(params (string Name, string Color)[] labels)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        var user = await db.Users.FirstAsync();
        foreach (var (name, color) in labels)
        {
            db.Labels.Add(new Label
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = name,
                Color = color,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedLabelsOnMessageByNameAsync(Guid messageId, string labelName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        var message = await db.CachedMessages.Include(m => m.Labels).FirstAsync(m => m.Id == messageId);
        var label = await db.Labels.FirstAsync(l => l.Name == labelName);
        message.Labels.Add(label);
        await db.SaveChangesAsync();
    }

    private sealed class StubClassificationService(ClassificationDetailedResult result) : IClassificationService
    {
        public Task<ClassificationServiceResult> ClassifyAsync(CachedMessage message, CancellationToken cancellationToken = default) =>
            Task.FromResult(result.IsSkipped
                ? ClassificationServiceResult.Skipped
                : new ClassificationServiceResult(result.Success, result.Result, result.Error));

        public Task<ClassificationDetailedResult> ClassifyDetailedAsync(CachedMessage message, CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
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
