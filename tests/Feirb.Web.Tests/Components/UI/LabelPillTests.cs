using Feirb.Web.Components.UI;
using FluentAssertions;

namespace Feirb.Web.Tests.Components.UI;

public class LabelPillTests
{
    [Theory]
    [InlineData("#ffffff", "#000000")] // White background → black text
    [InlineData("#FFFFFF", "#000000")] // Case insensitive
    [InlineData("#000000", "#ffffff")] // Black background → white text
    [InlineData("#b6004f", "#ffffff")] // Primary brand color → white text
    [InlineData("#FFE04A", "#000000")] // Warning yellow → black text
    [InlineData("#1D76DB", "#000000")] // Feature blue → black text (luminance ~0.18)
    [InlineData("#0E8A16", "#000000")] // Green → black text (luminance ~0.18)
    public void GetContrastColor_ReturnsCorrectTextColor(string background, string expected)
    {
        var result = LabelPill.GetContrastColor(background);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("#ff0000", "#000000")] // Pure red → black (luminance 0.2126 > 0.179)
    [InlineData("#00ff00", "#000000")] // Pure green → black (green has high luminance weight)
    [InlineData("#0000ff", "#ffffff")] // Pure blue → white
    public void GetContrastColor_PrimaryColors_ReturnsCorrectContrast(string background, string expected)
    {
        var result = LabelPill.GetContrastColor(background);

        result.Should().Be(expected);
    }

    [Fact]
    public void GetContrastColor_WithoutHashPrefix_ReturnsCorrectResult()
    {
        var result = LabelPill.GetContrastColor("ffffff");

        result.Should().Be("#000000");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("#xyz123")]
    public void GetContrastColor_InvalidHex_ReturnsWhite(string invalid)
    {
        var result = LabelPill.GetContrastColor(invalid);

        result.Should().Be("#ffffff");
    }
}
