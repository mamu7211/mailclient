using Feirb.Api.Data;
using Feirb.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Feirb.Api.Tests;

public static class TestWebApplicationFactory
{
    public static WebApplicationFactory<Program> Create(
        string dbName,
        IClassificationService? classificationServiceOverride = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                if (classificationServiceOverride is not null)
                {
                    services.RemoveAll<IClassificationService>();
                    services.AddScoped<IClassificationService>(_ => classificationServiceOverride);
                }

                // Replace IChatClient registrations (the Aspire/OllamaSharp client requires
                // a real connection string that doesn't exist in tests). Tests that exercise
                // classification provide their own IClassificationService stub above.
                var chatClientDescriptors = services
                    .Where(d => d.ServiceType == typeof(IChatClient))
                    .ToList();
                foreach (var d in chatClientDescriptors)
                    services.Remove(d);
                services.AddSingleton<IChatClient>(new NoopChatClient());

                // Replace PostgreSQL with in-memory database
                var dbDescriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<FeirbDbContext>) ||
                    d.ServiceType.FullName?.Contains("FeirbDbContext") == true ||
                    d.ServiceType.FullName?.Contains("Npgsql") == true).ToList();
                foreach (var d in dbDescriptors)
                    services.Remove(d);

                services.AddDbContext<FeirbDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                // Remove Quartz and scheduler hosted services to avoid
                // LoggerFactory disposal race during test teardown
                var hostedServiceDescriptors = services.Where(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    (d.ImplementationType?.FullName?.Contains("Quartz") == true ||
                     d.ImplementationType == typeof(JobSettingsScheduler) ||
                     d.ImplementationFactory is not null)).ToList();
                foreach (var d in hostedServiceDescriptors)
                    services.Remove(d);

                // Replace IJobSettingsScheduler with a no-op for test DI
                services.RemoveAll<IJobSettingsScheduler>();
                services.RemoveAll<JobSettingsScheduler>();
                services.AddSingleton<IJobSettingsScheduler, NoOpJobSettingsScheduler>();

                // Replace ISchedulerFactory with a no-op to avoid disposed Quartz scheduler
                services.RemoveAll<ISchedulerFactory>();
                services.AddSingleton<ISchedulerFactory, NoOpSchedulerFactory>();
            });
        });

    private sealed class NoopChatClient : IChatClient
    {
        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "[]")));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            EmptyAsyncEnumerableAsync();

        private static async IAsyncEnumerable<ChatResponseUpdate> EmptyAsyncEnumerableAsync()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class NoOpSchedulerFactory : ISchedulerFactory
    {
        public Task<IReadOnlyList<IScheduler>> GetAllSchedulers(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IScheduler>>([]);

        public Task<IScheduler> GetScheduler(CancellationToken cancellationToken = default) =>
            Task.FromResult(NSubstitute.Substitute.For<IScheduler>());

        public Task<IScheduler?> GetScheduler(string schedName, CancellationToken cancellationToken = default) =>
            Task.FromResult<IScheduler?>(NSubstitute.Substitute.For<IScheduler>());
    }
}
