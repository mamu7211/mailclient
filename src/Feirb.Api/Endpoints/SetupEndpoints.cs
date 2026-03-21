using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Resources;
using Feirb.Api.Services;
using Feirb.Shared.Setup;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class SetupEndpoints
{
    private const string _smtpPasswordPurpose = "SmtpPassword";

    public static RouteGroupBuilder MapSetupEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/status", GetStatusAsync);
        group.MapPost("/complete", CompleteSetupAsync);
        group.MapPost("/test-smtp", TestSmtpAsync);
        return group;
    }

    private static async Task<IResult> GetStatusAsync(FeirbDbContext db) =>
        Results.Ok(new SetupStatusResponse(await db.Users.AnyAsync(u => u.IsAdmin)));

    private static async Task<IResult> CompleteSetupAsync(
        CompleteSetupRequest request,
        FeirbDbContext db,
        IAuthService authService,
        IDataProtectionProvider dataProtection,
        IStringLocalizer<ApiMessages> localizer)
    {
        if (await db.Users.AnyAsync(u => u.IsAdmin))
            return Results.Conflict(new { message = localizer["SetupAlreadyComplete"].Value });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = authService.HashPassword(request.Password),
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var protector = dataProtection.CreateProtector(_smtpPasswordPurpose);
        var smtpSettings = new Data.Entities.SmtpSettings
        {
            Id = Guid.NewGuid(),
            Host = request.SmtpHost,
            Port = request.SmtpPort,
            Username = request.SmtpUsername,
            EncryptedPassword = request.SmtpPassword is not null ? protector.Protect(request.SmtpPassword) : null,
            UseTls = request.SmtpUseTls,
            RequiresAuth = request.SmtpRequiresAuth,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        db.SmtpSettings.Add(smtpSettings);
        await db.SaveChangesAsync();

        return Results.Created();
    }

    private static async Task<IResult> TestSmtpAsync(TestSmtpRequest request)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(request.Host, request.Port, request.UseTls);
            if (request.RequiresAuth)
                await client.AuthenticateAsync(request.Username, request.Password);
            await client.DisconnectAsync(quit: true);
            return Results.Ok(new TestSmtpResponse(true, null));
        }
        catch (Exception ex)
        {
            return Results.Ok(new TestSmtpResponse(false, ex.Message));
        }
    }
}
