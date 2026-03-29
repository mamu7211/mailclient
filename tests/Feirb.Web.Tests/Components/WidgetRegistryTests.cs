using Feirb.Web.Components.Widgets;
using FluentAssertions;

namespace Feirb.Web.Tests.Components;

public class WidgetRegistryTests
{
    [Fact]
    public void All_ContainsThreeWidgets()
    {
        WidgetRegistry.All.Should().HaveCount(3);
    }

    [Fact]
    public void All_ContainsMailCountWidget()
    {
        WidgetRegistry.All.Should().Contain(w => w.Id == "mail-count");
    }

    [Fact]
    public void All_ContainsMailsPerDayWidget()
    {
        WidgetRegistry.All.Should().Contain(w => w.Id == "mails-per-day");
    }

    [Fact]
    public void GetById_ExistingId_ReturnsWidget()
    {
        var widget = WidgetRegistry.GetById("mail-count");

        widget.Should().NotBeNull();
        widget!.Id.Should().Be("mail-count");
        widget.NameKey.Should().Be("WidgetMailCountName");
        widget.DescriptionKey.Should().Be("WidgetMailCountDescription");
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var widget = WidgetRegistry.GetById("unknown-widget");

        widget.Should().BeNull();
    }

    [Fact]
    public void All_WidgetsHaveUniqueIds()
    {
        WidgetRegistry.All.Select(w => w.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_WidgetsHavePositiveDefaultDimensions()
    {
        WidgetRegistry.All.Should().OnlyContain(w => w.DefaultWidth > 0 && w.DefaultHeight > 0);
    }

    [Theory]
    [InlineData("mail-count", typeof(TotalMailCountWidget))]
    [InlineData("mails-per-day", typeof(MailsPerDayWidget))]
    public void GetById_ReturnsCorrectComponentType(string id, Type expectedType)
    {
        var widget = WidgetRegistry.GetById(id);

        widget.Should().NotBeNull();
        widget!.ComponentType.Should().Be(expectedType);
    }

    [Fact]
    public void MailsPerDayWidget_HasDefaultConfig()
    {
        var widget = WidgetRegistry.GetById("mails-per-day");

        widget.Should().NotBeNull();
        widget!.DefaultConfig.Should().Be("7");
    }

    [Fact]
    public void MailCountWidget_HasNoDefaultConfig()
    {
        var widget = WidgetRegistry.GetById("mail-count");

        widget.Should().NotBeNull();
        widget!.DefaultConfig.Should().BeNull();
    }
}
