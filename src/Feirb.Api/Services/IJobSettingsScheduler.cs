namespace Feirb.Api.Services;

public interface IJobSettingsScheduler
{
    Task ScheduleJobAsync(string jobName, string jobType, string cronExpression);
    Task UnscheduleJobAsync(string jobName);
    Task RescheduleJobAsync(string jobName, string jobType, string cronExpression);
}
