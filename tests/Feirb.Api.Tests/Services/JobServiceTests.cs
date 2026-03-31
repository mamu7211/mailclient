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
    private static readonly Guid _adminUserId = Guid.NewGuid();
    private static readonly Guid _regularUserId = Guid.NewGuid();
    private static readonly Guid _otherUserId = Guid.NewGuid();

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
    public async Task GetByIdAsync_AsAdmin_ReturnsAnyJobAsync()
    {
        var jobId = GetSeededJobId();

        var result = await _sut.GetByIdAsync(jobId, _adminUserId, isAdmin: true);

        result.Should().NotBeNull();
        result!.Id.Should().Be(jobId);
        result.JobName.Should().Be("Classification");
    }

    [Fact]
    public async Task GetByIdAsync_AsUser_ReturnsSystemJobAsync()
    {
        var jobId = GetSeededJobId();

        var result = await _sut.GetByIdAsync(jobId, _regularUserId, isAdmin: false);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_AsUser_ReturnsNullForOtherUsersJobAsync()
    {
        var jobId = SeedUserJob(_otherUserId);

        var result = await _sut.GetByIdAsync(jobId, _regularUserId, isAdmin: false);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_AsUser_ReturnsOwnJobAsync()
    {
        var jobId = SeedUserJob(_regularUserId);

        var result = await _sut.GetByIdAsync(jobId, _regularUserId, isAdmin: false);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentJob_ReturnsNullAsync()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), _adminUserId, isAdmin: true);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithExecutions_IncludesRecentExecutionsAsync()
    {
        var jobId = GetSeededJobId();
        SeedExecutions(jobId, 7);

        var result = await _sut.GetByIdAsync(jobId, _adminUserId, isAdmin: true);

        result.Should().NotBeNull();
        result!.RecentExecutions.Should().HaveCount(5);
    }

    [Fact]
    public async Task TriggerRunAsync_NonExistentJob_ReturnsFalseAsync()
    {
        var result = await _sut.TriggerRunAsync(Guid.NewGuid(), _adminUserId, isAdmin: true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TriggerRunAsync_NoRegisteredJobType_ReturnsFalseAsync()
    {
        var jobId = GetSeededJobId();

        var result = await _sut.TriggerRunAsync(jobId, _adminUserId, isAdmin: true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TriggerRunAsync_AsUser_CannotTriggerSystemJobAsync()
    {
        var jobId = GetSeededJobId();
        var registry = CreateRegistryWithClassification();
        var sut = CreateService(registry: registry);

        var result = await sut.TriggerRunAsync(jobId, _regularUserId, isAdmin: false);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TriggerRunAsync_AsUser_CannotTriggerOtherUsersJobAsync()
    {
        var jobId = SeedUserJob(_otherUserId, jobType: "classification");
        var registry = CreateRegistryWithClassification();
        var quartzScheduler = Substitute.For<IScheduler>();
        quartzScheduler.CheckExists(Arg.Any<JobKey>(), Arg.Any<CancellationToken>()).Returns(true);
        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>()).Returns(quartzScheduler);

        var sut = CreateService(registry: registry);
        var result = await sut.TriggerRunAsync(jobId, _regularUserId, isAdmin: false);

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
        var result = await sut.TriggerRunAsync(jobId, _adminUserId, isAdmin: true);

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
        var result = await sut.TriggerRunAsync(jobId, _adminUserId, isAdmin: true);

        result.Should().BeTrue();
        await quartzScheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_DisablingJobWithNullJobType_CallsUnscheduleAsync()
    {
        var mockScheduler = Substitute.For<IJobSettingsScheduler>();
        var jobId = SeedUserJob(_adminUserId, jobType: null!);

        using var db = new FeirbDbContext(_dbOptions);
        var job = db.JobSettings.First(j => j.Id == jobId);
        var rowVersion = job.RowVersion;
        var jobName = job.JobName;

        var sut = CreateService(scheduler: mockScheduler);
        var request = new Feirb.Shared.Admin.Jobs.UpdateJobSettingsRequest("0 */5 * * * ?", false, rowVersion);
        var result = await sut.UpdateAsync(jobId, request, _adminUserId, isAdmin: true);

        result.Should().NotBeNull();
        result!.Enabled.Should().BeFalse();
        await mockScheduler.Received(1).UnscheduleJobAsync(jobName);
    }

    [Fact]
    public async Task UpdateAsync_AsUser_CannotUpdateSystemJobAsync()
    {
        var jobId = GetSeededJobId();
        using var db = new FeirbDbContext(_dbOptions);
        var rowVersion = db.JobSettings.First().RowVersion;

        var request = new Feirb.Shared.Admin.Jobs.UpdateJobSettingsRequest("0 */5 * * * ?", true, rowVersion);
        var result = await _sut.UpdateAsync(jobId, request, _regularUserId, isAdmin: false);

        result.Should().BeNull();
    }

    // --- Helpers ---

    private JobService CreateService(ManagedJobRegistry? registry = null, IJobSettingsScheduler? scheduler = null) =>
        new(
            new FeirbDbContext(_dbOptions),
            scheduler ?? _scheduler,
            _schedulerFactory,
            registry ?? _registry,
            NullLoggerFactory.Instance.CreateLogger<JobService>());

    private Guid GetSeededJobId()
    {
        using var db = new FeirbDbContext(_dbOptions);
        return db.JobSettings.First().Id;
    }

    private Guid SeedUserJob(Guid userId, string jobType = "test-job")
    {
        using var db = new FeirbDbContext(_dbOptions);
        var job = new JobSettings
        {
            Id = Guid.NewGuid(),
            JobName = $"user-job-{Guid.NewGuid():N}",
            JobType = jobType,
            Description = "User-scoped test job",
            Cron = "0 * * * * ?",
            Enabled = true,
            UserId = userId,
        };
        db.JobSettings.Add(job);
        db.SaveChanges();
        return job.Id;
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
        registration.JobTypeName.Returns("classification");
        registration.ClrType.Returns(typeof(TestClassificationJob));
        return new ManagedJobRegistry([registration]);
    }

    private sealed class TestClassificationJob(IServiceScopeFactory scopeFactory, ILogger logger)
        : ManagedJob(scopeFactory, logger)
    {
        protected override Task RunAsync(IServiceProvider serviceProvider, JobSettings jobSettings, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
