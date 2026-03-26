using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Shared.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Endpoints;

public static class WidgetConfigEndpoints
{
    public static RouteGroupBuilder MapWidgetConfigEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/widgets/{instanceId}/config", GetConfigAsync);
        group.MapPost("/widgets/{instanceId}/config", CreateConfigAsync);
        group.MapPut("/widgets/{instanceId}/config", UpdateConfigAsync);
        group.MapDelete("/widgets/{instanceId}/config", DeleteConfigAsync);
        return group;
    }

    private static async Task<IResult> GetConfigAsync(
        string instanceId,
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);

        var config = await db.WidgetConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.WidgetInstanceId == instanceId);

        return config is null
            ? Results.NotFound()
            : Results.Ok(new WidgetConfigResponse(config.ConfigValue));
    }

    private static async Task<IResult> CreateConfigAsync(
        string instanceId,
        WidgetConfigRequest request,
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);

        var existing = await db.WidgetConfigs
            .FirstOrDefaultAsync(c => c.UserId == userId && c.WidgetInstanceId == instanceId);

        if (existing is not null)
            return Results.Conflict();

        var config = new WidgetConfig
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WidgetInstanceId = instanceId,
            ConfigValue = request.ConfigValue,
        };
        db.WidgetConfigs.Add(config);
        await db.SaveChangesAsync();

        return Results.Created($"/api/dashboard/widgets/{instanceId}/config",
            new WidgetConfigResponse(config.ConfigValue));
    }

    private static async Task<IResult> UpdateConfigAsync(
        string instanceId,
        WidgetConfigRequest request,
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);

        var config = await db.WidgetConfigs
            .FirstOrDefaultAsync(c => c.UserId == userId && c.WidgetInstanceId == instanceId);

        if (config is null)
            return Results.NotFound();

        config.ConfigValue = request.ConfigValue;
        await db.SaveChangesAsync();

        return Results.Ok(new WidgetConfigResponse(config.ConfigValue));
    }

    private static async Task<IResult> DeleteConfigAsync(
        string instanceId,
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);

        var config = await db.WidgetConfigs
            .FirstOrDefaultAsync(c => c.UserId == userId && c.WidgetInstanceId == instanceId);

        if (config is null)
            return Results.NotFound();

        db.WidgetConfigs.Remove(config);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing NameIdentifier claim. Ensure the endpoint requires authorization.");
        return Guid.Parse(claim.Value);
    }
}
