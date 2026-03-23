using System.Net;
using System.Net.Http.Json;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Services;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Feirb.Api.Tests.Endpoints;

public class SetupEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SetupEndpointsTests()
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
    public async Task GetStatus_NoAdmin_ReturnsNotCompleteAsync()
    {
        var response = await _client.GetAsync("/api/setup/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SetupStatusResponse>();
        result.Should().NotBeNull();
        result!.IsComplete.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatus_AdminExists_ReturnsCompleteAsync()
    {
        await SeedAdminUserAsync();

        var response = await _client.GetAsync("/api/setup/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SetupStatusResponse>();
        result.Should().NotBeNull();
        result!.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteSetup_NoAdmin_CreatesAdminAndSmtpSettingsAsync()
    {
        var request = new CompleteSetupRequest(
            "admin",
            "admin@example.com",
            "Password123!",
            "smtp.example.com",
            587,
            "smtp-user",
            "smtp-password",
            true,
            false);

        var response = await _client.PostAsJsonAsync("/api/setup/complete", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        admin.Should().NotBeNull();
        admin!.IsAdmin.Should().BeTrue();
        admin.Email.Should().Be("admin@example.com");
        admin.PasswordHash.Should().NotBe("Password123!");

        var smtp = await db.SmtpSettings.FirstOrDefaultAsync();
        smtp.Should().NotBeNull();
        smtp!.Host.Should().Be("smtp.example.com");
        smtp.Port.Should().Be(587);
        smtp.Username.Should().Be("smtp-user");
        smtp.UseTls.Should().BeTrue();
        smtp.EncryptedPassword.Should().NotBe("smtp-password");
    }

    [Fact]
    public async Task CompleteSetup_AdminExists_Returns409ConflictAsync()
    {
        await SeedAdminUserAsync();

        var request = new CompleteSetupRequest(
            "admin2",
            "admin2@example.com",
            "Password123!",
            "smtp.example.com",
            587,
            "smtp-user",
            "smtp-password",
            true,
            false);

        var response = await _client.PostAsJsonAsync("/api/setup/complete", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CompleteSetup_FullFlow_StatusBecomesCompleteAsync()
    {
        var statusBefore = await _client.GetFromJsonAsync<SetupStatusResponse>("/api/setup/status");
        statusBefore!.IsComplete.Should().BeFalse();

        var request = new CompleteSetupRequest(
            "admin",
            "admin@example.com",
            "Password123!",
            "smtp.example.com",
            587,
            "smtp-user",
            "smtp-password",
            true,
            false);

        await _client.PostAsJsonAsync("/api/setup/complete", request);

        var statusAfter = await _client.GetFromJsonAsync<SetupStatusResponse>("/api/setup/status");
        statusAfter!.IsComplete.Should().BeTrue();
    }

    private async Task SeedAdminUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "existingadmin",
            Email = "existing@example.com",
            PasswordHash = authService.HashPassword("Password123!"),
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
