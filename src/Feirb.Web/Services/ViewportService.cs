using Microsoft.JSInterop;

namespace Feirb.Web.Services;

public sealed class ViewportService(IJSRuntime js) : IAsyncDisposable
{
    private DotNetObjectReference<ViewportService>? _dotnetRef;
    private bool _initialized;

    /// <summary>Current viewport width in pixels.</summary>
    public int Width { get; private set; }

    /// <summary>Fires when the viewport width changes.</summary>
    public event Action? OnResize;

    /// <summary>Initialize the service and start listening to resize events.</summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        Width = await js.InvokeAsync<int>("blazorViewport.getWidth");
        _dotnetRef = DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("blazorViewport.addResizeListener", _dotnetRef);
    }

    [JSInvokable]
    public void OnViewportResized(int width)
    {
        Width = width;
        OnResize?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_initialized)
        {
            try { await js.InvokeVoidAsync("blazorViewport.removeResizeListener"); }
            catch (JSDisconnectedException) { }
        }
        _dotnetRef?.Dispose();
    }
}
