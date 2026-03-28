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

    public void SetActions(IEnumerable<ToolbarAction> actions)
    {
        _actions.Clear();
        _actions.AddRange(actions);
        OnChange?.Invoke();
    }

    public void Clear()
    {
        _actions.Clear();
        OnChange?.Invoke();
    }
}
