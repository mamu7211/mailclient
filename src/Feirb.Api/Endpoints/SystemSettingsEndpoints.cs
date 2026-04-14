using System.Text.Json;
using Feirb.Api.Data;
using Feirb.Api.Resources;
using Feirb.Shared.Admin;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class SystemSettingsEndpoints
{
    private const string _logRetentionCleanupJobType = "log-retention-cleanup";
    private const int _defaultRetentionDays = 30;

    public static RouteGroupBuilder MapSystemSettingsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/smtp", GetSmtpSettingsAsync);
        group.MapPut("/smtp", UpdateSmtpSettingsAsync);
        group.MapGet("/job-retention", GetJobRetentionSettingsAsync);
        group.MapPut("/job-retention", UpdateJobRetentionSettingsAsync);
        return group;
    }

    private static async Task<IResult> GetSmtpSettingsAsync(
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var settings = await db.SmtpSettings.FirstOrDefaultAsync();
        if (settings is null)
            return Results.NotFound(new { message = localizer["SmtpSettingsNotFound"].Value });

        return Results.Ok(new SystemSmtpSettingsResponse(
            settings.Host,
            settings.Port,
            settings.TlsMode,
            settings.RequiresAuth,
            settings.Username,
            settings.FromAddress,
            settings.FromName));
    }

    private static async Task<IResult> UpdateSmtpSettingsAsync(
        UpdateSystemSmtpSettingsRequest request,
        FeirbDbContext db,
        IDataProtectionProvider dataProtection,
        IStringLocalizer<ApiMessages> localizer)
    {
        var settings = await db.SmtpSettings.FirstOrDefaultAsync();
        if (settings is null)
            return Results.NotFound(new { message = localizer["SmtpSettingsNotFound"].Value });

        settings.Host = request.Host;
        settings.Port = request.Port;
        settings.TlsMode = request.TlsMode;
        settings.RequiresAuth = request.RequiresAuth;
        settings.Username = request.Username;
        settings.FromAddress = request.FromAddress;
        settings.FromName = request.FromName;

        if (!string.IsNullOrEmpty(request.Password))
        {
            var protector = dataProtection.CreateProtector(DataProtectionPurposes.SmtpPassword);
            settings.EncryptedPassword = protector.Protect(request.Password);
        }

        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new SystemSmtpSettingsResponse(
            settings.Host,
            settings.Port,
            settings.TlsMode,
            settings.RequiresAuth,
            settings.Username,
            settings.FromAddress,
            settings.FromName));
    }

    private static async Task<IResult> GetJobRetentionSettingsAsync(
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var job = await db.JobSettings
            .FirstOrDefaultAsync(j => j.JobType == _logRetentionCleanupJobType);

        var retentionDays = _defaultRetentionDays;
        if (job?.Configuration is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(job.Configuration);
                if (doc.RootElement.TryGetProperty("retentionDays", out var element)
                    && element.TryGetInt32(out var days)
                    && days > 0)
                {
                    retentionDays = days;
                }
            }
            catch (JsonException) { }
        }

        return Results.Ok(new JobRetentionSettingsResponse(retentionDays));
    }

    private static async Task<IResult> UpdateJobRetentionSettingsAsync(
        UpdateJobRetentionSettingsRequest request,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        if (request.RetentionDays < 1)
            return Results.BadRequest(new { message = localizer["RetentionDaysMustBePositive"].Value });

        var job = await db.JobSettings
            .FirstOrDefaultAsync(j => j.JobType == _logRetentionCleanupJobType);

        if (job is null)
            return Results.NotFound(new { message = localizer["JobSettingsNotFound"].Value });

        job.Configuration = JsonSerializer.Serialize(new { retentionDays = request.RetentionDays });
        job.RowVersion = Guid.NewGuid();
        await db.SaveChangesAsync();

        return Results.Ok(new JobRetentionSettingsResponse(request.RetentionDays));
    }
}
