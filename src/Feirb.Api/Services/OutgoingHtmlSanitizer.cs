using Ganss.Xss;

namespace Feirb.Api.Services;

public static class OutgoingHtmlSanitizer
{
    private static readonly HtmlSanitizer _sanitizer = CreateSanitizer();

    public static string Sanitize(string html) =>
        string.IsNullOrWhiteSpace(html) ? string.Empty : _sanitizer.Sanitize(html);

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("mailto");
        sanitizer.AllowedSchemes.Add("data");

        sanitizer.AllowedTags.Remove("script");
        sanitizer.AllowedTags.Remove("form");
        sanitizer.AllowedTags.Remove("input");
        sanitizer.AllowedTags.Remove("button");
        sanitizer.AllowedTags.Remove("select");
        sanitizer.AllowedTags.Remove("textarea");

        return sanitizer;
    }
}
