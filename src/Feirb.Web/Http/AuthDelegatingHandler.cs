using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Shared.Auth;
using Microsoft.JSInterop;

namespace Feirb.Web.Http;

public sealed class AuthDelegatingHandler(IJSRuntime jsRuntime) : DelegatingHandler
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
        var refreshToken = await jsRuntime.InvokeAsync<string?>("blazorAuth.getRefreshToken");
        if (string.IsNullOrEmpty(refreshToken))
            return false;

        // Use InnerHandler directly to avoid recursive handler chain
        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Content = JsonContent.Create(new RefreshRequest(refreshToken));

        using var invoker = new HttpMessageInvoker(InnerHandler!, disposeHandler: false);
        var response = await invoker.SendAsync(refreshRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await jsRuntime.InvokeVoidAsync("blazorAuth.clearTokens");
            return false;
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (tokens is null)
        {
            await jsRuntime.InvokeVoidAsync("blazorAuth.clearTokens");
            return false;
        }

        await jsRuntime.InvokeVoidAsync("blazorAuth.setTokens", tokens.AccessToken, tokens.RefreshToken);
        return true;
    }
}
