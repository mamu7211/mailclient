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
        result.Should().Contain("Hello");
        result.Should().Contain("World");
    }

    [Fact]
    public void Sanitize_IframeTags_Removed()
    {
        var input = "<p>Content</p><iframe src=\"http://evil.com\"></iframe>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<iframe");
        result.Should().Contain("Content");
    }

    [Fact]
    public void Sanitize_ObjectEmbedTags_Removed()
    {
        var input = "<object data=\"flash.swf\"><param name=\"x\"></object><embed src=\"flash.swf\">";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<object");
        result.Should().NotContain("<embed");
    }

    [Fact]
    public void Sanitize_FormTags_Removed()
    {
        var input = "<form action=\"http://evil.com\"><input type=\"text\"></form>";
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
        result.Should().Contain("Text");
    }

    [Fact]
    public void Sanitize_ExternalImageSrc_Removed()
    {
        var input = "<img src=\"http://tracking.com/pixel.gif\" alt=\"test\">";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("http://tracking.com");
    }

    [Fact]
    public void Sanitize_DataUriImage_Preserved()
    {
        var input = "<img src=\"data:image/png;base64,abc123\" alt=\"inline\">";
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
        result.Should().Contain("https://example.com");
    }

    [Fact]
    public void Sanitize_MailtoHref_Preserved()
    {
        var input = "<a href=\"mailto:test@example.com\">Email</a>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().Contain("mailto:test@example.com");
    }

    [Fact]
    public void Sanitize_BasicFormatting_Preserved()
    {
        var input = "<p><strong>Bold</strong> and <em>italic</em></p>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().Contain("<strong>Bold</strong>");
        result.Should().Contain("<em>italic</em>");
    }

    [Fact]
    public void Sanitize_StyleTag_Removed()
    {
        var input = "<style>@import url('http://evil.com/track.css');</style><p>Content</p>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<style");
        result.Should().NotContain("evil.com");
    }

    [Fact]
    public void Sanitize_MetaRefresh_Removed()
    {
        var input = "<meta http-equiv=\"refresh\" content=\"0;url=http://evil.com\"><p>Content</p>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<meta");
        result.Should().NotContain("evil.com");
    }

    [Fact]
    public void Sanitize_SvgOnload_Removed()
    {
        var input = "<svg onload=\"alert('xss')\"><circle r=\"10\"></circle></svg>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("onload");
    }

    [Fact]
    public void Sanitize_BaseTag_Removed()
    {
        var input = "<base href=\"http://evil.com\"><a href=\"/page\">Link</a>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<base");
        result.Should().NotContain("evil.com");
    }

    [Fact]
    public void Sanitize_VbscriptHref_Removed()
    {
        var input = "<a href=\"vbscript:MsgBox('xss')\">Click</a>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("vbscript:");
    }

    [Fact]
    public void Sanitize_DataHref_Removed()
    {
        var input = "<a href=\"data:text/html,<script>alert(1)</script>\">Click</a>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<script");
        result.Should().NotContain("data:text/html");
    }

    [Fact]
    public void Sanitize_VideoTag_Removed()
    {
        var input = "<video src=\"http://example.com/video.mp4\"><source src=\"video.mp4\"></video>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<video");
        result.Should().NotContain("<source");
    }

    [Fact]
    public void Sanitize_AudioTag_Removed()
    {
        var input = "<audio src=\"http://example.com/audio.mp3\"></audio>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<audio");
    }

    [Fact]
    public void Sanitize_PictureTag_Removed()
    {
        var input = "<picture><source srcset=\"image.webp\"><img src=\"data:image/png;base64,abc\"></picture>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<picture");
        result.Should().NotContain("<source");
    }

    [Fact]
    public void Sanitize_LinkTag_Removed()
    {
        var input = "<link rel=\"stylesheet\" href=\"http://evil.com/track.css\"><p>Content</p>";
        var result = HtmlSanitizer.Sanitize(input);
        result.Should().NotContain("<link");
        result.Should().NotContain("evil.com");
    }
}
