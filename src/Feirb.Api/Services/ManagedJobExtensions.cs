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

    public static IServiceCollection AddManagedJob<TJob>(this IServiceCollection services, string jobTypeName)
        where TJob : ManagedJob
    {
        services.AddTransient<TJob>();

        services.AddSingleton<IManagedJobRegistration>(new ManagedJobRegistration(jobTypeName, typeof(TJob)));

        return services;
    }
}

public interface IManagedJobRegistration
{
    string JobTypeName { get; }
    Type ClrType { get; }
}

file record ManagedJobRegistration(string JobTypeName, Type ClrType) : IManagedJobRegistration;
