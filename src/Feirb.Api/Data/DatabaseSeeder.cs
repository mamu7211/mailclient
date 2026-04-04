using Feirb.Api.Data.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Data;

internal static class DatabaseSeeder
{
    private const string _imapPasswordPurpose = "MailboxImapPassword";
    private const string _smtpPasswordPurpose = "MailboxSmtpPassword";
    private const string _imapSyncJobType = "imap-sync";

    public static async Task SeedAsync(FeirbDbContext db, ILogger logger, IDataProtectionProvider dataProtection, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(dataProtection);

        var seeded = false;

        var adminUser = await SeedUserAsync(db, logger, "admin", "admin@feirb.local", "admin@feirb.local", isAdmin: true);
        seeded |= adminUser.created;

        var aliceUser = await SeedUserAsync(db, logger, "alice", "alice@feirb.local", "alice@feirb.local", isAdmin: false);
        seeded |= aliceUser.created;

        var mailHost = configuration["GREENMAIL_HOST"] ?? "localhost";
        var smtpPort = int.TryParse(configuration["GREENMAIL_SMTP_PORT"], out var sp) ? sp : 3025;
        var imapPort = int.TryParse(configuration["GREENMAIL_IMAP_PORT"], out var ip) ? ip : 3143;

        seeded |= await SeedMailboxAsync(db, logger, dataProtection, adminUser.user, "#4A90D9", mailHost, smtpPort, imapPort);
        seeded |= await SeedMailboxAsync(db, logger, dataProtection, aliceUser.user, "#E67E22", mailHost, smtpPort, imapPort);

        if (!await db.SmtpSettings.AnyAsync())
        {
            db.SmtpSettings.Add(new SmtpSettings
            {
                Id = Guid.NewGuid(),
                Host = mailHost,
                Port = smtpPort,
                UseTls = false,
                RequiresAuth = false,
                FromAddress = "noreply@feirb.local",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            seeded = true;
            logger.LogInformation("Seeded system SMTP settings for GreenMail");
        }

        // Save before backfill so mailboxes are visible to the query
        if (seeded)
            await db.SaveChangesAsync();

        if (await BackfillImapSyncJobsAsync(db, logger))
            await db.SaveChangesAsync();

        seeded = await SeedLabelsAsync(db, logger, adminUser.user);
        seeded |= await SeedClassificationRuleAsync(db, logger, adminUser.user);
        seeded |= await EnableAllJobsAsync(db, logger);

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

    private static async Task<bool> BackfillImapSyncJobsAsync(FeirbDbContext db, ILogger logger)
    {
        var mailboxesWithoutJob = await db.Mailboxes
            .Where(m => !db.JobSettings.Any(j => j.JobType == _imapSyncJobType && j.ResourceId == m.Id))
            .Select(m => new { m.Id, m.Name, m.UserId })
            .ToListAsync();

        foreach (var mailbox in mailboxesWithoutJob)
        {
            db.JobSettings.Add(new JobSettings
            {
                Id = Guid.NewGuid(),
                JobName = $"imap-sync:{mailbox.Id}",
                JobType = _imapSyncJobType,
                Description = $"IMAP sync for {mailbox.Name}",
                Cron = "0 0 * * * ?",
                Enabled = true,
                UserId = mailbox.UserId,
                ResourceId = mailbox.Id,
                ResourceType = "Mailbox",
            });
            logger.LogInformation("Backfilled IMAP sync job for mailbox {MailboxId}", mailbox.Id);
        }

        return mailboxesWithoutJob.Count > 0;
    }

    private static async Task<bool> SeedLabelsAsync(FeirbDbContext db, ILogger logger, User user)
    {
        if (await db.Labels.AnyAsync(l => l.UserId == user.Id))
            return false;

        var labels = new[]
        {
            new Label { Id = Guid.NewGuid(), UserId = user.Id, Name = "Newsletter", Color = "#4CAF50", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Label { Id = Guid.NewGuid(), UserId = user.Id, Name = "Work", Color = "#2196F3", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Label { Id = Guid.NewGuid(), UserId = user.Id, Name = "Personal", Color = "#FF9800", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        };

        db.Labels.AddRange(labels);
        logger.LogInformation("Seeded {Count} labels for {Email}", labels.Length, user.Email);
        return true;
    }

    private static async Task<bool> SeedClassificationRuleAsync(FeirbDbContext db, ILogger logger, User user)
    {
        if (await db.ClassificationRules.AnyAsync(r => r.UserId == user.Id))
            return false;

        db.ClassificationRules.Add(new ClassificationRule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Instruction = "Classify newsletter or mailing list emails as Newsletter. Classify work-related emails as Work. Classify personal emails as Personal.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        logger.LogInformation("Seeded classification rule for {Email}", user.Email);
        return true;
    }

    private static async Task<bool> EnableAllJobsAsync(FeirbDbContext db, ILogger logger)
    {
        var jobs = await db.JobSettings.Where(j => !j.Enabled || j.Cron != "0 * * * * ?").ToListAsync();
        if (jobs.Count == 0)
            return false;

        foreach (var job in jobs)
        {
            job.Enabled = true;
            job.Cron = "0 * * * * ?"; // Every minute for dev
            logger.LogInformation("Enabled job {JobName} with 1-minute interval", job.JobName);
        }

        return true;
    }

    private static async Task<bool> SeedMailboxAsync(
        FeirbDbContext db, ILogger logger, IDataProtectionProvider dataProtection,
        User user, string badgeColor, string mailHost, int smtpPort, int imapPort)
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
            ImapHost = mailHost,
            ImapPort = imapPort,
            ImapUsername = user.Email,
            ImapEncryptedPassword = imapProtector.Protect(user.Email),
            ImapUseTls = false,
            SmtpHost = mailHost,
            SmtpPort = smtpPort,
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
