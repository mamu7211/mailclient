using Feirb.Api.Services;

namespace Feirb.Api.Tests;

internal sealed class NoOpJobSettingsScheduler : IJobSettingsScheduler
{
    public Task ScheduleJobAsync(string jobName, string cronExpression) => Task.CompletedTask;
    public Task UnscheduleJobAsync(string jobName) => Task.CompletedTask;
    public Task RescheduleJobAsync(string jobName, string cronExpression) => Task.CompletedTask;
}
