using Feirb.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Data;

internal static class DatabaseSeeder
{
    public static async Task SeedAsync(FeirbDbContext db, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);

        var seeded = false;

        if (!await db.Users.AnyAsync(u => u.Email == "admin@feirb.local"))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@feirb.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            seeded = true;
            logger.LogInformation("Seeded admin user admin@feirb.local");
        }

        if (!await db.SmtpSettings.AnyAsync())
        {
            db.SmtpSettings.Add(new SmtpSettings
            {
                Id = Guid.NewGuid(),
                Host = "localhost",
                Port = 1025,
                UseTls = false,
                RequiresAuth = false,
                FromAddress = "noreply@feirb.local",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            seeded = true;
            logger.LogInformation("Seeded system SMTP settings for Mailpit");
        }

        if (seeded)
            await db.SaveChangesAsync();
    }
}
