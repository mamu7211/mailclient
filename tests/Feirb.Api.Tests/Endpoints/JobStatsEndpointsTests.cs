using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Shared.Admin.Jobs;
using Feirb.Shared.Auth;
using Feirb.Shared.Settings;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Feirb.Api.Tests.Endpoints;

public class JobStatsEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public JobStatsEndpointsTests()
    {
        _factory = TestWebApplicationFactory.Create($"TestDb-{Guid.NewGuid()}");
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetStats_ReturnsGroupedExecutionCountsAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        await SeedJobExecutionsAsync();

        var response = await _client.GetAsync("/api/admin/jobs/stats?days=7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<JobExecutionStatsResponse>();
        stats.Should().NotBeNull();
        stats!.TimeSeries.Should().NotBeEmpty();

        var totalSuccess = stats.TimeSeries.Sum(d => d.SuccessCount);
        var totalFailed = stats.TimeSeries.Sum(d => d.FailedCount);
        var totalCancelled = stats.TimeSeries.Sum(d => d.CancelledCount);
        totalSuccess.Should().Be(2);
        totalFailed.Should().Be(1);
        totalCancelled.Should().Be(1);
    }

    [Fact]
    public async Task GetStats_DefaultsTo7DaysAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/admin/jobs/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<JobExecutionStatsResponse>();
        stats.Should().NotBeNull();
        stats!.TimeSeries.Count.Should().BeInRange(7, 8);
    }

    [Fact]
    public async Task GetStats_NonAdmin_ReturnsForbiddenAsync()
    {
        await SetupAndLoginAsAdminAsync();
        var tokens = await CreateAndLoginAsRegularUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/admin/jobs/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUnhealthy_ReturnsFailedAndDisabledJobsAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        await SeedUnhealthyJobsAsync();

        var response = await _client.GetAsync("/api/admin/jobs/unhealthy");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UnhealthyJobsResponse>();
        result.Should().NotBeNull();
        // 2 seeded + 1 pre-existing Classification job (disabled by default)
        result!.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
        result.Items.Should().Contain(j => j.Status == "Failed");
        result.Items.Should().Contain(j => j.Status == "Disabled");
    }

    [Fact]
    public async Task GetUnhealthy_ExcludesHealthyJobsAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/admin/jobs/unhealthy");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UnhealthyJobsResponse>();
        result.Should().NotBeNull();
        // The seeded Classification job is disabled by default, so it should appear
        // but no failed jobs should exist without seeding
        result!.Items.Should().OnlyContain(j => j.Status == "Disabled" || j.Status == "Failed");
    }

    [Fact]
    public async Task GetUnhealthy_PaginationAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        await SeedUnhealthyJobsAsync();

        var response = await _client.GetAsync("/api/admin/jobs/unhealthy?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UnhealthyJobsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.TotalCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task GetUnhealthy_NonAdmin_ReturnsForbiddenAsync()
    {
        await SetupAndLoginAsAdminAsync();
        var tokens = await CreateAndLoginAsRegularUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/admin/jobs/unhealthy");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Helper Methods ---

    private async Task<TokenResponse> SetupAndLoginAsAdminAsync()
    {
        var setupRequest = new CompleteSetupRequest(
            "admin", "admin@example.com", "AdminPassword123!",
            "smtp.example.com", 587, "smtp@example.com", "smtppass", TlsMode.Auto, true);
        await _client.PostAsJsonAsync("/api/setup/complete", setupRequest);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin", "AdminPassword123!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        tokens.Should().NotBeNull();
        return tokens!;
    }

    private async Task<TokenResponse> CreateAndLoginAsRegularUserAsync()
    {
        var registerRequest = new RegisterRequest("regularuser", "regular@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("regularuser", "Password123!"));
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        return tokens!;
    }

    private async Task SeedJobExecutionsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        var job = await db.JobSettings.FindAsync(db.JobSettings.Select(j => j.Id).First());
        if (job is null) return;

        var now = DateTimeOffset.UtcNow;
        db.JobExecutions.AddRange(
            new JobExecution { Id = Guid.NewGuid(), JobSettingsId = job.Id, StartedAt = now.AddHours(-1), FinishedAt = now.AddHours(-1).AddMinutes(1), Status = JobExecutionStatus.Success },
            new JobExecution { Id = Guid.NewGuid(), JobSettingsId = job.Id, StartedAt = now.AddHours(-2), FinishedAt = now.AddHours(-2).AddMinutes(1), Status = JobExecutionStatus.Success },
            new JobExecution { Id = Guid.NewGuid(), JobSettingsId = job.Id, StartedAt = now.AddHours(-3), FinishedAt = now.AddHours(-3).AddMinutes(1), Status = JobExecutionStatus.Failed, Error = "Test error message" },
            new JobExecution { Id = Guid.NewGuid(), JobSettingsId = job.Id, StartedAt = now.AddHours(-4), FinishedAt = now.AddHours(-4).AddMinutes(1), Status = JobExecutionStatus.Cancelled }
        );
        await db.SaveChangesAsync();
    }

    private async Task SeedUnhealthyJobsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        var now = DateTimeOffset.UtcNow;

        var failedJob = new JobSettings
        {
            Id = Guid.NewGuid(),
            JobName = "FailedTestJob",
            Description = "A job that failed",
            Cron = "0 * * * * ?",
            Enabled = true,
            LastStatus = JobExecutionStatus.Failed,
            LastRunAt = now.AddMinutes(-10),
        };
        db.JobSettings.Add(failedJob);

        db.JobExecutions.Add(new JobExecution
        {
            Id = Guid.NewGuid(),
            JobSettingsId = failedJob.Id,
            StartedAt = now.AddMinutes(-10),
            FinishedAt = now.AddMinutes(-9),
            Status = JobExecutionStatus.Failed,
            Error = "Connection refused",
        });

        var disabledJob = new JobSettings
        {
            Id = Guid.NewGuid(),
            JobName = "DisabledTestJob",
            Description = "A job that was auto-disabled",
            Cron = "0 * * * * ?",
            Enabled = false,
            LastStatus = JobExecutionStatus.Failed,
            LastRunAt = now.AddMinutes(-20),
        };
        db.JobSettings.Add(disabledJob);

        await db.SaveChangesAsync();
    }
}
