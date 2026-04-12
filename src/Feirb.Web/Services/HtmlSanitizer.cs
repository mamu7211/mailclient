using Feirb.Shared.Mail;

namespace Feirb.Web.Services;

/// <summary>
/// Sanitizes HTML for incoming (received) mail display.
///
/// Extends the shared base (<see cref="HtmlSanitizerBase"/>) with:
///   - data: URI scheme — allowed for inline images already embedded in mail.
///   - Blocked media tags (video, audio, source, picture, link) — prevents
///     auto-loading external media content in the browser.
///   - External image URL filter — only data: URIs pass on img tags;
///     remote image URLs are stripped to prevent tracking pixels.
/// </summary>
public static class HtmlSanitizer
{
    private static readonly Ganss.Xss.HtmlSanitizer _sanitizer = CreateSanitizer();

    public static string Sanitize(string? html) =>
        string.IsNullOrWhiteSpace(html) ? string.Empty : _sanitizer.Sanitize(html);

    private static Ganss.Xss.HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = HtmlSanitizerBase.CreateBaseSanitizer();

        // Allow data: URIs for inline images already embedded in the mail body.
        sanitizer.AllowedSchemes.Add("data");

        // Block tags that auto-load external media content in the browser.
        sanitizer.AllowedTags.Remove("video");
        sanitizer.AllowedTags.Remove("audio");
        sanitizer.AllowedTags.Remove("source");
        sanitizer.AllowedTags.Remove("picture");
        sanitizer.AllowedTags.Remove("link");

        // Block external image sources — only allow data: URIs on img tags
        // to prevent tracking pixels and remote content loading.
        sanitizer.FilterUrl += (_, e) =>
        {
            if (e.OriginalUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return;

            // Block external URLs on img tags
            if (string.Equals(e.Tag.LocalName, "img", StringComparison.OrdinalIgnoreCase))
                e.SanitizedUrl = null;
        };

        return sanitizer;
    }
}
