using System.Text.RegularExpressions;

namespace Feirb.Web.Services;

public static partial class HtmlSanitizer
{
    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var result = html;

        // Remove dangerous tags with their content
        result = ScriptTagRegex().Replace(result, string.Empty);
        result = IframeTagRegex().Replace(result, string.Empty);
        result = ObjectTagRegex().Replace(result, string.Empty);
        result = EmbedTagRegex().Replace(result, string.Empty);
        result = FormTagRegex().Replace(result, string.Empty);
        result = InputTagRegex().Replace(result, string.Empty);

        // Remove on* event handler attributes
        result = EventHandlerRegex().Replace(result, string.Empty);

        // Remove external src attributes (keep data: URIs)
        result = ExternalSrcRegex().Replace(result, "$1$2");

        // Remove external CSS url() references (keep data: URIs)
        result = ExternalCssUrlRegex().Replace(result, "url()");

        // Sanitize <a> href attributes — only allow http:, https:, mailto:
        result = DangerousHrefRegex().Replace(result, "$1$2");

        return result;
    }

    [GeneratedRegex(@"<script\b[^>]*>[\s\S]*?</script\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"<iframe\b[^>]*>[\s\S]*?</iframe\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex IframeTagRegex();

    [GeneratedRegex(@"<object\b[^>]*>[\s\S]*?</object\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex ObjectTagRegex();

    [GeneratedRegex(@"<embed\b[^>]*?/?\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex EmbedTagRegex();

    [GeneratedRegex(@"<form\b[^>]*>[\s\S]*?</form\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex FormTagRegex();

    [GeneratedRegex(@"<input\b[^>]*?/?\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex InputTagRegex();

    [GeneratedRegex(@"\s+on\w+\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]*)", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerRegex();

    // Match src="..." where value does NOT start with data:
    [GeneratedRegex(@"(<[^>]*?)\s+src\s*=\s*(?:""(?!data:)[^""]*""|'(?!data:)[^']*'|(?!data:)[^\s>]*)", RegexOptions.IgnoreCase)]
    private static partial Regex ExternalSrcRegex();

    [GeneratedRegex(@"url\(\s*(?:""(?!data:)[^""]*""|'(?!data:)[^']*'|(?!data:)[^)]*)\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex ExternalCssUrlRegex();

    // Match href="javascript:..." or href="vbscript:..." or href="data:..."
    [GeneratedRegex(@"(<[^>]*?)\s+href\s*=\s*(?:""(?:javascript|vbscript|data):[^""]*""|'(?:javascript|vbscript|data):[^']*')", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousHrefRegex();
}
