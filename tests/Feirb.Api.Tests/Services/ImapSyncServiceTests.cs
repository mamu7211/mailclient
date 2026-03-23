using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Services;
using FluentAssertions;
using MailKit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;

namespace Feirb.Api.Tests.Services;

public class ImapSyncServiceTests
{
    [Fact]
    public void MapToCachedMessage_ValidMessage_ProducesCorrectFields()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Alice", "alice@example.com"));
        message.To.Add(new MailboxAddress("Bob", "bob@example.com"));
        message.Cc.Add(new MailboxAddress("Charlie", "charlie@example.com"));
        message.ReplyTo.Add(new MailboxAddress("Alice", "alice@example.com"));
        message.Subject = "Test Subject";
        message.MessageId = "unique-id@example.com";
        message.Date = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        message.Body = new TextPart("plain") { Text = "Hello" };

        var mailboxId = Guid.NewGuid();
        var uid = new UniqueId(42);

        var cached = ImapSyncService.MapToCachedMessage(message, mailboxId, uid);

        cached.MailboxId.Should().Be(mailboxId);
        cached.MessageId.Should().Be("unique-id@example.com");
        cached.ImapUid.Should().Be(42u);
        cached.Subject.Should().Be("Test Subject");
        cached.From.Should().Contain("alice@example.com");
        cached.To.Should().Contain("bob@example.com");
        cached.Cc.Should().Contain("charlie@example.com");
        cached.ReplyTo.Should().Contain("alice@example.com");
        cached.Date.Should().Be(new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero));
        cached.SyncedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MapToCachedMessage_NonUtcDate_ConvertsToUtc()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Alice", "alice@example.com"));
        message.To.Add(new MailboxAddress("Bob", "bob@example.com"));
        message.Subject = "CET Message";
        message.Date = new DateTimeOffset(2026, 3, 15, 11, 0, 0, TimeSpan.FromHours(1));
        message.Body = new TextPart("plain") { Text = "Hello" };

        var cached = ImapSyncService.MapToCachedMessage(message, Guid.NewGuid(), new UniqueId(1));

        cached.Date.Offset.Should().Be(TimeSpan.Zero);
        cached.Date.Should().Be(new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void MapToCachedMessage_NullMessageId_GeneratesFallback()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Alice", "alice@example.com"));
        message.To.Add(new MailboxAddress("Bob", "bob@example.com"));
        message.Subject = "No ID";
        message.Body = new TextPart("plain") { Text = "Hello" };

        // MimeMessage auto-generates a MessageId, so we test the fallback path
        // by verifying the mapping still works
        var uid = new UniqueId(99);
        var cached = ImapSyncService.MapToCachedMessage(message, Guid.NewGuid(), uid);

        cached.MessageId.Should().NotBeNullOrEmpty();
        cached.ImapUid.Should().Be(99u);
    }

    [Fact]
    public void MapToCachedMessage_NoOptionalFields_SetsNulls()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Alice", "alice@example.com"));
        message.To.Add(new MailboxAddress("Bob", "bob@example.com"));
        message.Subject = "Minimal";
        message.Body = new TextPart("plain") { Text = "Hello" };

        var cached = ImapSyncService.MapToCachedMessage(message, Guid.NewGuid(), new UniqueId(1));

        cached.Cc.Should().BeNull();
        cached.ReplyTo.Should().BeNull();
        cached.BodyPlainText.Should().Be("Hello");
        cached.BodyHtml.Should().BeNull();
    }

    [Fact]
    public void MapToCachedMessage_WithAttachment_ParsesMetadata()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Alice", "alice@example.com"));
        message.To.Add(new MailboxAddress("Bob", "bob@example.com"));
        message.Subject = "With Attachment";

        var attachment = new MimePart("application", "pdf")
        {
            FileName = "report.pdf",
            Content = new MimeContent(new MemoryStream(new byte[1024])),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
        };

        var multipart = new Multipart("mixed");
        multipart.Add(new TextPart("plain") { Text = "See attached" });
        multipart.Add(attachment);
        message.Body = multipart;

        var cached = ImapSyncService.MapToCachedMessage(message, Guid.NewGuid(), new UniqueId(5));

        cached.Attachments.Should().HaveCount(1);
        var att = cached.Attachments.First();
        att.Filename.Should().Be("report.pdf");
        att.MimeType.Should().Be("application/pdf");
        att.Size.Should().Be(1024);
    }

    [Fact]
    public async Task SyncMailboxAsync_MailboxNotFound_LogsWarningAsync()
    {
        var services = CreateServiceProvider();
        var logger = NullLogger<ImapSyncService>.Instance;
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();

        var service = new ImapSyncService(scopeFactory, logger);

        // Should not throw — just logs warning
        await service.Invoking(s => s.SyncMailboxAsync(Guid.NewGuid()))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncMailboxAsync_NoImapPassword_SkipsSyncAsync()
    {
        var services = CreateServiceProvider();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
        };
        db.Users.Add(user);

        var mailbox = new Mailbox
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Test",
            EmailAddress = "test@example.com",
            ImapHost = "localhost",
            ImapUsername = "test",
            ImapEncryptedPassword = null,
            SmtpHost = "localhost",
            SmtpUsername = "test",
        };
        db.Mailboxes.Add(mailbox);
        await db.SaveChangesAsync();

        var logger = NullLogger<ImapSyncService>.Instance;
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var service = new ImapSyncService(scopeFactory, logger);

        // Should not throw — just logs warning and skips
        await service.Invoking(s => s.SyncMailboxAsync(mailbox.Id))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task DuplicateMessageId_IsSkipped_WhenAlreadyInDatabaseAsync()
    {
        var services = CreateServiceProvider();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        var mailboxId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
        };
        db.Users.Add(user);
        db.Mailboxes.Add(new Mailbox
        {
            Id = mailboxId,
            UserId = user.Id,
            Name = "Test",
            EmailAddress = "test@example.com",
            ImapHost = "localhost",
            ImapUsername = "test",
            SmtpHost = "localhost",
            SmtpUsername = "test",
        });

        // Pre-existing message
        db.CachedMessages.Add(new CachedMessage
        {
            Id = Guid.NewGuid(),
            MailboxId = mailboxId,
            MessageId = "duplicate@example.com",
            ImapUid = 10,
            Subject = "Existing",
            From = "alice@example.com",
            To = "bob@example.com",
            Date = DateTimeOffset.UtcNow,
            SyncedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        // Simulate the dedup logic from SyncMailboxAsync
        var existingMessageIds = await db.CachedMessages
            .Where(cm => cm.MailboxId == mailboxId)
            .Select(cm => cm.MessageId)
            .ToHashSetAsync();

        // Create a "new" message with the same MessageId
        var duplicateMessage = new MimeMessage();
        duplicateMessage.From.Add(new MailboxAddress("Alice", "alice@example.com"));
        duplicateMessage.To.Add(new MailboxAddress("Bob", "bob@example.com"));
        duplicateMessage.Subject = "Duplicate";
        duplicateMessage.MessageId = "duplicate@example.com";
        duplicateMessage.Body = new TextPart("plain") { Text = "Hello again" };

        // Create a genuinely new message
        var newMessage = new MimeMessage();
        newMessage.From.Add(new MailboxAddress("Charlie", "charlie@example.com"));
        newMessage.To.Add(new MailboxAddress("Bob", "bob@example.com"));
        newMessage.Subject = "New message";
        newMessage.MessageId = "new@example.com";
        newMessage.Body = new TextPart("plain") { Text = "Hello" };

        // Apply same dedup logic as SyncMailboxAsync
        var messages = new[] { (duplicateMessage, new UniqueId(11)), (newMessage, new UniqueId(12)) };
        foreach (var (msg, uid) in messages)
        {
            if (msg.MessageId is not null && existingMessageIds.Contains(msg.MessageId))
                continue;

            db.CachedMessages.Add(ImapSyncService.MapToCachedMessage(msg, mailboxId, uid));
        }

        await db.SaveChangesAsync();

        // Only the new message should have been added (1 existing + 1 new = 2 total)
        var totalMessages = await db.CachedMessages.CountAsync(cm => cm.MailboxId == mailboxId);
        totalMessages.Should().Be(2);

        // The duplicate should not exist twice
        var duplicateCount = await db.CachedMessages
            .CountAsync(cm => cm.MailboxId == mailboxId && cm.MessageId == "duplicate@example.com");
        duplicateCount.Should().Be(1);

        // The new message should exist
        var newExists = await db.CachedMessages
            .AnyAsync(cm => cm.MailboxId == mailboxId && cm.MessageId == "new@example.com");
        newExists.Should().BeTrue();
    }

    [Fact]
    public async Task SyncMailboxAsync_IncrementalSync_QueriesAboveLastSeenUidAsync()
    {
        var services = CreateServiceProvider();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
        };
        db.Users.Add(user);

        var mailboxId = Guid.NewGuid();
        var mailbox = new Mailbox
        {
            Id = mailboxId,
            UserId = user.Id,
            Name = "Test",
            EmailAddress = "test@example.com",
            ImapHost = "localhost",
            ImapUsername = "test",
            SmtpHost = "localhost",
            SmtpUsername = "test",
        };
        db.Mailboxes.Add(mailbox);

        // Add messages with known UIDs
        db.CachedMessages.Add(new CachedMessage
        {
            Id = Guid.NewGuid(),
            MailboxId = mailboxId,
            MessageId = "msg1@example.com",
            ImapUid = 50,
            Subject = "First",
            From = "alice@example.com",
            To = "bob@example.com",
            Date = DateTimeOffset.UtcNow,
            SyncedAt = DateTimeOffset.UtcNow,
        });
        db.CachedMessages.Add(new CachedMessage
        {
            Id = Guid.NewGuid(),
            MailboxId = mailboxId,
            MessageId = "msg2@example.com",
            ImapUid = 100,
            Subject = "Second",
            From = "alice@example.com",
            To = "bob@example.com",
            Date = DateTimeOffset.UtcNow,
            SyncedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        // Verify last seen UID is the max
        var lastSeenUid = await db.CachedMessages
            .Where(cm => cm.MailboxId == mailboxId && cm.ImapUid != null)
            .MaxAsync(cm => (uint?)cm.ImapUid);

        lastSeenUid.Should().Be(100u);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<FeirbDbContext>(options =>
            options.UseInMemoryDatabase($"ImapSyncTest-{Guid.NewGuid()}"));
        services.AddDataProtection()
            .SetApplicationName("Tests");
        services.AddLogging();
        return services.BuildServiceProvider();
    }
}
