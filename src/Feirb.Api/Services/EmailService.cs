using Feirb.Api.Data;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Feirb.Api.Services;

public class EmailService(
    FeirbDbContext db,
    IDataProtectionProvider dataProtection,
    ILogger<EmailService> logger) : IEmailService
{
    public async Task<bool> SendAsync(string to, string subject, string htmlBody)
    {
        var settings = await db.SmtpSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            logger.LogWarning("No SMTP settings configured — cannot send email");
            return false;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName ?? "Feirb", settings.FromAddress ?? "noreply@feirb.local"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(settings.Host, settings.Port, settings.UseTls);

            if (settings.RequiresAuth && settings.Username is not null && settings.EncryptedPassword is not null)
            {
                var protector = dataProtection.CreateProtector(DataProtectionPurposes.SmtpPassword);
                var password = protector.Unprotect(settings.EncryptedPassword);
                await client.AuthenticateAsync(settings.Username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);

            logger.LogInformation("Email sent to {To} with subject '{Subject}'", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }
}
