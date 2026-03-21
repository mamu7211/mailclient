using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Feirb.Shared.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Feirb.Web.Services;

public sealed class JwtAuthenticationStateProvider(IJSRuntime jsRuntime) : AuthenticationStateProvider
{
    private static readonly AuthenticationState _anonymousState =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await jsRuntime.InvokeAsync<string?>("blazorAuth.getAccessToken");
        if (string.IsNullOrEmpty(token))
            return _anonymousState;

        var claims = ParseClaimsFromJwt(token);
        if (claims is null)
            return _anonymousState;

        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task LoginAsync(TokenResponse tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        await jsRuntime.InvokeVoidAsync("blazorAuth.setTokens", tokens.AccessToken, tokens.RefreshToken);
        var claims = ParseClaimsFromJwt(tokens.AccessToken);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task LogoutAsync()
    {
        await jsRuntime.InvokeVoidAsync("blazorAuth.clearTokens");
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymousState));
    }

    private static IEnumerable<Claim>? ParseClaimsFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo < DateTime.UtcNow)
                return null;

            return jwt.Claims;
        }
        catch
        {
            return null;
        }
    }
}
