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

public class ManagedJobTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly DbContextOptions<FeirbDbContext> _dbOptions;

    public ManagedJobTests()
    {
        var dbName = $"ManagedJobTests-{Guid.NewGuid()}";
        _dbOptions = new DbContextOptionsBuilder<FeirbDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var services = new ServiceCollection();
        services.AddSingleton(_dbOptions);
        services.AddScoped(sp => new FeirbDbContext(sp.GetRequiredService<DbContextOptions<FeirbDbContext>>()));
        services.AddSingleton<IJobSettingsScheduler, NoOpJobSettingsScheduler>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        using var db = new FeirbDbContext(_dbOptions);
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Execute_SuccessfulRun_CreatesExecutionAndUpdatesLastRunAsync()
    {
        var jobSettingsId = SeedJobSettings();
        var job = CreateTestJob(succeeds: true);

        await job.Execute(CreateJobContext());

        using var db = new FeirbDbContext(_dbOptions);
        var updated = await db.JobSettings.AsNoTracking().FirstAsync(j => j.Id == jobSettingsId);
        updated.LastRunAt.Should().NotBeNull();
        updated.LastStatus.Should().Be(JobExecutionStatus.Success);

        var executions = await db.JobExecutions.Where(e => e.JobSettingsId == jobSettingsId).ToListAsync();
        executions.Should().ContainSingle();
        executions[0].Status.Should().Be(JobExecutionStatus.Success);
        executions[0].FinishedAt.Should().NotBeNull();
        executions[0].Error.Should().BeNull();
    }

    [Fact]
    public async Task Execute_FailedRun_RecordsErrorAsync()
    {
        var jobSettingsId = SeedJobSettings();
        var job = CreateTestJob(succeeds: false, errorMessage: "Something went wrong");

        await job.Execute(CreateJobContext());

        using var db = new FeirbDbContext(_dbOptions);
        var execution = await db.JobExecutions.AsNoTracking().FirstAsync(e => e.JobSettingsId == jobSettingsId);
        execution.Status.Should().Be(JobExecutionStatus.Failed);
        execution.Error.Should().Be("Something went wrong");

        var updated = await db.JobSettings.AsNoTracking().FirstAsync(j => j.Id == jobSettingsId);
        updated.LastStatus.Should().Be(JobExecutionStatus.Failed);
    }

    [Fact]
    public async Task Execute_TenConsecutiveFailures_AutoDisablesJobAsync()
    {
        var jobSettingsId = SeedJobSettings(enabled: true);
        var job = CreateTestJob(succeeds: false, errorMessage: "Failing");

        for (var i = 0; i < 10; i++)
        {
            await job.Execute(CreateJobContext());
        }

        using var db = new FeirbDbContext(_dbOptions);
        var updated = await db.JobSettings.AsNoTracking().FirstAsync(j => j.Id == jobSettingsId);
        updated.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Execute_NineFailuresThenSuccess_DoesNotDisableAsync()
    {
        var jobSettingsId = SeedJobSettings(enabled: true);

        var failingJob = CreateTestJob(succeeds: false, errorMessage: "Failing");
        for (var i = 0; i < 9; i++)
        {
            await failingJob.Execute(CreateJobContext());
        }

        var successJob = CreateTestJob(succeeds: true);
        await successJob.Execute(CreateJobContext());

        using var db = new FeirbDbContext(_dbOptions);
        var updated = await db.JobSettings.AsNoTracking().FirstAsync(j => j.Id == jobSettingsId);
        updated.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_AlreadyRunning_SkipsExecutionAsync()
    {
        var jobSettingsId = SeedJobSettings();

        // Simulate a running execution (FinishedAt is null)
        using (var db = new FeirbDbContext(_dbOptions))
        {
            db.JobExecutions.Add(new JobExecution
            {
                Id = Guid.NewGuid(),
                JobSettingsId = jobSettingsId,
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                FinishedAt = null,
                Status = JobExecutionStatus.Success,
            });
            await db.SaveChangesAsync();
        }

        var job = CreateTestJob(succeeds: true);
        await job.Execute(CreateJobContext());

        using var verifyDb = new FeirbDbContext(_dbOptions);
        var executions = await verifyDb.JobExecutions
            .Where(e => e.JobSettingsId == jobSettingsId)
            .OrderBy(e => e.StartedAt)
            .ToListAsync();
        executions.Should().HaveCount(2);
        executions[0].Status.Should().Be(JobExecutionStatus.Success, "the original running execution");
        executions[1].Status.Should().Be(JobExecutionStatus.Skipped, "the skipped execution");
        executions[1].FinishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_JobSettingsNotFound_SkipsWithoutErrorAsync()
    {
        var job = CreateTestJob(succeeds: true);

        var act = () => job.Execute(CreateJobContext());

        await act.Should().NotThrowAsync();
    }

    private Guid SeedJobSettings(bool enabled = false)
    {
        using var db = new FeirbDbContext(_dbOptions);
        var jobSettings = new JobSettings
        {
            Id = Guid.NewGuid(),
            JobName = "TestJob",
            Description = "A test job",
            Cron = "0 * * * * ?",
            Enabled = enabled,
        };
        db.JobSettings.Add(jobSettings);
        db.SaveChanges();
        return jobSettings.Id;
    }

    private TestManagedJob CreateTestJob(bool succeeds, string? errorMessage = null)
    {
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = NullLoggerFactory.Instance.CreateLogger<TestManagedJob>();
        return new TestManagedJob(scopeFactory, logger, succeeds, errorMessage);
    }

    private static IJobExecutionContext CreateJobContext()
    {
        var dataMap = new JobDataMap { { ManagedJob.JobNameKey, "TestJob" } };
        var context = Substitute.For<IJobExecutionContext>();
        context.MergedJobDataMap.Returns(dataMap);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    private sealed class TestManagedJob(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        bool succeeds,
        string? errorMessage) : ManagedJob(scopeFactory, logger)
    {
        protected override Task RunAsync(IServiceProvider serviceProvider, JobSettings jobSettings, CancellationToken cancellationToken)
        {
            if (!succeeds)
                throw new InvalidOperationException(errorMessage ?? "Test failure");
            return Task.CompletedTask;
        }
    }
}
