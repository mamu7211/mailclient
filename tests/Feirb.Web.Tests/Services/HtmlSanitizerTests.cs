using Feirb.Web.Services;
using FluentAssertions;

namespace Feirb.Web.Tests.Services;

public class HtmlSanitizerTests
{
    [Fact]
    public void Sanitize_NullInput_ReturnsEmpty() =>
        HtmlSanitizer.Sanitize(null).Should().BeEmpty();

    [Fact]
    public void Sanitize_EmptyInput_ReturnsEmpty() =>
        HtmlSanitizer.Sanitize("").Should().BeEmpty();

    [Fact]
    public void Sanitize_WhitespaceInput_ReturnsEmpty() =>
        HtmlSanitizer.Sanitize("   ").Should().BeEmpty();

    [Fact]
    public void Sanitize_ScriptTags_Removed()
    {
        var input = "<p>Hello</p><script>alert('xss')</script><p>World</p>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<script");
        result.Should().Contain("<p>Hello</p>");
        result.Should().Contain("<p>World</p>");
    }

    [Fact]
    public void Sanitize_IframeTags_Removed()
    {
        var input = "<p>Content</p><iframe src=\"http://evil.com\"></iframe>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<iframe");
        result.Should().Contain("<p>Content</p>");
    }

    [Fact]
    public void Sanitize_ObjectEmbedTags_Removed()
    {
        var input = "<object data=\"flash.swf\"><param name=\"x\"/></object><embed src=\"flash.swf\"/>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<object");
        result.Should().NotContain("<embed");
    }

    [Fact]
    public void Sanitize_FormTags_Removed()
    {
        var input = "<form action=\"http://evil.com\"><input type=\"text\"/></form>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<form");
        result.Should().NotContain("<input");
    }

    [Fact]
    public void Sanitize_EventHandlers_Removed()
    {
        var input = "<p onclick=\"alert('xss')\" onmouseover=\"steal()\">Text</p>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("onclick");
        result.Should().NotContain("onmouseover");
        result.Should().Contain(">Text</p>");
    }

    [Fact]
    public void Sanitize_ExternalImageSrc_Removed()
    {
        var input = "<img src=\"http://tracking.com/pixel.gif\" alt=\"test\"/>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("http://tracking.com");
    }

    [Fact]
    public void Sanitize_DataUriImage_Preserved()
    {
        var input = "<img src=\"data:image/png;base64,abc123\" alt=\"inline\"/>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().Contain("data:image/png;base64,abc123");
    }

    [Fact]
    public void Sanitize_JavascriptHref_Removed()
    {
        var input = "<a href=\"javascript:alert('xss')\">Click</a>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("javascript:");
    }

    [Fact]
    public void Sanitize_SafeHref_Preserved()
    {
        var input = "<a href=\"https://example.com\">Link</a>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().Contain("href=\"https://example.com\"");
    }

    [Fact]
    public void Sanitize_MailtoHref_Preserved()
    {
        var input = "<a href=\"mailto:test@example.com\">Email</a>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().Contain("href=\"mailto:test@example.com\"");
    }

    [Fact]
    public void Sanitize_BasicFormatting_Preserved()
    {
        var input = "<p><strong>Bold</strong> and <em>italic</em></p>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().Be(input);
    }

    [Fact]
    public void Sanitize_ExternalCssUrl_Removed()
    {
        var input = "<div style=\"background: url('http://evil.com/bg.png')\">Content</div>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("http://evil.com");
    }

    [Fact]
    public void Sanitize_DataCssUrl_Preserved()
    {
        var input = "<div style=\"background: url('data:image/png;base64,abc')\">Content</div>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().Contain("data:image/png;base64,abc");
    }
}
