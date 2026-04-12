using Feirb.Shared.Mail;

namespace Feirb.Web.Services;

/// <summary>
/// Sanitizes HTML for incoming (received) mail display.
///
/// Extends the shared base (<see cref="HtmlSanitizerBase"/>) with:
///   - data: URI scheme — enabled at scheme level but restricted via FilterUrl
///     to img tags only; data: URIs on other elements (e.g., a href) are
///     stripped to prevent phishing.
///   - Blocked media tags (video, audio, source, picture, link) — prevents
///     auto-loading external media content in the browser.
///   - External image URL filter — remote image URLs are stripped to prevent
///     tracking pixels.
/// </summary>
public static class HtmlSanitizer
{
    private static readonly Ganss.Xss.HtmlSanitizer _sanitizer = CreateSanitizer();

    public static string Sanitize(string? html) =>
        string.IsNullOrWhiteSpace(html) ? string.Empty : _sanitizer.Sanitize(html);

    private static Ganss.Xss.HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = HtmlSanitizerBase.CreateBaseSanitizer();

        // Allow data: scheme so the FilterUrl callback can selectively permit
        // data: URIs on img tags while stripping them everywhere else.
        sanitizer.AllowedSchemes.Add("data");

        // Block tags that auto-load external media content in the browser.
        sanitizer.AllowedTags.Remove("video");
        sanitizer.AllowedTags.Remove("audio");
        sanitizer.AllowedTags.Remove("source");
        sanitizer.AllowedTags.Remove("picture");
        sanitizer.AllowedTags.Remove("link");

        // Filter URLs: allow data: URIs only on img src (inline images),
        // block all external URLs on img tags (tracking pixel protection),
        // and strip data: URIs on all other elements (phishing prevention).
        sanitizer.FilterUrl += (_, e) =>
        {
            var isDataUri = e.OriginalUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
            var isImgTag = string.Equals(e.Tag.LocalName, "img", StringComparison.OrdinalIgnoreCase);

            if (isDataUri)
            {
                // Allow data: URIs only on img tags (inline images);
                // strip on all other elements (e.g., a href) to prevent phishing.
                if (!isImgTag)
                    e.SanitizedUrl = null;

                return;
            }

            // Block external URLs on img tags (tracking pixel protection)
            if (string.Equals(e.Tag.LocalName, "img", StringComparison.OrdinalIgnoreCase))
                e.SanitizedUrl = null;
        };

        return sanitizer;
    }
}
