using Microsoft.AspNetCore.Components;

namespace Feirb.Web.Components.UI;

/// <summary>
/// One item in a <see cref="DropdownButton"/> menu.
/// </summary>
/// <param name="Label">Display text.</param>
/// <param name="OnClick">Async callback fired when the item is selected.</param>
/// <param name="Icon">Optional Bootstrap Icon name (without <c>bi-</c> prefix).</param>
/// <param name="Disabled">When true the item is rendered greyed-out and not clickable.</param>
/// <param name="TestId">Optional <c>data-testid</c> for the menu item button.</param>
public sealed record DropdownButtonItem(
    string Label,
    EventCallback OnClick,
    string? Icon = null,
    bool Disabled = false,
    string? TestId = null);
