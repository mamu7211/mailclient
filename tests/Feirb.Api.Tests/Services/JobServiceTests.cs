using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Services;
using Feirb.Api.Tests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;

namespace Feirb.Api.Tests.Services;

public class JobServiceTests : IDisposable
{
    private readonly DbContextOptions<FeirbDbContext> _dbOptions;
    private readonly IJobSettingsScheduler _scheduler;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ManagedJobRegistry _registry;
    private readonly JobService _sut;

    public JobServiceTests()
    {
        var dbName = $"JobServiceTests-{Guid.NewGuid()}";
        _dbOptions = new DbContextOptionsBuilder<FeirbDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        _scheduler = new NoOpJobSettingsScheduler();
        _schedulerFactory = Substitute.For<ISchedulerFactory>();
        _registry = new ManagedJobRegistry([]);

        using var db = new FeirbDbContext(_dbOptions);
        db.Database.EnsureCreated();

        _sut = CreateService();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingJob_ReturnsJobAsync()
    {
        var jobId = GetSeededJobId();

        var result = await _sut.GetByIdAsync(jobId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(jobId);
        result.JobName.Should().Be("Classification");
        result.Cron.Should().Be("0 * * * * ?");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentJob_ReturnsNullAsync()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithExecutions_IncludesRecentExecutionsAsync()
    {
        var jobId = GetSeededJobId();
        SeedExecutions(jobId, 7);

        var result = await _sut.GetByIdAsync(jobId);

        result.Should().NotBeNull();
        result!.RecentExecutions.Should().HaveCount(5);
    }

    [Fact]
    public async Task TriggerRunAsync_NonExistentJob_ReturnsFalseAsync()
    {
        var result = await _sut.TriggerRunAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TriggerRunAsync_NoRegisteredJobType_ReturnsFalseAsync()
    {
        var jobId = GetSeededJobId();

        var result = await _sut.TriggerRunAsync(jobId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TriggerRunAsync_ExistingScheduledJob_TriggersViaQuartzAsync()
    {
        var jobId = GetSeededJobId();
        var registry = CreateRegistryWithClassification();
        var quartzScheduler = Substitute.For<IScheduler>();
        quartzScheduler.CheckExists(Arg.Any<JobKey>(), Arg.Any<CancellationToken>()).Returns(true);
        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>()).Returns(quartzScheduler);

        var sut = CreateService(registry: registry);
        var result = await sut.TriggerRunAsync(jobId);

        result.Should().BeTrue();
        await quartzScheduler.Received(1).TriggerJob(Arg.Any<JobKey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerRunAsync_DisabledJob_CreatesAdhocJobAsync()
    {
        var jobId = GetSeededJobId();
        var registry = CreateRegistryWithClassification();
        var quartzScheduler = Substitute.For<IScheduler>();
        quartzScheduler.CheckExists(Arg.Any<JobKey>(), Arg.Any<CancellationToken>()).Returns(false);
        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>()).Returns(quartzScheduler);

        var sut = CreateService(registry: registry);
        var result = await sut.TriggerRunAsync(jobId);

        result.Should().BeTrue();
        await quartzScheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());
    }

    // --- Helpers ---

    private JobService CreateService(ManagedJobRegistry? registry = null) =>
        new(
            new FeirbDbContext(_dbOptions),
            _scheduler,
            _schedulerFactory,
            registry ?? _registry,
            NullLoggerFactory.Instance.CreateLogger<JobService>());

    private Guid GetSeededJobId()
    {
        using var db = new FeirbDbContext(_dbOptions);
        return db.JobSettings.First().Id;
    }

    private void SeedExecutions(Guid jobId, int count)
    {
        using var db = new FeirbDbContext(_dbOptions);
        for (var i = 0; i < count; i++)
        {
            db.JobExecutions.Add(new JobExecution
            {
                Id = Guid.NewGuid(),
                JobSettingsId = jobId,
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-i),
                FinishedAt = DateTimeOffset.UtcNow.AddMinutes(-i).AddSeconds(1),
                Status = JobExecutionStatus.Success,
            });
        }

        db.SaveChanges();
    }

    private static ManagedJobRegistry CreateRegistryWithClassification()
    {
        var registration = Substitute.For<IManagedJobRegistration>();
        registration.JobName.Returns("Classification");
        registration.JobType.Returns(typeof(TestClassificationJob));
        return new ManagedJobRegistry([registration]);
    }

    private sealed class TestClassificationJob(IServiceScopeFactory scopeFactory, ILogger logger)
        : ManagedJob(scopeFactory, logger)
    {
        protected override Task RunAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
