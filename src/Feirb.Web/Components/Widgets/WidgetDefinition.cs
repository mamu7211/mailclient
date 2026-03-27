namespace Feirb.Web.Components.Widgets;

public sealed record WidgetDefinition(
    string Id,
    string NameKey,
    string DescriptionKey,
    Type ComponentType,
    string Icon = "bi-puzzle",
    int DefaultWidth = 4,
    int DefaultHeight = 2,
    string? DefaultConfig = null);
