using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Resources;
using Feirb.Api.Services;
using Feirb.Shared.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class MailboxEndpoints
{
    private const string _imapPasswordPurpose = "MailboxImapPassword";
    private const string _smtpPasswordPurpose = "MailboxSmtpPassword";
    private const string _imapSyncJobType = "imap-sync";
    private const string _defaultSyncCron = "0 0 * * * ?"; // hourly

    public static RouteGroupBuilder MapMailboxEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/mailboxes", ListMailboxesAsync);
        group.MapPost("/mailboxes", CreateMailboxAsync);
        group.MapGet("/mailboxes/{id:guid}", GetMailboxAsync);
        group.MapPut("/mailboxes/{id:guid}", UpdateMailboxAsync);
        group.MapDelete("/mailboxes/{id:guid}", DeleteMailboxAsync);
        return group;
    }

    private static async Task<IResult> ListMailboxesAsync(
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);
        var mailboxes = await db.Mailboxes
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Name)
            .Select(m => new MailboxListResponse(m.Id, m.Name, m.EmailAddress, m.BadgeColor))
            .ToListAsync();

        return Results.Ok(mailboxes);
    }

    private static async Task<IResult> CreateMailboxAsync(
        CreateMailboxRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IDataProtectionProvider dataProtection,
        IJobSettingsScheduler jobScheduler,
        IJobService jobService)
    {
        var userId = GetCurrentUserId(httpContext);
        var imapProtector = dataProtection.CreateProtector(_imapPasswordPurpose);
        var smtpProtector = dataProtection.CreateProtector(_smtpPasswordPurpose);

        var mailbox = new Mailbox
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            EmailAddress = request.EmailAddress,
            DisplayName = request.DisplayName,
            BadgeColor = request.BadgeColor,
            ImapHost = request.ImapHost,
            ImapPort = request.ImapPort,
            ImapUsername = request.ImapUsername,
            ImapEncryptedPassword = !string.IsNullOrEmpty(request.ImapPassword)
                ? imapProtector.Protect(request.ImapPassword) : null,
            ImapUseTls = request.ImapUseTls,
            SmtpHost = request.SmtpHost,
            SmtpPort = request.SmtpPort,
            SmtpUsername = request.SmtpUsername,
            SmtpEncryptedPassword = !string.IsNullOrEmpty(request.SmtpPassword)
                ? smtpProtector.Protect(request.SmtpPassword) : null,
            SmtpUseTls = request.SmtpUseTls,
            SmtpRequiresAuth = request.SmtpRequiresAuth,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var jobSettings = new JobSettings
        {
            Id = Guid.NewGuid(),
            JobName = $"imap-sync:{mailbox.Id}",
            JobType = _imapSyncJobType,
            Description = $"IMAP sync for {mailbox.Name}",
            Cron = _defaultSyncCron,
            Enabled = true,
            UserId = userId,
            ResourceId = mailbox.Id,
            ResourceType = "Mailbox",
        };

        db.Mailboxes.Add(mailbox);
        db.JobSettings.Add(jobSettings);
        await db.SaveChangesAsync();

        await jobScheduler.ScheduleJobAsync(jobSettings.JobName, _imapSyncJobType, _defaultSyncCron);

        try
        {
            await jobService.TriggerRunAsync(jobSettings.Id, userId, false);
        }
        catch (Exception)
        {
            // Best-effort immediate sync — cron will catch up
        }

        return Results.Created($"/api/settings/mailboxes/{mailbox.Id}", ToDetailResponse(mailbox));
    }

    private static async Task<IResult> GetMailboxAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var mailbox = await db.Mailboxes.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
        if (mailbox is null)
            return Results.NotFound(new { message = localizer["MailboxNotFound"].Value });

        return Results.Ok(ToDetailResponse(mailbox));
    }

    private static async Task<IResult> UpdateMailboxAsync(
        Guid id,
        UpdateMailboxRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IDataProtectionProvider dataProtection,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var mailbox = await db.Mailboxes.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
        if (mailbox is null)
            return Results.NotFound(new { message = localizer["MailboxNotFound"].Value });

        var imapProtector = dataProtection.CreateProtector(_imapPasswordPurpose);
        var smtpProtector = dataProtection.CreateProtector(_smtpPasswordPurpose);

        mailbox.Name = request.Name;
        mailbox.EmailAddress = request.EmailAddress;
        mailbox.DisplayName = request.DisplayName;
        mailbox.BadgeColor = request.BadgeColor;
        mailbox.ImapHost = request.ImapHost;
        mailbox.ImapPort = request.ImapPort;
        mailbox.ImapUsername = request.ImapUsername;
        if (!string.IsNullOrEmpty(request.ImapPassword))
            mailbox.ImapEncryptedPassword = imapProtector.Protect(request.ImapPassword);
        mailbox.ImapUseTls = request.ImapUseTls;
        mailbox.SmtpHost = request.SmtpHost;
        mailbox.SmtpPort = request.SmtpPort;
        mailbox.SmtpUsername = request.SmtpUsername;
        if (!string.IsNullOrEmpty(request.SmtpPassword))
            mailbox.SmtpEncryptedPassword = smtpProtector.Protect(request.SmtpPassword);
        mailbox.SmtpUseTls = request.SmtpUseTls;
        mailbox.SmtpRequiresAuth = request.SmtpRequiresAuth;
        mailbox.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(ToDetailResponse(mailbox));
    }

    private static async Task<IResult> DeleteMailboxAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer,
        IJobSettingsScheduler jobScheduler)
    {
        var userId = GetCurrentUserId(httpContext);
        var mailbox = await db.Mailboxes.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
        if (mailbox is null)
            return Results.NotFound(new { message = localizer["MailboxNotFound"].Value });

        var jobName = $"imap-sync:{mailbox.Id}";
        await jobScheduler.UnscheduleJobAsync(jobName);

        var job = await db.JobSettings.FirstOrDefaultAsync(
            j => j.JobType == _imapSyncJobType && j.ResourceId == mailbox.Id);
        if (job is not null)
            db.JobSettings.Remove(job);

        db.Mailboxes.Remove(mailbox);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = localizer["MailboxDeleted"].Value });
    }

    private static MailboxDetailResponse ToDetailResponse(Mailbox mailbox) =>
        new(
            mailbox.Id,
            mailbox.Name,
            mailbox.EmailAddress,
            mailbox.DisplayName,
            mailbox.BadgeColor,
            mailbox.ImapHost,
            mailbox.ImapPort,
            mailbox.ImapUsername,
            mailbox.ImapUseTls,
            mailbox.SmtpHost,
            mailbox.SmtpPort,
            mailbox.SmtpUsername,
            mailbox.SmtpUseTls,
            mailbox.SmtpRequiresAuth,
            mailbox.CreatedAt,
            mailbox.UpdatedAt);

    private static Guid GetCurrentUserId(HttpContext httpContext) =>
        Guid.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

}
