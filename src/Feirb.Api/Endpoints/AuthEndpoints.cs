using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Resources;
using Feirb.Api.Services;
using Feirb.Shared.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Feirb.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/request-reset", RequestResetAsync);
        group.MapGet("/validate-reset-token/{token}", ValidateResetTokenAsync);
        group.MapPost("/reset-password", ResetPasswordAsync);
        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        FeirbDbContext db,
        IAuthService authService,
        IStringLocalizer<ApiMessages> localizer)
    {
        var usernameExists = await db.Users.AnyAsync(u => u.Username == request.Username);
        if (usernameExists)
            return Results.Conflict(new { message = localizer["UsernameAlreadyTaken"].Value });

        var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return Results.Conflict(new { message = localizer["EmailAlreadyRegistered"].Value });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = authService.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = new RegisterResponse(user.Id, user.Username, user.Email);
        return Results.Created($"/api/auth/users/{user.Id}", response);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        FeirbDbContext db,
        IAuthService authService,
        IOptions<JwtSettings> jwtSettings,
        IStringLocalizer<ApiMessages> localizer)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null || !authService.VerifyPassword(request.Password, user.PasswordHash))
            return Results.Unauthorized();

        var tokens = authService.GenerateTokens(user);

        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryDays);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(tokens);
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        FeirbDbContext db,
        IAuthService authService,
        IOptions<JwtSettings> jwtSettings,
        IStringLocalizer<ApiMessages> localizer)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);
        if (user is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            return Results.Unauthorized();

        var tokens = authService.GenerateTokens(user);

        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryDays);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(tokens);
    }

    private static async Task<IResult> RequestResetAsync(
        RequestResetRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IAuthService authService,
        IEmailService emailService,
        ILogger<Program> logger,
        IStringLocalizer<ApiMessages> localizer)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is not null)
        {
            var token = authService.GenerateResetToken();

            db.PasswordResetTokens.Add(new Data.Entities.PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var resetLink = $"{baseUrl}/reset-password/{token}";
            var subject = localizer["ResetEmailSubject"].Value;
            var htmlBody = EmailTemplates.BuildPasswordResetEmail(user.Username, resetLink, localizer);

            var sent = await emailService.SendAsync(user.Email, subject, htmlBody);
            if (!sent)
            {
                logger.LogInformation("Password reset token for {Email}: {Token}", user.Email, token);
            }
        }

        // Always return OK to not reveal whether the email exists
        return Results.Ok(new { message = localizer["ResetRequestAccepted"].Value });
    }

    private static async Task<IResult> ValidateResetTokenAsync(
        string token,
        FeirbDbContext db)
    {
        var resetToken = await db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (resetToken is null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
            return Results.NotFound();

        return Results.Ok();
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        FeirbDbContext db,
        IAuthService authService,
        IStringLocalizer<ApiMessages> localizer)
    {
        var resetToken = await db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token);

        if (resetToken is null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
            return Results.BadRequest(new { message = localizer["InvalidOrExpiredResetToken"].Value });

        resetToken.User.PasswordHash = authService.HashPassword(request.NewPassword);
        resetToken.User.SecurityStamp = Guid.NewGuid().ToString();
        resetToken.User.UpdatedAt = DateTime.UtcNow;
        // Invalidate any existing refresh tokens and sessions
        resetToken.User.RefreshToken = null;
        resetToken.User.RefreshTokenExpiresAt = null;
        resetToken.IsUsed = true;

        await db.SaveChangesAsync();

        return Results.Ok(new { message = localizer["PasswordResetSuccess"].Value });
    }
}
