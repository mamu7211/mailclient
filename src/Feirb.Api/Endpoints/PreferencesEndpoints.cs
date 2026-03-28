using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Api.Resources;
using Feirb.Shared.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class PreferencesEndpoints
{
    private static readonly HashSet<string> _validThemes =
    [
        "red-light", "red-dark", "orange-light", "orange-dark",
        "green-light", "green-dark", "teal-light", "teal-dark",
        "blue-light", "blue-dark", "purple-light", "purple-dark",
        "pink-light", "pink-dark"
    ];

    public static RouteGroupBuilder MapPreferencesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/preferences", GetPreferencesAsync);
        group.MapPut("/preferences", UpdatePreferencesAsync);
        return group;
    }

    private static async Task<IResult> GetPreferencesAsync(
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound();

        return Results.Ok(new PreferencesResponse(user.Theme ?? "green-light"));
    }

    private static async Task<IResult> UpdatePreferencesAsync(
        UpdatePreferencesRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        if (!_validThemes.Contains(request.Theme))
            return Results.BadRequest(new MessageResponse(localizer["InvalidTheme"].Value));

        var userId = GetCurrentUserId(httpContext);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound();

        user.Theme = request.Theme;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new PreferencesResponse(user.Theme));
    }

    private static Guid GetCurrentUserId(HttpContext httpContext) =>
        Guid.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
