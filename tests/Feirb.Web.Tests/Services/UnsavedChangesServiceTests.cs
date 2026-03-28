using Feirb.Web.Services;
using FluentAssertions;

namespace Feirb.Web.Tests.Services;

public class UnsavedChangesServiceTests
{
    private readonly UnsavedChangesService _sut = new();

    [Fact]
    public void SetUnsavedChanges_WithTrue_SetsHasUnsavedChanges()
    {
        _sut.SetUnsavedChanges(true);

        _sut.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public void SetUnsavedChanges_WithFalse_ClearsHasUnsavedChanges()
    {
        _sut.SetUnsavedChanges(true);

        _sut.SetUnsavedChanges(false);

        _sut.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void SetUnsavedChanges_StoresCallbacks()
    {
        Func<Task<bool>> save = () => Task.FromResult(true);
        Func<Task> discard = () => Task.CompletedTask;

        _sut.SetUnsavedChanges(true, save, discard);

        _sut.SaveAsync.Should().BeSameAs(save);
        _sut.DiscardAsync.Should().BeSameAs(discard);
    }

    [Fact]
    public void SetUnsavedChanges_RaisesOnChange()
    {
        var changed = false;
        _sut.OnChange += () => changed = true;

        _sut.SetUnsavedChanges(true);

        changed.Should().BeTrue();
    }

    [Fact]
    public void Clear_ResetsAllState()
    {
        _sut.SetUnsavedChanges(true, () => Task.FromResult(true), () => Task.CompletedTask);

        _sut.Clear();

        _sut.HasUnsavedChanges.Should().BeFalse();
        _sut.SaveAsync.Should().BeNull();
        _sut.DiscardAsync.Should().BeNull();
    }

    [Fact]
    public void Clear_RaisesOnChange()
    {
        _sut.SetUnsavedChanges(true);
        var changed = false;
        _sut.OnChange += () => changed = true;

        _sut.Clear();

        changed.Should().BeTrue();
    }
}
