using Feirb.Web.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace Feirb.Web.Extensions;

public static class EditContextExtensions
{
    public static IDisposable EnableUnsavedChangesTracking(
        this EditContext editContext,
        UnsavedChangesService service,
        Func<Task<bool>>? saveAsync = null,
        Func<Task>? discardAsync = null)
    {
        return new UnsavedChangesTracker(editContext, service, saveAsync, discardAsync);
    }

    private sealed class UnsavedChangesTracker : IDisposable
    {
        private readonly EditContext _editContext;
        private readonly UnsavedChangesService _service;
        private readonly Func<Task<bool>>? _saveAsync;
        private readonly Func<Task>? _discardAsync;

        public UnsavedChangesTracker(
            EditContext editContext,
            UnsavedChangesService service,
            Func<Task<bool>>? saveAsync,
            Func<Task>? discardAsync)
        {
            _editContext = editContext;
            _service = service;
            _saveAsync = saveAsync;
            _discardAsync = discardAsync;

            _editContext.OnFieldChanged += OnFieldChanged;
        }

        private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
        {
            if (!_service.HasUnsavedChanges)
            {
                _service.SetUnsavedChanges(true, _saveAsync, _discardAsync);
            }
        }

        public void Dispose()
        {
            _editContext.OnFieldChanged -= OnFieldChanged;
            _service.Clear();
        }
    }
}
