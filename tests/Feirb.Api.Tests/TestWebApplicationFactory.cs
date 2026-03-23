using Feirb.Api.Data;
using Feirb.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Feirb.Api.Tests;

public static class TestWebApplicationFactory
{
    public static WebApplicationFactory<Program> Create(string dbName) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace PostgreSQL with in-memory database
                var dbDescriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<FeirbDbContext>) ||
                    d.ServiceType.FullName?.Contains("FeirbDbContext") == true ||
                    d.ServiceType.FullName?.Contains("Npgsql") == true).ToList();
                foreach (var d in dbDescriptors)
                    services.Remove(d);

                services.AddDbContext<FeirbDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                // Remove Quartz and ImapSyncScheduler hosted services to avoid
                // LoggerFactory disposal race during test teardown
                var hostedServiceDescriptors = services.Where(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    (d.ImplementationType?.FullName?.Contains("Quartz") == true ||
                     d.ImplementationType == typeof(ImapSyncScheduler) ||
                     d.ImplementationFactory is not null)).ToList();
                foreach (var d in hostedServiceDescriptors)
                    services.Remove(d);

                // Replace IImapSyncScheduler with a no-op for endpoint DI
                var schedulerDescriptors = services.Where(d =>
                    d.ServiceType == typeof(IImapSyncScheduler) ||
                    d.ServiceType == typeof(ImapSyncScheduler)).ToList();
                foreach (var d in schedulerDescriptors)
                    services.Remove(d);

                services.AddSingleton<IImapSyncScheduler, NoOpImapSyncScheduler>();
            });
        });

    private sealed class NoOpImapSyncScheduler : IImapSyncScheduler
    {
        public Task ScheduleMailboxAsync(Guid mailboxId, int pollIntervalMinutes, bool triggerImmediately = false) =>
            Task.CompletedTask;

        public Task UnscheduleMailboxAsync(Guid mailboxId) => Task.CompletedTask;
        public Task RescheduleMailboxAsync(Guid mailboxId, int pollIntervalMinutes) => Task.CompletedTask;
    }
}
