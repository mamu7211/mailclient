using System.Security.Claims;
using Feirb.Api.Resources;
using Feirb.Api.Services;
using Feirb.Shared.Admin.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class JobSettingsEndpoints
{
    public static RouteGroupBuilder MapJobSettingsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllJobsAsync);
        group.MapGet("/{id:guid}", GetJobByIdAsync);
        group.MapPut("/{id:guid}", UpdateJobAsync);
        group.MapPost("/{id:guid}/run", TriggerJobRunAsync);
        group.MapGet("/{id:guid}/executions", GetJobExecutionsAsync);
        group.MapGet("/by-resource/{resourceType}/{resourceId:guid}", GetJobsByResourceAsync);
        return group;
    }

    private static async Task<IResult> GetAllJobsAsync(
        HttpContext httpContext,
        IJobService jobService)
    {
        if (httpContext.User.IsInRole("Admin"))
            return Results.Ok(await jobService.GetAllAsync());

        var userId = GetCurrentUserId(httpContext);
        return Results.Ok(await jobService.GetForUserAsync(userId));
    }

    private static async Task<IResult> GetJobByIdAsync(
        Guid id,
        HttpContext httpContext,
        IJobService jobService,
        IStringLocalizer<ApiMessages> localizer)
    {
        var (userId, isAdmin) = GetAuthContext(httpContext);
        var result = await jobService.GetByIdAsync(id, userId, isAdmin);
        return result is not null
            ? Results.Ok(result)
            : Results.NotFound(new { message = localizer["JobNotFound"].Value });
    }

    private static async Task<IResult> UpdateJobAsync(
        Guid id,
        UpdateJobSettingsRequest request,
        HttpContext httpContext,
        IJobService jobService,
        IStringLocalizer<ApiMessages> localizer)
    {
        var (userId, isAdmin) = GetAuthContext(httpContext);
        try
        {
            var result = await jobService.UpdateAsync(id, request, userId, isAdmin);
            if (result is null)
                return Results.NotFound(new { message = localizer["JobNotFound"].Value });

            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { message = localizer["JobConcurrencyConflict"].Value });
        }
    }

    private static async Task<IResult> TriggerJobRunAsync(
        Guid id,
        HttpContext httpContext,
        IJobService jobService,
        IStringLocalizer<ApiMessages> localizer)
    {
        var (userId, isAdmin) = GetAuthContext(httpContext);
        return await jobService.TriggerRunAsync(id, userId, isAdmin)
            ? Results.Accepted()
            : Results.NotFound(new { message = localizer["JobNotFound"].Value });
    }

    private static async Task<IResult> GetJobExecutionsAsync(
        Guid id,
        int page,
        int pageSize,
        HttpContext httpContext,
        IJobService jobService,
        IStringLocalizer<ApiMessages> localizer)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (userId, isAdmin) = GetAuthContext(httpContext);
        var result = await jobService.GetExecutionsAsync(id, userId, isAdmin, page, pageSize);
        if (result is null)
            return Results.NotFound(new { message = localizer["JobNotFound"].Value });

        return Results.Ok(result);
    }

    private static async Task<IResult> GetJobsByResourceAsync(
        HttpContext httpContext,
        string resourceType,
        Guid resourceId,
        IJobService jobService)
    {
        var (userId, isAdmin) = GetAuthContext(httpContext);
        return Results.Ok(await jobService.GetByResourceAsync(resourceType, resourceId, userId, isAdmin));
    }

    private static Guid GetCurrentUserId(HttpContext httpContext) =>
        Guid.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private static (Guid userId, bool isAdmin) GetAuthContext(HttpContext httpContext) =>
        (GetCurrentUserId(httpContext), httpContext.User.IsInRole("Admin"));
}
