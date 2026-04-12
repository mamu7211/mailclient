using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Resources;
using Feirb.Shared.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class LabelEndpoints
{
    public static RouteGroupBuilder MapLabelEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/labels", ListLabelsAsync);
        group.MapPost("/labels", CreateLabelAsync);
        group.MapPut("/labels/{id:guid}", UpdateLabelAsync);
        group.MapDelete("/labels/{id:guid}", DeleteLabelAsync);
        return group;
    }

    private static async Task<IResult> ListLabelsAsync(
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);
        var labels = await db.Labels
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Name)
            .Select(l => new LabelResponse(l.Id, l.Name, l.Color, l.Description, l.CreatedAt, l.UpdatedAt))
            .ToListAsync();

        return Results.Ok(labels);
    }

    private static async Task<IResult> CreateLabelAsync(
        CreateLabelRequest request,
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);

        var label = new Label
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.ToLowerInvariant(),
            Color = request.Color ?? GenerateRandomColor(),
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Labels.Add(label);
        await db.SaveChangesAsync();

        return Results.Created($"/api/settings/labels/{label.Id}", ToResponse(label));
    }

    private static async Task<IResult> UpdateLabelAsync(
        Guid id,
        UpdateLabelRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var label = await db.Labels.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        if (label is null)
            return Results.NotFound(new { message = localizer["LabelNotFound"].Value });

        label.Name = request.Name.ToLowerInvariant();
        label.Color = request.Color;
        label.Description = request.Description;
        label.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(ToResponse(label));
    }

    private static async Task<IResult> DeleteLabelAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var label = await db.Labels.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        if (label is null)
            return Results.NotFound(new { message = localizer["LabelNotFound"].Value });

        db.Labels.Remove(label);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = localizer["LabelDeleted"].Value });
    }

    private static LabelResponse ToResponse(Label label) =>
        new(label.Id, label.Name, label.Color, label.Description, label.CreatedAt, label.UpdatedAt);

    private static string GenerateRandomColor()
    {
        var bytes = new byte[3];
        Random.Shared.NextBytes(bytes);
        return $"#{bytes[0]:X2}{bytes[1]:X2}{bytes[2]:X2}";
    }

    private static Guid GetCurrentUserId(HttpContext httpContext) =>
        Guid.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
