using System.Net;
using System.Net.Http.Json;
using Feirb.Shared.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Feirb.Api.Tests.Endpoints;

public class AuthEndpointsLocalizationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointsLocalizationTests()
    {
        _factory = TestWebApplicationFactory.Create($"TestDb-{Guid.NewGuid()}");
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Register_DuplicateUsername_EnUs_ReturnsEnglishMessageAsync()
    {
        var client = _factory.CreateClient();
        var first = new RegisterRequest("dupeuser", "first@example.com", "Password123!");
        await client.PostAsJsonAsync("/api/auth/register", first);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
        request.Headers.AcceptLanguage.ParseAdd("en-US");
        request.Content = JsonContent.Create(new RegisterRequest("dupeuser", "second@example.com", "Password123!"));

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        body!.Message.Should().Be("Username is already taken.");
    }

    [Fact]
    public async Task Register_DuplicateUsername_DeDe_ReturnsGermanMessageAsync()
    {
        var client = _factory.CreateClient();
        var first = new RegisterRequest("dupeuser", "first@example.com", "Password123!");
        await client.PostAsJsonAsync("/api/auth/register", first);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
        request.Headers.AcceptLanguage.ParseAdd("de-DE");
        request.Content = JsonContent.Create(new RegisterRequest("dupeuser", "second@example.com", "Password123!"));

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        body!.Message.Should().Be("Der Benutzername ist bereits vergeben.");
    }

    [Fact]
    public async Task Register_DuplicateEmail_FrFr_ReturnsFrenchMessageAsync()
    {
        var client = _factory.CreateClient();
        var first = new RegisterRequest("user1", "same@example.com", "Password123!");
        await client.PostAsJsonAsync("/api/auth/register", first);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
        request.Headers.AcceptLanguage.ParseAdd("fr-FR");
        request.Content = JsonContent.Create(new RegisterRequest("user2", "same@example.com", "Password123!"));

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        body!.Message.Should().Be("Cette adresse e-mail est déjà enregistrée.");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ItIt_ReturnsItalianMessageAsync()
    {
        var client = _factory.CreateClient();
        var first = new RegisterRequest("user1", "same@example.com", "Password123!");
        await client.PostAsJsonAsync("/api/auth/register", first);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
        request.Headers.AcceptLanguage.ParseAdd("it-IT");
        request.Content = JsonContent.Create(new RegisterRequest("user2", "same@example.com", "Password123!"));

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        body!.Message.Should().Be("Questa e-mail è già registrata.");
    }

    [Fact]
    public async Task Register_DuplicateUsername_UnsupportedLocale_FallsBackToEnglishAsync()
    {
        var client = _factory.CreateClient();
        var first = new RegisterRequest("dupeuser", "first@example.com", "Password123!");
        await client.PostAsJsonAsync("/api/auth/register", first);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
        request.Headers.AcceptLanguage.ParseAdd("ja-JP");
        request.Content = JsonContent.Create(new RegisterRequest("dupeuser", "second@example.com", "Password123!"));

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        body!.Message.Should().Be("Username is already taken.");
    }

    private sealed record ErrorBody(string Message);
}
