using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Feirb.Api.Tests.Services;

public class ClassificationServiceTests : IDisposable
{
    private readonly DbContextOptions<FeirbDbContext> _dbOptions;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _mailboxId = Guid.NewGuid();

    public ClassificationServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<FeirbDbContext>()
            .UseInMemoryDatabase($"ClassificationServiceTests-{Guid.NewGuid()}")
            .Options;

        using var db = new FeirbDbContext(_dbOptions);
        db.Database.EnsureCreated();

        db.Users.Add(new User
        {
            Id = _userId,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsAdmin = false,
        });

        db.Mailboxes.Add(new Mailbox
        {
            Id = _mailboxId,
            UserId = _userId,
            Name = "Test Mailbox",
            EmailAddress = "test@example.com",
            ImapHost = "localhost",
            ImapPort = 993,
            ImapUsername = "test",
            SmtpHost = "localhost",
            SmtpPort = 587,
            SmtpUsername = "test",
        });

        db.SaveChanges();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ClassifyAsync_NoRules_ReturnsSkippedAsync()
    {
        SeedLabels("Newsletter", "Important");
        var service = CreateService(MockChatClient("[]"));
        var message = CreateMessage();

        var result = await service.ClassifyAsync(message);

        result.IsSkipped.Should().BeTrue();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ClassifyAsync_NoLabels_ReturnsSkippedAsync()
    {
        SeedRules("Mark newsletters as Newsletter");
        var service = CreateService(MockChatClient("[]"));
        var message = CreateMessage();

        var result = await service.ClassifyAsync(message);

        result.IsSkipped.Should().BeTrue();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ClassifyAsync_ValidLabels_ReturnsSuccessWithLabelsAsync()
    {
        SeedLabels("Newsletter", "Important");
        SeedRules("Mark newsletters as Newsletter");
        var service = CreateService(MockChatClient("""["Newsletter"]"""));
        var message = CreateMessage();

        var result = await service.ClassifyAsync(message);

        result.Success.Should().BeTrue();
        result.IsSkipped.Should().BeFalse();
        result.Result.Should().Be("""["Newsletter"]""");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyAsync_EmptyArrayResponse_ReturnsSuccessAsync()
    {
        SeedLabels("Newsletter");
        SeedRules("Mark newsletters as Newsletter");
        var service = CreateService(MockChatClient("[]"));
        var message = CreateMessage();

        var result = await service.ClassifyAsync(message);

        result.Success.Should().BeTrue();
        result.Result.Should().Be("[]");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyAsync_UnknownLabels_ReturnsFailedAsync()
    {
        SeedLabels("Newsletter");
        SeedRules("Classify emails");
        var service = CreateService(MockChatClient("""["NonExistent"]"""));
        var message = CreateMessage();

        var result = await service.ClassifyAsync(message);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Unknown labels").And.Contain("NonExistent");
    }

    [Fact]
    public async Task ClassifyAsync_MalformedJson_ReturnsFailedAsync()
    {
        SeedLabels("Newsletter");
        SeedRules("Classify emails");
        var service = CreateService(MockChatClient("not json at all"));
        var message = CreateMessage();

        var result = await service.ClassifyAsync(message);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Failed to parse LLM response");
    }

    [Fact]
    public async Task ClassifyAsync_OllamaUnavailable_ReturnsSkippedAsync()
    {
        SeedLabels("Newsletter");
        SeedRules("Classify emails");

        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(_ => throw new HttpRequestException("Connection refused"));

        var service = CreateService(chatClient);
        var message = CreateMessage();

        var result = await service.ClassifyAsync(message);

        result.IsSkipped.Should().BeTrue();
    }

    [Fact]
    public async Task ClassifyAsync_MailboxNotFound_ReturnsFailedAsync()
    {
        var service = CreateService(MockChatClient("[]"));
        var message = new CachedMessage
        {
            Id = Guid.NewGuid(),
            MailboxId = Guid.NewGuid(), // Non-existent mailbox
            MessageId = "<test@example.com>",
            Subject = "Test",
            From = "sender@example.com",
            To = "recipient@example.com",
            Date = DateTimeOffset.UtcNow,
            SyncedAt = DateTimeOffset.UtcNow,
        };

        var result = await service.ClassifyAsync(message);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Mailbox not found");
    }

    [Fact]
    public void BuildPrompt_IncludesEmailDelimitersAndStructuredRoles()
    {
        var message = CreateMessage(subject: "Newsletter issue #42", body: "Hello, this is a newsletter.");
        var rules = new List<string> { "Mark newsletters as Newsletter" };
        var labels = new List<string> { "Newsletter", "Important" };

        var chatMessages = ClassificationService.BuildPrompt(message, rules, labels);

        chatMessages.Should().HaveCount(2);
        chatMessages[0].Role.Should().Be(ChatRole.System);
        chatMessages[1].Role.Should().Be(ChatRole.User);

        var systemText = chatMessages[0].Text!;
        systemText.Should().Contain("Newsletter");
        systemText.Should().Contain("Important");
        systemText.Should().Contain("Mark newsletters as Newsletter");
        systemText.Should().Contain("<email>");

        var userText = chatMessages[1].Text!;
        userText.Should().Contain("<email>");
        userText.Should().Contain("</email>");
        userText.Should().Contain("Newsletter issue #42");
        userText.Should().Contain("Hello, this is a newsletter.");
    }

    [Fact]
    public void BuildUserPrompt_TruncatesBodyAt500Chars()
    {
        var longBody = new string('A', 1000);
        var message = CreateMessage(body: longBody);

        var prompt = ClassificationService.BuildUserPrompt(message);

        prompt.Should().Contain(new string('A', 500));
        prompt.Should().NotContain(new string('A', 501));
    }

    [Fact]
    public void BuildUserPrompt_IncludesCcWhenPresent()
    {
        var message = CreateMessage();
        message.Cc = "cc@example.com";

        var prompt = ClassificationService.BuildUserPrompt(message);

        prompt.Should().Contain("CC: cc@example.com");
    }

    [Fact]
    public void BuildUserPrompt_OmitsCcWhenEmpty()
    {
        var message = CreateMessage();
        message.Cc = null;

        var prompt = ClassificationService.BuildUserPrompt(message);

        prompt.Should().NotContain("CC:");
    }

    [Fact]
    public void ParseAndValidateResponse_ValidLabels_ReturnsSuccess()
    {
        var validLabels = new List<string> { "Newsletter", "Important" };

        var result = ClassificationService.ParseAndValidateResponse("""["Newsletter"]""", validLabels);

        result.Success.Should().BeTrue();
        result.Result.Should().Be("""["Newsletter"]""");
    }

    [Fact]
    public void ParseAndValidateResponse_EmptyArray_ReturnsSuccess()
    {
        var validLabels = new List<string> { "Newsletter" };

        var result = ClassificationService.ParseAndValidateResponse("[]", validLabels);

        result.Success.Should().BeTrue();
        result.Result.Should().Be("[]");
    }

    [Fact]
    public void ParseAndValidateResponse_UnknownLabels_ReturnsFailed()
    {
        var validLabels = new List<string> { "Newsletter" };

        var result = ClassificationService.ParseAndValidateResponse("""["Spam"]""", validLabels);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Unknown labels").And.Contain("Spam");
    }

    [Fact]
    public void ParseAndValidateResponse_MalformedJson_ReturnsFailed()
    {
        var validLabels = new List<string> { "Newsletter" };

        var result = ClassificationService.ParseAndValidateResponse("invalid json", validLabels);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Failed to parse");
    }

    [Fact]
    public void ParseAndValidateResponse_MarkdownCodeFence_StripsAndParses()
    {
        var validLabels = new List<string> { "Newsletter" };
        var response = """
            ```json
            ["Newsletter"]
            ```
            """;

        var result = ClassificationService.ParseAndValidateResponse(response, validLabels);

        result.Success.Should().BeTrue();
        result.Result.Should().Be("""["Newsletter"]""");
    }

    [Fact]
    public void ParseAndValidateResponse_CaseInsensitiveLabelMatching_ReturnsSuccess()
    {
        var validLabels = new List<string> { "Newsletter" };

        var result = ClassificationService.ParseAndValidateResponse("""["newsletter"]""", validLabels);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void ParseAndValidateResponse_MultipleValidLabels_ReturnsAll()
    {
        var validLabels = new List<string> { "Newsletter", "Important", "Work" };

        var result = ClassificationService.ParseAndValidateResponse(
            """["Newsletter", "Important"]""", validLabels);

        result.Success.Should().BeTrue();
        result.Result.Should().Contain("Newsletter");
        result.Result.Should().Contain("Important");
    }

    private ClassificationService CreateService(IChatClient chatClient)
    {
        var db = new FeirbDbContext(_dbOptions);
        return new ClassificationService(db, chatClient, NullLogger<ClassificationService>.Instance);
    }

    private static IChatClient MockChatClient(string responseText)
    {
        var chatClient = Substitute.For<IChatClient>();
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText));
        chatClient.GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));
        return chatClient;
    }

    private CachedMessage CreateMessage(string? subject = null, string? body = null)
    {
        return new CachedMessage
        {
            Id = Guid.NewGuid(),
            MailboxId = _mailboxId,
            MessageId = $"<{Guid.NewGuid()}@example.com>",
            Subject = subject ?? "Test Subject",
            From = "sender@example.com",
            To = "recipient@example.com",
            Date = DateTimeOffset.UtcNow,
            SyncedAt = DateTimeOffset.UtcNow,
            BodyPlainText = body ?? "Test body content",
        };
    }

    private void SeedLabels(params string[] labelNames)
    {
        using var db = new FeirbDbContext(_dbOptions);
        foreach (var name in labelNames)
        {
            db.Labels.Add(new Label
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                Name = name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        db.SaveChanges();
    }

    private void SeedRules(params string[] instructions)
    {
        using var db = new FeirbDbContext(_dbOptions);
        foreach (var instruction in instructions)
        {
            db.ClassificationRules.Add(new ClassificationRule
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                Instruction = instruction,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        db.SaveChanges();
    }
}
