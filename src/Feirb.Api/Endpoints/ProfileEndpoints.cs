using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Api.Resources;
using Feirb.Api.Services;
using Feirb.Shared.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfileEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/profile", GetProfileAsync);
        group.MapPut("/profile", UpdateProfileAsync);
        group.MapPut("/profile/password", ChangePasswordAsync);
        group.MapPost("/profile/logout-all", LogoutAllAsync);
        return group;
    }

    private static async Task<IResult> GetProfileAsync(
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound();

        return Results.Ok(new ProfileResponse(user.Username, user.Email));
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateProfileRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound();

        if (request.Username is null && request.Email is null)
            return Results.Ok(new ProfileResponse(user.Username, user.Email));

        if (request.Username is not null)
        {
            var usernameTaken = await db.Users.AnyAsync(u => u.Username == request.Username && u.Id != userId);
            if (usernameTaken)
                return Results.Conflict(new MessageResponse(localizer["UsernameAlreadyTaken"].Value));

            user.Username = request.Username;
        }

        if (request.Email is not null)
        {
            var emailTaken = await db.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId);
            if (emailTaken)
                return Results.Conflict(new MessageResponse(localizer["EmailAlreadyRegistered"].Value));

            user.Email = request.Email;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new ProfileResponse(user.Username, user.Email));
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IAuthService authService,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound();

        if (!authService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Results.BadRequest(new MessageResponse(localizer["InvalidCurrentPassword"].Value));

        user.PasswordHash = authService.HashPassword(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new MessageResponse(localizer["PasswordChanged"].Value));
    }

    private static async Task<IResult> LogoutAllAsync(
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound();

        user.SecurityStamp = Guid.NewGuid().ToString();
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new MessageResponse(localizer["AllSessionsLoggedOut"].Value));
    }

    private static Guid GetCurrentUserId(HttpContext httpContext) =>
        Guid.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
