namespace Feirb.Web.Components.UI;

/// <summary>A single item in a sidebar menu.</summary>
/// <param name="Icon">Bootstrap Icon name without the "bi-" prefix.</param>
/// <param name="Label">Display text.</param>
/// <param name="Href">Navigation target. Ignored when <paramref name="OnClick"/> is set.</param>
/// <param name="OnClick">Action callback. When set, the item renders as a button instead of a link.</param>
/// <param name="Visible">Controls whether the item is rendered. Defaults to always visible.</param>
/// <param name="Match">NavLink match mode. Defaults to <c>null</c> (auto: exact for "/" or empty, prefix otherwise).</param>
public record SidebarMenuItem(
    string Icon,
    string Label,
    string? Href = null,
    Func<Task>? OnClick = null,
    Func<bool>? Visible = null,
    Microsoft.AspNetCore.Components.Routing.NavLinkMatch? Match = null);

/// <summary>A group of menu items with an optional section header.</summary>
/// <param name="Items">Menu items in this section.</param>
/// <param name="Label">Optional section header text. Omit for standalone items.</param>
public record SidebarMenuSection(
    IReadOnlyList<SidebarMenuItem> Items,
    string? Label = null);
