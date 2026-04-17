using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;

namespace Feirb.Api.Tests.Services;

public class JobSettingsSchedulerTests : IDisposable
{
    private readonly DbContextOptions<FeirbDbContext> _dbOptions;
    private readonly ServiceProvider _serviceProvider;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IScheduler _quartzScheduler;

    public JobSettingsSchedulerTests()
    {
        var dbName = $"JobSettingsSchedulerTests-{Guid.NewGuid()}";
        _dbOptions = new DbContextOptionsBuilder<FeirbDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var services = new ServiceCollection();
        services.AddSingleton(_dbOptions);
        services.AddScoped(sp => new FeirbDbContext(sp.GetRequiredService<DbContextOptions<FeirbDbContext>>()));

        _serviceProvider = services.BuildServiceProvider();

        _quartzScheduler = Substitute.For<IScheduler>();
        _quartzScheduler.IsStarted.Returns(true);

        _schedulerFactory = Substitute.For<ISchedulerFactory>();
        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>()).Returns(_quartzScheduler);

        using var db = new FeirbDbContext(_dbOptions);
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExecuteAsync_SchedulesAllEnabledJobsOnStartupAsync()
    {
        SeedJobSettings("job-a", "classification", enabled: true);
        SeedJobSettings("job-b", "classification", enabled: true);
        SeedJobSettings("job-c", "classification", enabled: false);

        var callCount = 0;
        var completed = new TaskCompletionSource();
        _quartzScheduler.ScheduleJob(Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(DateTimeOffset.UtcNow)
            .AndDoes(_ => { if (Interlocked.Increment(ref callCount) >= 2) completed.TrySetResult(); });

        var sut = CreateScheduler(withClassificationJob: true);
        await sut.StartAsync(CancellationToken.None);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await sut.StopAsync(CancellationToken.None);

        await _quartzScheduler.Received(2).ScheduleJob(
            Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SkipsJobsWithUnregisteredJobTypeAsync()
    {
        SeedJobSettings("job-unknown", "unknown-type", enabled: true);

        // Signal when GetScheduler is called — ExecuteAsync queries the DB after this
        var schedulerRequested = new TaskCompletionSource();
        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>())
            .Returns(_quartzScheduler)
            .AndDoes(_ => schedulerRequested.TrySetResult());

        var sut = CreateScheduler(withClassificationJob: true);
        await sut.StartAsync(CancellationToken.None);
        await schedulerRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        // Give ExecuteAsync time to process the DB query after GetScheduler returns
        await Task.Yield();
        await sut.StopAsync(CancellationToken.None);

        await _quartzScheduler.DidNotReceive().ScheduleJob(
            Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleJobAsync_RegistersQuartzJobWithCronTriggerAsync()
    {
        var sut = CreateScheduler(withClassificationJob: true);

        await sut.ScheduleJobAsync("my-job", "classification", "0 */5 * * * ?");

        await _quartzScheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key.Name == "managed-my-job" && j.Key.Group == "managed-jobs"),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleJobAsync_UnregisteredJobType_DoesNotScheduleAsync()
    {
        var sut = CreateScheduler(withClassificationJob: false);

        await sut.ScheduleJobAsync("my-job", "unknown-type", "0 */5 * * * ?");

        await _quartzScheduler.DidNotReceive().ScheduleJob(
            Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnscheduleJobAsync_ExistingJob_DeletesItAsync()
    {
        var jobKey = new JobKey("managed-my-job", "managed-jobs");
        _quartzScheduler.CheckExists(jobKey, Arg.Any<CancellationToken>()).Returns(true);

        var sut = CreateScheduler(withClassificationJob: true);

        await sut.UnscheduleJobAsync("my-job");

        await _quartzScheduler.Received(1).DeleteJob(jobKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnscheduleJobAsync_NonExistentJob_DoesNothingAsync()
    {
        var jobKey = new JobKey("managed-my-job", "managed-jobs");
        _quartzScheduler.CheckExists(jobKey, Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateScheduler(withClassificationJob: true);

        await sut.UnscheduleJobAsync("my-job");

        await _quartzScheduler.DidNotReceive().DeleteJob(Arg.Any<JobKey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescheduleJobAsync_DeletesAndReCreatesJobAsync()
    {
        var jobKey = new JobKey("managed-my-job", "managed-jobs");
        _quartzScheduler.CheckExists(jobKey, Arg.Any<CancellationToken>()).Returns(true);

        var sut = CreateScheduler(withClassificationJob: true);

        await sut.RescheduleJobAsync("my-job", "classification", "0 */10 * * * ?");

        Received.InOrder(() =>
        {
            _quartzScheduler.DeleteJob(jobKey, Arg.Any<CancellationToken>());
            _quartzScheduler.ScheduleJob(
                Arg.Is<IJobDetail>(j => j.Key.Name == "managed-my-job"),
                Arg.Any<ITrigger>(),
                Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RescheduleJobAsync_NonExistentJob_CreatesWithoutDeleteAsync()
    {
        var jobKey = new JobKey("managed-my-job", "managed-jobs");
        _quartzScheduler.CheckExists(jobKey, Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateScheduler(withClassificationJob: true);

        await sut.RescheduleJobAsync("my-job", "classification", "0 */10 * * * ?");

        await _quartzScheduler.DidNotReceive().DeleteJob(Arg.Any<JobKey>(), Arg.Any<CancellationToken>());
        await _quartzScheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescheduleJobAsync_UnregisteredJobType_DoesNothingAsync()
    {
        var sut = CreateScheduler(withClassificationJob: false);

        await sut.RescheduleJobAsync("my-job", "unknown-type", "0 */10 * * * ?");

        await _quartzScheduler.DidNotReceive().DeleteJob(Arg.Any<JobKey>(), Arg.Any<CancellationToken>());
        await _quartzScheduler.DidNotReceive().ScheduleJob(
            Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_MarksOrphanedExecutionsAsFailedOnStartupAsync()
    {
        var jobId = SeedJobSettings("job-a", "classification", enabled: true);
        var orphanStarted = DateTimeOffset.UtcNow.AddMinutes(-5);
        var orphanId = SeedJobExecution(jobId, startedAt: orphanStarted, finishedAt: null, status: JobExecutionStatus.Success);

        var schedulerRequested = new TaskCompletionSource();
        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>())
            .Returns(_quartzScheduler)
            .AndDoes(_ => schedulerRequested.TrySetResult());

        var sut = CreateScheduler(withClassificationJob: true);
        await sut.StartAsync(CancellationToken.None);
        await schedulerRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));

        JobExecution? recovered = null;
        JobSettings? updatedJob = null;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            using var verifyDb = new FeirbDbContext(_dbOptions);
            recovered = await verifyDb.JobExecutions.FindAsync(orphanId);
            updatedJob = await verifyDb.JobSettings.FindAsync(jobId);
            if (recovered?.FinishedAt is not null) break;
            await Task.Delay(50);
        }
        await sut.StopAsync(CancellationToken.None);

        recovered.Should().NotBeNull();
        recovered!.Status.Should().Be(JobExecutionStatus.Failed);
        recovered.FinishedAt.Should().NotBeNull();
        recovered.Error.Should().Contain("interrupted");
        updatedJob!.LastStatus.Should().Be(JobExecutionStatus.Failed);
    }

    [Fact]
    public async Task ExecuteAsync_LeavesCompletedExecutionsAloneAsync()
    {
        var jobId = SeedJobSettings("job-a", "classification", enabled: true);
        var completedId = SeedJobExecution(
            jobId,
            startedAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            finishedAt: DateTimeOffset.UtcNow.AddMinutes(-9),
            status: JobExecutionStatus.Success);

        var completed = new TaskCompletionSource();
        _quartzScheduler.ScheduleJob(Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(DateTimeOffset.UtcNow)
            .AndDoes(_ => completed.TrySetResult());

        var sut = CreateScheduler(withClassificationJob: true);
        await sut.StartAsync(CancellationToken.None);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await sut.StopAsync(CancellationToken.None);

        using var verifyDb = new FeirbDbContext(_dbOptions);
        var unchanged = await verifyDb.JobExecutions.FindAsync(completedId);
        unchanged!.Status.Should().Be(JobExecutionStatus.Success);
    }

    // --- Helpers ---

    private JobSettingsScheduler CreateScheduler(bool withClassificationJob)
    {
        var registry = withClassificationJob
            ? CreateRegistryWithClassification()
            : new ManagedJobRegistry([]);

        return new JobSettingsScheduler(
            _schedulerFactory,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            registry,
            NullLoggerFactory.Instance.CreateLogger<JobSettingsScheduler>());
    }

    private Guid SeedJobSettings(string jobName, string jobType, bool enabled)
    {
        using var db = new FeirbDbContext(_dbOptions);
        var id = Guid.NewGuid();
        db.JobSettings.Add(new JobSettings
        {
            Id = id,
            JobName = jobName,
            JobType = jobType,
            Description = $"Test job {jobName}",
            Cron = "0 * * * * ?",
            Enabled = enabled,
        });
        db.SaveChanges();
        return id;
    }

    private Guid SeedJobExecution(Guid jobSettingsId, DateTimeOffset startedAt, DateTimeOffset? finishedAt, JobExecutionStatus status)
    {
        using var db = new FeirbDbContext(_dbOptions);
        var id = Guid.NewGuid();
        db.JobExecutions.Add(new JobExecution
        {
            Id = id,
            JobSettingsId = jobSettingsId,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            Status = status,
        });
        db.SaveChanges();
        return id;
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
        protected override Task<JobRunResult> RunAsync(IServiceProvider serviceProvider, JobSettings jobSettings, CancellationToken cancellationToken) =>
            Task.FromResult(JobRunResult.Succeeded);
    }
}
