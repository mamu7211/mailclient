using Microsoft.AspNetCore.Components;

namespace Feirb.Web.Components.Widgets;

public abstract class WidgetBase : ComponentBase
{
    [Parameter, EditorRequired]
    public string InstanceId { get; set; } = "";

    [Parameter]
    public bool EditMode { get; set; }

    [Parameter]
    public EventCallback OnRemove { get; set; }

    protected WidgetState State { get; set; } = WidgetState.Loading;

    protected string? ErrorMessage { get; set; }

    protected abstract Task LoadDataAsync();

    protected async Task ExecuteLoadAsync()
    {
        State = WidgetState.Loading;
        ErrorMessage = null;

        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            State = WidgetState.Error;
        }
    }

    protected async Task RetryAsync()
    {
        await ExecuteLoadAsync();
        StateHasChanged();
    }
}
