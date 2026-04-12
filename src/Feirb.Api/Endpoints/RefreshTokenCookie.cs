namespace Feirb.Api.Endpoints;

internal static class RefreshTokenCookie
{
    internal const string Name = "refreshToken";

    internal static void Set(HttpContext httpContext, string refreshToken, int expiryDays)
    {
        httpContext.Response.Cookies.Append(Name, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = DateTime.UtcNow.AddDays(expiryDays),
        });
    }

    internal static void Clear(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
        });
    }
}
