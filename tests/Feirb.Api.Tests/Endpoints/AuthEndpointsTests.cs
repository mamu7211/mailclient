using System.Net;
using System.Net.Http.Json;
using Feirb.Api.Data;
using Feirb.Shared.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Feirb.Api.Tests.Endpoints;

public class AuthEndpointsTests : IDisposable
{
    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests()
    {
        var dbName = _dbName;
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all DbContext-related registrations (Aspire pools + Npgsql)
                var dbDescriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<FeirbDbContext>) ||
                    d.ServiceType.FullName?.Contains("FeirbDbContext") == true ||
                    d.ServiceType.FullName?.Contains("Npgsql") == true).ToList();
                foreach (var d in dbDescriptors)
                    services.Remove(d);

                // Add in-memory database with a shared name per test
                services.AddDbContext<FeirbDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        });
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreatedAsync()
    {
        var request = new RegisterRequest("testuser", "test@example.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsConflictAsync()
    {
        var first = new RegisterRequest("dupeuser", "first@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", first);

        var second = new RegisterRequest("dupeuser", "second@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/register", second);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflictAsync()
    {
        var first = new RegisterRequest("user1", "same@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", first);

        var second = new RegisterRequest("user2", "same@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/register", second);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_ValidRequest_PasswordIsHashedInDatabaseAsync()
    {
        var request = new RegisterRequest("hashcheck", "hash@example.com", "Password123!");

        await _client.PostAsJsonAsync("/api/auth/register", request);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == "hashcheck");

        user.Should().NotBeNull();
        user!.PasswordHash.Should().NotBe("Password123!");
        user.PasswordHash.Should().StartWith("$2");
    }
}
