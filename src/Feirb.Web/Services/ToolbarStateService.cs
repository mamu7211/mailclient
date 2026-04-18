using Feirb.Web.Components.UI;

namespace Feirb.Web.Services;

public sealed class ToolbarAction(string label, ButtonVariant variant, Func<Task> onClickAsync, string? icon = null)
{
    public string Label { get; } = label;
    public ButtonVariant Variant { get; } = variant;
    public Func<Task> OnClickAsync { get; } = onClickAsync;
    public string? Icon { get; } = icon;
}

/// <summary>
/// A dropdown-style toolbar entry that opens a small action menu when clicked.
/// Items are <see cref="DropdownButtonItem"/> instances. Use this when a single
/// toolbar action has two or three closely-related variants (e.g. "Preview" /
/// "Apply Now") and you don't want to spend extra space on multiple buttons.
/// </summary>
public sealed class ToolbarDropdownAction(
    string label,
    ButtonVariant variant,
    IReadOnlyList<DropdownButtonItem> items,
    string? icon = null,
    string? testId = null,
    string? menuTestId = null)
{
    public string Label { get; } = label;
    public ButtonVariant Variant { get; } = variant;
    public IReadOnlyList<DropdownButtonItem> Items { get; } = items;
    public string? Icon { get; } = icon;
    public string? TestId { get; } = testId;
    public string? MenuTestId { get; } = menuTestId;
}

public sealed class ToolbarStateService
{
    private readonly List<ToolbarAction> _actions = [];
    private readonly List<ToolbarDropdownAction> _dropdownActions = [];

    public IReadOnlyList<ToolbarAction> Actions => _actions;
    public IReadOnlyList<ToolbarDropdownAction> DropdownActions => _dropdownActions;

    public event Action? OnChange;

    public void AddActions(IEnumerable<ToolbarAction> actions)
    {
        _actions.AddRange(actions);
        OnChange?.Invoke();
    }

    public void AddDropdownActions(IEnumerable<ToolbarDropdownAction> actions)
    {
        _dropdownActions.AddRange(actions);
        OnChange?.Invoke();
    }

    public void RemoveActions(IEnumerable<ToolbarAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        var changed = false;
        foreach (var action in actions)
        {
            changed |= _actions.Remove(action);
        }

        if (changed)
        {
            OnChange?.Invoke();
        }
    }

    public void RemoveDropdownActions(IEnumerable<ToolbarDropdownAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        var changed = false;
        foreach (var action in actions)
        {
            changed |= _dropdownActions.Remove(action);
        }

        if (changed)
        {
            OnChange?.Invoke();
        }
    }

    public void Clear()
    {
        if (_actions.Count == 0 && _dropdownActions.Count == 0)
            return;

        _actions.Clear();
        _dropdownActions.Clear();
        OnChange?.Invoke();
    }
}
