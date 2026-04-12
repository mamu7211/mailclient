namespace Feirb.Web.Services;

public sealed class UnsavedChangesService
{
    private readonly List<ITrackedForm> _forms = [];

    public bool HasUnsavedChanges => _forms.Exists(f => f.HasUnsavedChanges);

    public event Action? OnChange;

    public void Register(ITrackedForm form) => _forms.Add(form);

    public void Unregister(ITrackedForm form)
    {
        _forms.Remove(form);
        OnChange?.Invoke();
    }

    public async Task<bool> SaveAllAsync()
    {
        foreach (var form in _forms.ToList())
        {
            if (!await form.SubmitAsync())
                return false;
        }

        return true;
    }

    public void DiscardAll()
    {
        foreach (var form in _forms)
            form.ResetDirtyState();

        OnChange?.Invoke();
    }

    public void NotifyChanged() => OnChange?.Invoke();
}
