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
        group.MapGet("/jobs", GetAllJobsAsync);
        group.MapGet("/jobs/{id:guid}", GetJobByIdAsync);
        group.MapPut("/jobs/{id:guid}", UpdateJobAsync);
        group.MapPost("/jobs/{id:guid}/run", TriggerJobRunAsync);
        group.MapGet("/jobs/{id:guid}/executions", GetJobExecutionsAsync);
        return group;
    }

    private static async Task<IResult> GetAllJobsAsync(IJobService jobService) =>
        Results.Ok(await jobService.GetAllAsync());

    private static async Task<IResult> GetJobByIdAsync(
        Guid id,
        IJobService jobService,
        IStringLocalizer<ApiMessages> localizer)
    {
        var result = await jobService.GetByIdAsync(id);
        return result is not null
            ? Results.Ok(result)
            : Results.NotFound(new { message = localizer["JobNotFound"].Value });
    }

    private static async Task<IResult> UpdateJobAsync(
        Guid id,
        UpdateJobSettingsRequest request,
        IJobService jobService,
        IStringLocalizer<ApiMessages> localizer)
    {
        try
        {
            var result = await jobService.UpdateAsync(id, request);
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
        IJobService jobService,
        IStringLocalizer<ApiMessages> localizer)
    {
        var result = await jobService.TriggerRunAsync(id);
        return result
            ? Results.Accepted()
            : Results.NotFound(new { message = localizer["JobNotFound"].Value });
    }

    private static async Task<IResult> GetJobExecutionsAsync(
        Guid id,
        int page,
        int pageSize,
        IJobService jobService,
        IStringLocalizer<ApiMessages> localizer)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var result = await jobService.GetExecutionsAsync(id, page, pageSize);
        if (result is null)
            return Results.NotFound(new { message = localizer["JobNotFound"].Value });

        return Results.Ok(result);
    }
}
