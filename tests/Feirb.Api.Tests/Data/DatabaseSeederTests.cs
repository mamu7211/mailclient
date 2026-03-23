using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Feirb.Api.Tests.Data;

public class DatabaseSeederTests
{
    private static FeirbDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<FeirbDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new FeirbDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static ILogger CreateLogger() =>
        LoggerFactory.Create(_ => { }).CreateLogger("DatabaseSeeder");

    private static IDataProtectionProvider CreateDataProtection() =>
        DataProtectionProvider.Create("Tests");

    [Fact]
    public async Task SeedAsync_EmptyDatabase_CreatesUsersMailboxesAndSmtpSettingsAsync()
    {
        using var db = CreateInMemoryContext();

        await DatabaseSeeder.SeedAsync(db, CreateLogger(), CreateDataProtection());

        var users = await db.Users.OrderBy(u => u.Username).ToListAsync();
        users.Should().HaveCount(2);

        users[0].Email.Should().Be("admin@feirb.local");
        users[0].Username.Should().Be("admin");
        users[0].IsAdmin.Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("admin@feirb.local", users[0].PasswordHash).Should().BeTrue();

        users[1].Email.Should().Be("alice@feirb.local");
        users[1].Username.Should().Be("alice");
        users[1].IsAdmin.Should().BeFalse();
        BCrypt.Net.BCrypt.Verify("alice@feirb.local", users[1].PasswordHash).Should().BeTrue();

        var mailboxes = await db.Mailboxes.OrderBy(m => m.Name).ToListAsync();
        mailboxes.Should().HaveCount(2);
        mailboxes[0].EmailAddress.Should().Be("admin@feirb.local");
        mailboxes[0].ImapHost.Should().Be("localhost");
        mailboxes[0].ImapPort.Should().Be(3143);
        mailboxes[0].ImapUseTls.Should().BeFalse();
        mailboxes[0].SmtpHost.Should().Be("localhost");
        mailboxes[0].SmtpPort.Should().Be(3025);
        mailboxes[0].ImapEncryptedPassword.Should().NotBeNullOrEmpty();

        mailboxes[1].EmailAddress.Should().Be("alice@feirb.local");

        var smtp = await db.SmtpSettings.SingleAsync();
        smtp.Host.Should().Be("localhost");
        smtp.Port.Should().Be(3025);
        smtp.UseTls.Should().BeFalse();
        smtp.RequiresAuth.Should().BeFalse();
        smtp.FromAddress.Should().Be("noreply@feirb.local");
    }

    [Fact]
    public async Task SeedAsync_UsersAlreadyExist_DoesNotCreateDuplicatesAsync()
    {
        using var db = CreateInMemoryContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@feirb.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("existing"),
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Email = "alice@feirb.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("existing"),
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await DatabaseSeeder.SeedAsync(db, CreateLogger(), CreateDataProtection());

        var users = await db.Users.ToListAsync();
        users.Should().HaveCount(2);
    }

    [Fact]
    public async Task SeedAsync_SmtpSettingsAlreadyExist_DoesNotCreateDuplicateAsync()
    {
        using var db = CreateInMemoryContext();
        db.SmtpSettings.Add(new SmtpSettings
        {
            Id = Guid.NewGuid(),
            Host = "existing-host",
            Port = 587,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await DatabaseSeeder.SeedAsync(db, CreateLogger(), CreateDataProtection());

        var settings = await db.SmtpSettings.ToListAsync();
        settings.Should().HaveCount(1);
        settings[0].Host.Should().Be("existing-host");
    }

    [Fact]
    public async Task SeedAsync_CalledTwice_IsIdempotentAsync()
    {
        using var db = CreateInMemoryContext();
        var dp = CreateDataProtection();

        await DatabaseSeeder.SeedAsync(db, CreateLogger(), dp);
        await DatabaseSeeder.SeedAsync(db, CreateLogger(), dp);

        (await db.Users.CountAsync()).Should().Be(2);
        (await db.Mailboxes.CountAsync()).Should().Be(2);
        (await db.SmtpSettings.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_MailboxAlreadyExists_DoesNotCreateDuplicateAsync()
    {
        using var db = CreateInMemoryContext();
        var dp = CreateDataProtection();

        // First seed creates everything
        await DatabaseSeeder.SeedAsync(db, CreateLogger(), dp);

        // Second seed should not duplicate mailboxes
        await DatabaseSeeder.SeedAsync(db, CreateLogger(), dp);

        var mailboxes = await db.Mailboxes.ToListAsync();
        mailboxes.Should().HaveCount(2);
    }
}
