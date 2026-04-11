using Feirb.Web.Services;
using FluentAssertions;

namespace Feirb.Web.Tests.Services;

public class NotificationServiceTests
{
    private readonly NotificationService _sut = new();

    [Fact]
    public void Add_SingleNotification_AppearsInList()
    {
        var item = _sut.Add("Test message", NotificationSeverity.Info);

        _sut.Notifications.Should().ContainSingle()
            .Which.Should().BeSameAs(item);
    }

    [Fact]
    public void Add_SingleNotification_SetsPropertiesCorrectly()
    {
        var item = _sut.Add("Test message", NotificationSeverity.Warning);

        item.Message.Should().Be("Test message");
        item.Severity.Should().Be(NotificationSeverity.Warning);
        item.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Add_RaisesOnChange()
    {
        var changed = false;
        _sut.OnChange += () => changed = true;

        _sut.Add("Test", NotificationSeverity.Info);

        changed.Should().BeTrue();
    }

    [Fact]
    public void Dismiss_RemovesNotification()
    {
        var item = _sut.Add("Test", NotificationSeverity.Info);

        _sut.Dismiss(item.Id);

        _sut.Notifications.Should().BeEmpty();
    }

    [Fact]
    public void Dismiss_NonExistentId_DoesNotThrow()
    {
        _sut.Add("Test", NotificationSeverity.Info);

        var act = () => _sut.Dismiss(Guid.NewGuid());

        act.Should().NotThrow();
        _sut.Notifications.Should().HaveCount(1);
    }

    [Fact]
    public void Dismiss_RaisesOnChange()
    {
        var item = _sut.Add("Test", NotificationSeverity.Info);
        var changed = false;
        _sut.OnChange += () => changed = true;

        _sut.Dismiss(item.Id);

        changed.Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesAllNotifications()
    {
        _sut.Add("One", NotificationSeverity.Info);
        _sut.Add("Two", NotificationSeverity.Warning);
        _sut.Add("Three", NotificationSeverity.Error);

        _sut.Clear();

        _sut.Notifications.Should().BeEmpty();
    }

    [Fact]
    public void Clear_RaisesOnChange()
    {
        _sut.Add("Test", NotificationSeverity.Info);
        var changed = false;
        _sut.OnChange += () => changed = true;

        _sut.Clear();

        changed.Should().BeTrue();
    }

    [Fact]
    public void Add_ErrorNotification_DoesNotAutoDismiss()
    {
        _sut.Add("Error message", NotificationSeverity.Error);

        // Error notifications should remain until manually dismissed
        _sut.Notifications.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(NotificationSeverity.Info, 10)]
    [InlineData(NotificationSeverity.Success, 10)]
    [InlineData(NotificationSeverity.Warning, 30)]
    public void GetAutoDismissSeconds_ReturnsExpectedDelay(NotificationSeverity severity, int expected)
    {
        NotificationService.GetAutoDismissSeconds(severity).Should().Be(expected);
    }

    [Fact]
    public void GetAutoDismissSeconds_Error_ReturnsNull()
    {
        NotificationService.GetAutoDismissSeconds(NotificationSeverity.Error).Should().BeNull();
    }

    [Theory]
    [InlineData(NotificationSeverity.Info)]
    [InlineData(NotificationSeverity.Success)]
    [InlineData(NotificationSeverity.Warning)]
    [InlineData(NotificationSeverity.Error)]
    public void Add_AllSeverityLevels_Accepted(NotificationSeverity severity)
    {
        var item = _sut.Add("Test", severity);

        item.Severity.Should().Be(severity);
        _sut.Notifications.Should().ContainSingle();
    }

    [Fact]
    public void Notifications_PreservesInsertionOrder()
    {
        _sut.Add("First", NotificationSeverity.Error);
        _sut.Add("Second", NotificationSeverity.Error);
        _sut.Add("Third", NotificationSeverity.Error);

        _sut.Notifications.Select(n => n.Message)
            .Should().ContainInOrder("First", "Second", "Third");
    }
}
