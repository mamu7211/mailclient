using Feirb.Api.Data;
using Feirb.Shared.Mail;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Feirb.Api.Services;

public class MailSendingService(
    FeirbDbContext db,
    IDataProtectionProvider dataProtection,
    ILogger<MailSendingService> logger) : IMailSendingService
{
    private const string _smtpPasswordPurpose = "MailboxSmtpPassword";
    private const string _imapPasswordPurpose = "MailboxImapPassword";

    public async Task<string> SendMailAsync(Guid userId, SendMailRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mailbox = await db.Mailboxes
            .FirstOrDefaultAsync(m => m.Id == request.MailboxId && m.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Mailbox not found or not owned by user.");

        var message = BuildMimeMessage(mailbox, request);

        await SendViaSmtpAsync(mailbox, message, cancellationToken);
        await AppendToSentFolderAsync(mailbox, message, cancellationToken);

        logger.LogInformation(
            "Mail sent from {From} to {To} via mailbox {MailboxId}",
            mailbox.EmailAddress, string.Join(", ", request.To), mailbox.Id);

        return message.MessageId ?? string.Empty;
    }

    private static MimeMessage BuildMimeMessage(Data.Entities.Mailbox mailbox, SendMailRequest request)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(mailbox.DisplayName ?? mailbox.Name, mailbox.EmailAddress));

        foreach (var to in request.To)
            message.To.Add(MailboxAddress.Parse(to.Trim()));

        if (request.Cc is { Length: > 0 })
            foreach (var cc in request.Cc)
                message.Cc.Add(MailboxAddress.Parse(cc.Trim()));

        if (request.Bcc is { Length: > 0 })
            foreach (var bcc in request.Bcc)
                message.Bcc.Add(MailboxAddress.Parse(bcc.Trim()));

        message.Subject = request.Subject;

        if (request.ContentType == "html")
        {
            var sanitizedHtml = OutgoingHtmlSanitizer.Sanitize(request.Body);
            message.Body = new TextPart("html") { Text = sanitizedHtml };
        }
        else
        {
            message.Body = new TextPart("plain") { Text = request.Body };
        }

        return message;
    }

    private async Task SendViaSmtpAsync(Data.Entities.Mailbox mailbox, MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(mailbox.SmtpHost, mailbox.SmtpPort, mailbox.SmtpUseTls, cancellationToken);

        if (mailbox.SmtpRequiresAuth && !string.IsNullOrEmpty(mailbox.SmtpEncryptedPassword))
        {
            var protector = dataProtection.CreateProtector(_smtpPasswordPurpose);
            var password = protector.Unprotect(mailbox.SmtpEncryptedPassword);
            await client.AuthenticateAsync(mailbox.SmtpUsername, password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    private async Task AppendToSentFolderAsync(Data.Entities.Mailbox mailbox, MimeMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(mailbox.ImapEncryptedPassword))
        {
            logger.LogWarning("Mailbox {MailboxId} has no IMAP password — skipping Sent folder append", mailbox.Id);
            return;
        }

        try
        {
            using var client = new ImapClient();
            await client.ConnectAsync(mailbox.ImapHost, mailbox.ImapPort, mailbox.ImapUseTls, cancellationToken);

            var protector = dataProtection.CreateProtector(_imapPasswordPurpose);
            var password = protector.Unprotect(mailbox.ImapEncryptedPassword);
            await client.AuthenticateAsync(mailbox.ImapUsername, password, cancellationToken);

            var sent = client.GetFolder(SpecialFolder.Sent)
                ?? (await client.GetFoldersAsync(client.PersonalNamespaces[0], cancellationToken: cancellationToken))
                    .FirstOrDefault(f => f.Name.Equals("Sent", StringComparison.OrdinalIgnoreCase));

            if (sent is not null)
            {
                await sent.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
                await sent.AppendAsync(message, MessageFlags.Seen, cancellationToken);
                logger.LogDebug("Appended sent message to Sent folder for mailbox {MailboxId}", mailbox.Id);
            }
            else
            {
                logger.LogWarning("No Sent folder found for mailbox {MailboxId}", mailbox.Id);
            }

            await client.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to append message to Sent folder for mailbox {MailboxId}", mailbox.Id);
        }
    }
}
