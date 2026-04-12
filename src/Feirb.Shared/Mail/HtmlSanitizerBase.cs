using Ganss.Xss;

namespace Feirb.Shared.Mail;

/// <summary>
/// Shared base configuration for HTML sanitizers used across the application.
/// Both incoming (Web) and outgoing (API) sanitizers inherit this baseline,
/// then apply context-specific rules on top.
///
/// Base policy:
///   - Allowed URI schemes: https, http, mailto
///   - Blocked tags: script, form, input, button, select, textarea
///
/// Context-specific extensions are documented in each consumer.
/// </summary>
public static class HtmlSanitizerBase
{
    /// <summary>
    /// URI schemes considered safe for all contexts.
    /// Consumers may add additional schemes (e.g., data: for inline images).
    /// </summary>
    private static readonly string[] _baseAllowedSchemes = ["https", "http", "mailto"];

    /// <summary>
    /// Tags blocked in all contexts: scripting and form interaction elements.
    /// Consumers may block additional tags (e.g., media tags for incoming mail).
    /// </summary>
    private static readonly string[] _baseBlockedTags =
        ["script", "form", "input", "button", "select", "textarea"];

    /// <summary>
    /// Creates a sanitizer with the shared base configuration applied.
    /// Callers can further customize the returned instance before use.
    /// </summary>
    public static HtmlSanitizer CreateBaseSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in _baseAllowedSchemes)
        {
            sanitizer.AllowedSchemes.Add(scheme);
        }

        foreach (var tag in _baseBlockedTags)
        {
            sanitizer.AllowedTags.Remove(tag);
        }

        return sanitizer;
    }
}
