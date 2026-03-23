using Feirb.Api.Data.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Data;

internal static class DatabaseSeeder
{
    private const string _imapPasswordPurpose = "MailboxImapPassword";
    private const string _smtpPasswordPurpose = "MailboxSmtpPassword";

    public static async Task SeedAsync(FeirbDbContext db, ILogger logger, IDataProtectionProvider dataProtection)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(dataProtection);

        var seeded = false;

        var adminUser = await SeedUserAsync(db, logger, "admin", "admin@feirb.local", "admin@feirb.local", isAdmin: true);
        seeded |= adminUser.created;

        var aliceUser = await SeedUserAsync(db, logger, "alice", "alice@feirb.local", "alice@feirb.local", isAdmin: false);
        seeded |= aliceUser.created;

        seeded |= await SeedMailboxAsync(db, logger, dataProtection, adminUser.user, "#4A90D9");
        seeded |= await SeedMailboxAsync(db, logger, dataProtection, aliceUser.user, "#E67E22");

        if (!await db.SmtpSettings.AnyAsync())
        {
            db.SmtpSettings.Add(new SmtpSettings
            {
                Id = Guid.NewGuid(),
                Host = "localhost",
                Port = 3025,
                UseTls = false,
                RequiresAuth = false,
                FromAddress = "noreply@feirb.local",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            seeded = true;
            logger.LogInformation("Seeded system SMTP settings for GreenMail");
        }

        if (seeded)
            await db.SaveChangesAsync();
    }

    private static async Task<(User user, bool created)> SeedUserAsync(
        FeirbDbContext db, ILogger logger, string username, string email, string password, bool isAdmin)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing is not null)
            return (existing, false);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsAdmin = isAdmin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        logger.LogInformation("Seeded user {Email}", email);
        return (user, true);
    }

    private static async Task<bool> SeedMailboxAsync(
        FeirbDbContext db, ILogger logger, IDataProtectionProvider dataProtection,
        User user, string badgeColor)
    {
        if (await db.Mailboxes.AnyAsync(m => m.UserId == user.Id))
            return false;

        var imapProtector = dataProtection.CreateProtector(_imapPasswordPurpose);
        var smtpProtector = dataProtection.CreateProtector(_smtpPasswordPurpose);

        db.Mailboxes.Add(new Mailbox
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = user.Email,
            EmailAddress = user.Email,
            DisplayName = user.Username[0..1].ToUpperInvariant() + user.Username[1..],
            ImapHost = "localhost",
            ImapPort = 3143,
            ImapUsername = user.Email,
            ImapEncryptedPassword = imapProtector.Protect(user.Email),
            ImapUseTls = false,
            SmtpHost = "localhost",
            SmtpPort = 3025,
            SmtpUsername = user.Email,
            SmtpEncryptedPassword = smtpProtector.Protect(user.Email),
            SmtpUseTls = false,
            SmtpRequiresAuth = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        logger.LogInformation("Seeded mailbox for {Email}", user.Email);
        return true;
    }
}
