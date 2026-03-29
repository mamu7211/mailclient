namespace Feirb.Api.Services;

public static class ManagedJobExtensions
{
    public static IServiceCollection AddManagedJobInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ManagedJobRegistry>();
        services.AddSingleton<JobSettingsScheduler>();
        services.AddSingleton<IJobSettingsScheduler>(sp => sp.GetRequiredService<JobSettingsScheduler>());
        services.AddHostedService(sp => sp.GetRequiredService<JobSettingsScheduler>());
        services.AddScoped<IJobService, JobService>();
        return services;
    }

    public static IServiceCollection AddManagedJob<TJob>(this IServiceCollection services, string jobName)
        where TJob : ManagedJob
    {
        services.AddTransient<TJob>();

        services.AddSingleton<IManagedJobRegistration>(new ManagedJobRegistration(jobName, typeof(TJob)));

        return services;
    }
}

public interface IManagedJobRegistration
{
    string JobName { get; }
    Type JobType { get; }
}

file record ManagedJobRegistration(string JobName, Type JobType) : IManagedJobRegistration;
