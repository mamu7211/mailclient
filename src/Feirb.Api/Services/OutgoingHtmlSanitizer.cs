using Feirb.Shared.Mail;

namespace Feirb.Api.Services;

/// <summary>
/// Sanitizes HTML for outgoing (composed) mail.
///
/// Extends the shared base (<see cref="HtmlSanitizerBase"/>) with:
///   - data: URI scheme — allowed so users can embed inline images
///     (e.g., pasted screenshots) in composed messages.
/// </summary>
public static class OutgoingHtmlSanitizer
{
    private static readonly Ganss.Xss.HtmlSanitizer _sanitizer = CreateSanitizer();

    public static string Sanitize(string html) =>
        string.IsNullOrWhiteSpace(html) ? string.Empty : _sanitizer.Sanitize(html);

    private static Ganss.Xss.HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = HtmlSanitizerBase.CreateBaseSanitizer();

        // Outgoing mail may contain user-pasted inline images as data: URIs.
        sanitizer.AllowedSchemes.Add("data");

        return sanitizer;
    }
}
