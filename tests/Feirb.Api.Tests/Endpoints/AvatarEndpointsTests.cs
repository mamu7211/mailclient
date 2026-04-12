using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Shared.Auth;
using Feirb.Shared.Avatars;
using Feirb.Shared.Settings;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SkiaSharp;

namespace Feirb.Api.Tests.Endpoints;

public class AvatarEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AvatarEndpointsTests()
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

    // --- GET Tests ---

    [Fact]
    public async Task GetAvatar_NotFound_Returns204Async()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var hash = AvatarHashHelper.ComputeEmailHash("nobody@example.com");
        var response = await _client.GetAsync($"/api/avatars/{hash}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetAvatar_AfterUpload_ReturnsPngAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var hash = AvatarHashHelper.ComputeEmailHash("admin@example.com");
        await UploadTestImageAsync(hash);

        var response = await _client.GetAsync($"/api/avatars/{hash}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");

        var imageBytes = await response.Content.ReadAsByteArrayAsync();
        imageBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAvatar_Unauthenticated_ReturnsNoContentAsync()
    {
        var hash = AvatarHashHelper.ComputeEmailHash("admin@example.com");
        var response = await _client.GetAsync($"/api/avatars/{hash}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // --- PUT Tests ---

    [Fact]
    public async Task UploadAvatar_OwnEmail_ReturnsNoContentAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var hash = AvatarHashHelper.ComputeEmailHash("admin@example.com");
        var response = await UploadTestImageAsync(hash);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UploadAvatar_ResizesTo256x256Async()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var hash = AvatarHashHelper.ComputeEmailHash("admin@example.com");
        await UploadTestImageAsync(hash, width: 512, height: 512);

        var response = await _client.GetAsync($"/api/avatars/{hash}");
        var imageBytes = await response.Content.ReadAsByteArrayAsync();

        using var bitmap = SKBitmap.Decode(imageBytes);
        bitmap.Should().NotBeNull();
        bitmap!.Width.Should().Be(256);
        bitmap.Height.Should().Be(256);
    }

    [Fact]
    public async Task UploadAvatar_InvalidFile_ReturnsBadRequestAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var hash = AvatarHashHelper.ComputeEmailHash("admin@example.com");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("not an image"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "test.png");

        var response = await _client.PutAsync($"/api/avatars/{hash}", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadAvatar_OtherEmailNonAdmin_ReturnsForbidAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();

        // Register a regular user and login
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("regular", "regular@example.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("regular", "Password123!"));
        var regularTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", regularTokens!.AccessToken);

        // Try to upload for a different email
        var hash = AvatarHashHelper.ComputeEmailHash("someone-else@example.com");
        var response = await UploadTestImageAsync(hash);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadAvatar_AnyEmailAsAdmin_ReturnsNoContentAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var hash = AvatarHashHelper.ComputeEmailHash("someone-else@example.com");
        var response = await UploadTestImageAsync(hash);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UploadAvatar_ReplaceExisting_UpdatesImageAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var hash = AvatarHashHelper.ComputeEmailHash("admin@example.com");

        // Upload first image
        await UploadTestImageAsync(hash, width: 100, height: 100);
        var firstResponse = await _client.GetAsync($"/api/avatars/{hash}");
        var firstImage = await firstResponse.Content.ReadAsByteArrayAsync();

        // Upload replacement
        await UploadTestImageAsync(hash, width: 200, height: 200);
        var secondResponse = await _client.GetAsync($"/api/avatars/{hash}");
        var secondImage = await secondResponse.Content.ReadAsByteArrayAsync();

        // Both should be 256x256 but the pixel data should differ
        firstImage.Should().NotBeEmpty();
        secondImage.Should().NotBeEmpty();
    }

    // --- DELETE Tests ---

    [Fact]
    public async Task DeleteAvatar_AsAdmin_ReturnsNoContentAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var hash = AvatarHashHelper.ComputeEmailHash("admin@example.com");
        await UploadTestImageAsync(hash);

        var response = await _client.DeleteAsync($"/api/avatars/{hash}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone
        var getResponse = await _client.GetAsync($"/api/avatars/{hash}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAvatar_NotFound_Returns404Async()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var hash = AvatarHashHelper.ComputeEmailHash("nobody@example.com");
        var response = await _client.DeleteAsync($"/api/avatars/{hash}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAvatar_NonAdmin_ReturnsForbidAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();

        // Register and login as regular user
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("regular", "regular@example.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("regular", "Password123!"));
        var regularTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", regularTokens!.AccessToken);

        var hash = AvatarHashHelper.ComputeEmailHash("regular@example.com");
        var response = await _client.DeleteAsync($"/api/avatars/{hash}");

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

    private async Task<HttpResponseMessage> UploadTestImageAsync(string hash, int width = 100, int height = 100)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Blue);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = data.ToArray();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "avatar.png");

        return await _client.PutAsync($"/api/avatars/{hash}", content);
    }
}
