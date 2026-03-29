using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Shared.Admin.Jobs;
using Feirb.Shared.Auth;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Feirb.Api.Tests.Endpoints;

public class JobSettingsEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public JobSettingsEndpointsTests()
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
    public async Task GetAllJobs_AsAdmin_ReturnsSeededJobsAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/admin/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jobs = await response.Content.ReadFromJsonAsync<List<JobSettingsResponse>>();
        jobs.Should().NotBeNull();
        jobs.Should().ContainSingle();
        jobs![0].JobName.Should().Be("Classification");
        jobs[0].Enabled.Should().BeFalse();
        jobs[0].Cron.Should().Be("0 * * * * ?");
    }

    [Fact]
    public async Task GetAllJobs_Unauthenticated_ReturnsUnauthorizedAsync()
    {
        var response = await _client.GetAsync("/api/admin/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllJobs_AsNonAdmin_ReturnsForbiddenAsync()
    {
        await SetupAndLoginAsAdminAsync();
        var tokens = await CreateAndLoginAsRegularUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/admin/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateJob_AsAdmin_UpdatesCronAndEnabledAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var jobs = await _client.GetFromJsonAsync<List<JobSettingsResponse>>("/api/admin/jobs");
        var job = jobs![0];

        var request = new UpdateJobSettingsRequest("0 */5 * * * ?", true, job.RowVersion);
        var response = await _client.PutAsJsonAsync($"/api/admin/jobs/{job.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<JobSettingsResponse>();
        updated.Should().NotBeNull();
        updated!.Cron.Should().Be("0 */5 * * * ?");
        updated.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateJob_InvalidCron_ReturnsBadRequestAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var jobs = await _client.GetFromJsonAsync<List<JobSettingsResponse>>("/api/admin/jobs");
        var job = jobs![0];

        var request = new UpdateJobSettingsRequest("not-a-cron", false, job.RowVersion);
        var response = await _client.PutAsJsonAsync($"/api/admin/jobs/{job.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateJob_NonExistentId_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var request = new UpdateJobSettingsRequest("0 * * * * ?", true, Guid.Empty);
        var response = await _client.PutAsJsonAsync($"/api/admin/jobs/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetJobExecutions_AsAdmin_ReturnsPaginatedResultAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var jobs = await _client.GetFromJsonAsync<List<JobSettingsResponse>>("/api/admin/jobs");
        var job = jobs![0];

        // Seed some executions
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
            for (var i = 0; i < 3; i++)
            {
                db.JobExecutions.Add(new JobExecution
                {
                    Id = Guid.NewGuid(),
                    JobSettingsId = job.Id,
                    StartedAt = DateTime.UtcNow.AddMinutes(-i),
                    FinishedAt = DateTime.UtcNow.AddMinutes(-i).AddSeconds(1),
                    Status = JobExecutionStatus.Success,
                });
            }
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/admin/jobs/{job.Id}/executions?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedJobExecutionsResponse>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetAllJobs_WithRecentExecutions_IncludesLast5Async()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var jobs = await _client.GetFromJsonAsync<List<JobSettingsResponse>>("/api/admin/jobs");
        var job = jobs![0];

        // Seed 7 executions
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
            for (var i = 0; i < 7; i++)
            {
                db.JobExecutions.Add(new JobExecution
                {
                    Id = Guid.NewGuid(),
                    JobSettingsId = job.Id,
                    StartedAt = DateTime.UtcNow.AddMinutes(-i),
                    FinishedAt = DateTime.UtcNow.AddMinutes(-i).AddSeconds(1),
                    Status = JobExecutionStatus.Success,
                });
            }
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/admin/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<JobSettingsResponse>>();
        result![0].RecentExecutions.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetJobById_AsAdmin_ReturnsJobAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var jobs = await _client.GetFromJsonAsync<List<JobSettingsResponse>>("/api/admin/jobs");
        var job = jobs![0];

        var response = await _client.GetAsync($"/api/admin/jobs/{job.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JobSettingsResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
        result.JobName.Should().Be("Classification");
        result.Cron.Should().Be("0 * * * * ?");
        result.RecentExecutions.Should().NotBeNull();
    }

    [Fact]
    public async Task GetJobById_NonExistentId_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync($"/api/admin/jobs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TriggerJobRun_NonExistentId_ReturnsNotFoundAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.PostAsync($"/api/admin/jobs/{Guid.NewGuid()}/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Helper Methods ---

    private async Task<TokenResponse> SetupAndLoginAsAdminAsync()
    {
        var setupRequest = new CompleteSetupRequest(
            "admin", "admin@example.com", "AdminPassword123!",
            "smtp.example.com", 587, "smtp@example.com", "smtppass", true, true);
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
}
