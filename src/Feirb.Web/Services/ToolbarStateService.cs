using Feirb.Web.Components.UI;

namespace Feirb.Web.Services;

public sealed class ToolbarAction(string label, ButtonVariant variant, Func<Task> onClickAsync, string? icon = null)
{
    public string Label { get; } = label;
    public ButtonVariant Variant { get; } = variant;
    public Func<Task> OnClickAsync { get; } = onClickAsync;
    public string? Icon { get; } = icon;
}

public sealed class ToolbarStateService
{
    private readonly List<ToolbarAction> _actions = [];

    public IReadOnlyList<ToolbarAction> Actions => _actions;

    public event Action? OnChange;

    public void AddActions(IEnumerable<ToolbarAction> actions)
    {
        _actions.AddRange(actions);
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

    public void Clear()
    {
        if (_actions.Count == 0)
            return;

        _actions.Clear();
        OnChange?.Invoke();
    }
}
