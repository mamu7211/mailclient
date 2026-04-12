using Feirb.Shared.Mail;

namespace Feirb.Api.Services;

/// <summary>
/// Sanitizes HTML for outgoing (composed) mail.
///
/// Extends the shared base (<see cref="HtmlSanitizerBase"/>) with:
///   - data: URI scheme — enabled at scheme level but restricted via FilterUrl
///     to img tags only (for user-pasted screenshots); data: URIs on other
///     elements (e.g., a href) are stripped to prevent phishing.
/// </summary>
public static class OutgoingHtmlSanitizer
{
    private static readonly Ganss.Xss.HtmlSanitizer _sanitizer = CreateSanitizer();

    public static string Sanitize(string html) =>
        string.IsNullOrWhiteSpace(html) ? string.Empty : _sanitizer.Sanitize(html);

    private static Ganss.Xss.HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = HtmlSanitizerBase.CreateBaseSanitizer();

        // Allow data: scheme so the FilterUrl callback can selectively permit
        // data: URIs on img tags while stripping them everywhere else.
        sanitizer.AllowedSchemes.Add("data");

        // Allow data: URIs only on img tags for user-pasted inline images;
        // strip data: URIs on all other elements to prevent phishing vectors.
        sanitizer.FilterUrl += (_, e) =>
        {
            if (!e.OriginalUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return;

            var isImgTag = string.Equals(e.Tag.LocalName, "img", StringComparison.OrdinalIgnoreCase);

            if (!isImgTag)
                e.SanitizedUrl = null;
        };

        return sanitizer;
    }
}
