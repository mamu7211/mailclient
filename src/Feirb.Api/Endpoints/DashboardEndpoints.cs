using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Shared.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/layout", GetLayoutAsync);
        group.MapPut("/layout", SaveLayoutAsync);
        return group;
    }

    private static async Task<IResult> GetLayoutAsync(
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);

        var layout = await db.DashboardLayouts
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.UserId == userId);

        return Results.Ok(new DashboardLayoutResponse(layout?.LayoutJson ?? "[]"));
    }

    private static async Task<IResult> SaveLayoutAsync(
        DashboardLayoutRequest request,
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);

        var layout = await db.DashboardLayouts
            .FirstOrDefaultAsync(l => l.UserId == userId);

        if (layout is null)
        {
            layout = new DashboardLayout
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LayoutJson = request.LayoutJson,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            db.DashboardLayouts.Add(layout);
        }
        else
        {
            layout.LayoutJson = request.LayoutJson;
            layout.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new DashboardLayoutResponse(layout.LayoutJson));
    }

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing NameIdentifier claim. Ensure the endpoint requires authorization.");
        return Guid.Parse(claim.Value);
    }
}
