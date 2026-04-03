using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Resources;
using Feirb.Shared.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class ClassificationRuleEndpoints
{
    public static RouteGroupBuilder MapClassificationRuleEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/rules", ListRulesAsync);
        group.MapPost("/rules", CreateRuleAsync);
        group.MapPut("/rules/{id:guid}", UpdateRuleAsync);
        group.MapDelete("/rules/{id:guid}", DeleteRuleAsync);
        return group;
    }

    private static async Task<IResult> ListRulesAsync(
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);
        var rules = await db.ClassificationRules
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .Select(r => new ClassificationRuleResponse(r.Id, r.Instruction, r.CreatedAt, r.UpdatedAt))
            .ToListAsync();

        return Results.Ok(rules);
    }

    private static async Task<IResult> CreateRuleAsync(
        CreateClassificationRuleRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        if (string.IsNullOrWhiteSpace(request.Instruction) || request.Instruction.Length > 500)
            return Results.BadRequest(new { message = localizer["ClassificationRuleInstructionInvalid"].Value });

        var userId = GetCurrentUserId(httpContext);

        var rule = new ClassificationRule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Instruction = request.Instruction,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.ClassificationRules.Add(rule);
        await db.SaveChangesAsync();

        return Results.Created($"/api/settings/rules/{rule.Id}", ToResponse(rule));
    }

    private static async Task<IResult> UpdateRuleAsync(
        Guid id,
        UpdateClassificationRuleRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        if (string.IsNullOrWhiteSpace(request.Instruction) || request.Instruction.Length > 500)
            return Results.BadRequest(new { message = localizer["ClassificationRuleInstructionInvalid"].Value });

        var userId = GetCurrentUserId(httpContext);
        var rule = await db.ClassificationRules.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (rule is null)
            return Results.NotFound(new { message = localizer["ClassificationRuleNotFound"].Value });

        rule.Instruction = request.Instruction;
        rule.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(ToResponse(rule));
    }

    private static async Task<IResult> DeleteRuleAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var rule = await db.ClassificationRules.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (rule is null)
            return Results.NotFound(new { message = localizer["ClassificationRuleNotFound"].Value });

        db.ClassificationRules.Remove(rule);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = localizer["ClassificationRuleDeleted"].Value });
    }

    private static ClassificationRuleResponse ToResponse(ClassificationRule rule) =>
        new(rule.Id, rule.Instruction, rule.CreatedAt, rule.UpdatedAt);

    private static Guid GetCurrentUserId(HttpContext httpContext) =>
        Guid.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
