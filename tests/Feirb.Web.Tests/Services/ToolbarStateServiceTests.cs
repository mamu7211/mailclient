using Feirb.Web.Components.UI;
using Feirb.Web.Services;
using FluentAssertions;

namespace Feirb.Web.Tests.Services;

public class ToolbarStateServiceTests
{
    private readonly ToolbarStateService _sut = new();

    private static ToolbarAction CreateAction(string label = "Test") =>
        new(label, ButtonVariant.Primary, () => Task.CompletedTask);

    [Fact]
    public void AddActions_AddsToList()
    {
        var actions = new[] { CreateAction("First"), CreateAction("Second") };

        _sut.AddActions(actions);

        _sut.Actions.Should().HaveCount(2);
        _sut.Actions[0].Label.Should().Be("First");
        _sut.Actions[1].Label.Should().Be("Second");
    }

    [Fact]
    public void AddActions_AppendsToExisting()
    {
        _sut.AddActions([CreateAction("First")]);

        _sut.AddActions([CreateAction("Second")]);

        _sut.Actions.Should().HaveCount(2);
        _sut.Actions[0].Label.Should().Be("First");
        _sut.Actions[1].Label.Should().Be("Second");
    }

    [Fact]
    public void AddActions_RaisesOnChange()
    {
        var changed = false;
        _sut.OnChange += () => changed = true;

        _sut.AddActions([CreateAction()]);

        changed.Should().BeTrue();
    }

    [Fact]
    public void RemoveActions_RemovesOwnActions()
    {
        var actions = new[] { CreateAction("Mine") };
        _sut.AddActions(actions);

        _sut.RemoveActions(actions);

        _sut.Actions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveActions_DoesNotRemoveOtherActions()
    {
        var pageA = new[] { CreateAction("A") };
        var pageB = new[] { CreateAction("B") };
        _sut.AddActions(pageA);
        _sut.AddActions(pageB);

        _sut.RemoveActions(pageA);

        _sut.Actions.Should().ContainSingle()
            .Which.Label.Should().Be("B");
    }

    [Fact]
    public void RemoveActions_AlreadyReplaced_IsNoOp()
    {
        var pageA = new[] { CreateAction("A") };
        var pageB = new[] { CreateAction("B") };
        _sut.AddActions(pageA);
        _sut.Clear();
        _sut.AddActions(pageB);

        _sut.RemoveActions(pageA);

        _sut.Actions.Should().ContainSingle()
            .Which.Label.Should().Be("B");
    }

    [Fact]
    public void RemoveActions_AlreadyGone_DoesNotRaiseOnChange()
    {
        var actions = new[] { CreateAction() };
        _sut.AddActions(actions);
        _sut.Clear();
        var changed = false;
        _sut.OnChange += () => changed = true;

        _sut.RemoveActions(actions);

        changed.Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllActions()
    {
        _sut.AddActions([CreateAction(), CreateAction()]);

        _sut.Clear();

        _sut.Actions.Should().BeEmpty();
    }

    [Fact]
    public void Clear_WhenEmpty_DoesNotRaiseOnChange()
    {
        var changed = false;
        _sut.OnChange += () => changed = true;

        _sut.Clear();

        changed.Should().BeFalse();
    }
}
