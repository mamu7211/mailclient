using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using FluentAssertions;
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

    [Fact]
    public async Task SeedAsync_EmptyDatabase_CreatesAdminUserAndSmtpSettingsAsync()
    {
        using var db = CreateInMemoryContext();

        await DatabaseSeeder.SeedAsync(db, CreateLogger());

        var user = await db.Users.SingleAsync();
        user.Email.Should().Be("admin@feirb.local");
        user.Username.Should().Be("admin");
        user.IsAdmin.Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("admin", user.PasswordHash).Should().BeTrue();

        var smtp = await db.SmtpSettings.SingleAsync();
        smtp.Host.Should().Be("localhost");
        smtp.Port.Should().Be(1025);
        smtp.UseTls.Should().BeFalse();
        smtp.RequiresAuth.Should().BeFalse();
        smtp.FromAddress.Should().Be("noreply@feirb.local");
    }

    [Fact]
    public async Task SeedAsync_AdminAlreadyExists_DoesNotCreateDuplicateAsync()
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
        await db.SaveChangesAsync();

        await DatabaseSeeder.SeedAsync(db, CreateLogger());

        var users = await db.Users.ToListAsync();
        users.Should().HaveCount(1);
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

        await DatabaseSeeder.SeedAsync(db, CreateLogger());

        var settings = await db.SmtpSettings.ToListAsync();
        settings.Should().HaveCount(1);
        settings[0].Host.Should().Be("existing-host");
    }

    [Fact]
    public async Task SeedAsync_CalledTwice_IsIdempotentAsync()
    {
        using var db = CreateInMemoryContext();

        await DatabaseSeeder.SeedAsync(db, CreateLogger());
        await DatabaseSeeder.SeedAsync(db, CreateLogger());

        (await db.Users.CountAsync()).Should().Be(1);
        (await db.SmtpSettings.CountAsync()).Should().Be(1);
    }
}
