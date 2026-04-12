using Feirb.Api.Services;
using FluentAssertions;

namespace Feirb.Api.Tests.Services;

public class OutgoingHtmlSanitizerTests
{
    [Fact]
    public void Sanitize_ScriptTags_Stripped()
    {
        var input = "<p>Hello</p><script>alert('xss')</script><p>World</p>";
        var result = OutgoingHtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<script");
        result.Should().Contain("Hello");
        result.Should().Contain("World");
    }

    [Fact]
    public void Sanitize_FormTags_Stripped()
    {
        var input = "<form action=\"http://evil.com\"><input type=\"text\"><button>Submit</button></form>";
        var result = OutgoingHtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<form");
        result.Should().NotContain("<input");
        result.Should().NotContain("<button");
    }

    [Fact]
    public void Sanitize_SelectTextareaTags_Stripped()
    {
        var input = "<select><option>A</option></select><textarea>Text</textarea>";
        var result = OutgoingHtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<select");
        result.Should().NotContain("<textarea");
    }

    [Fact]
    public void Sanitize_DataUriInImgSrc_Preserved()
    {
        var input = "<img src=\"data:image/png;base64,abc123\" alt=\"inline\">";
        var result = OutgoingHtmlSanitizer.Sanitize(input);
        result.Should().Contain("data:image/png;base64,abc123");
    }

    [Fact]
    public void Sanitize_DataUriInAnchorHref_Stripped()
    {
        var input = "<a href=\"data:text/html,<script>alert(1)</script>\">Click</a>";
        var result = OutgoingHtmlSanitizer.Sanitize(input);
        result.Should().NotContain("data:text/html");
    }

    [Fact]
    public void Sanitize_ExternalHttpsLink_Preserved()
    {
        var input = "<a href=\"https://example.com\">Link</a>";
        var result = OutgoingHtmlSanitizer.Sanitize(input);
        result.Should().Contain("https://example.com");
    }

    [Fact]
    public void Sanitize_ExternalHttpLink_Preserved()
    {
        var input = "<a href=\"http://example.com\">Link</a>";
        var result = OutgoingHtmlSanitizer.Sanitize(input);
        result.Should().Contain("http://example.com");
    }

    [Fact]
    public void Sanitize_MailtoLink_Preserved()
    {
        var input = "<a href=\"mailto:test@example.com\">Email</a>";
        var result = OutgoingHtmlSanitizer.Sanitize(input);
        result.Should().Contain("mailto:test@example.com");
    }
}
