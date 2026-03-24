namespace Feirb.Web.Services;

public sealed class BreadcrumbOverrideService
{
    public string? LastSegmentLabel { get; private set; }

    public event Action? OnChange;

    public void SetLastSegmentLabel(string label)
    {
        LastSegmentLabel = label;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        LastSegmentLabel = null;
        OnChange?.Invoke();
    }
}
