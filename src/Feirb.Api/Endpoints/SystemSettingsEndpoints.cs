using Feirb.Api.Data;
using Feirb.Api.Resources;
using Feirb.Shared.Admin;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class SystemSettingsEndpoints
{

    public static RouteGroupBuilder MapSystemSettingsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/smtp", GetSmtpSettingsAsync);
        group.MapPut("/smtp", UpdateSmtpSettingsAsync);
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
}
