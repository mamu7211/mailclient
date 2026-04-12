using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Shared.Auth;
using Feirb.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Feirb.Web.Http;

public sealed class AuthDelegatingHandler(
    IJSRuntime jsRuntime,
    AuthenticationStateProvider authStateProvider,
    NavigationManager navigationManager) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await jsRuntime.InvokeAsync<string?>("blazorAuth.getAccessToken");
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
        {
            var refreshed = await TryRefreshTokenAsync(cancellationToken);
            if (refreshed)
            {
                var newToken = await jsRuntime.InvokeAsync<string?>("blazorAuth.getAccessToken");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                response = await base.SendAsync(request, cancellationToken);
            }
        }

        return response;
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        // Use InnerHandler directly to avoid recursive handler chain.
        // The refresh token cookie is sent automatically by the browser.
        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");

        using var invoker = new HttpMessageInvoker(InnerHandler!, disposeHandler: false);
        var response = await invoker.SendAsync(refreshRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await ForceLogoutAsync();
            return false;
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (tokens is null)
        {
            await ForceLogoutAsync();
            return false;
        }

        await jsRuntime.InvokeVoidAsync("blazorAuth.setAccessToken", tokens.AccessToken);
        return true;
    }

    private async Task ForceLogoutAsync()
    {
        if (authStateProvider is JwtAuthenticationStateProvider jwtProvider)
            await jwtProvider.LogoutAsync();

        navigationManager.NavigateTo("/login", forceLoad: true);
    }
}
