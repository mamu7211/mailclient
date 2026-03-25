namespace Feirb.Web.Components.Widgets;

public static class WidgetRegistry
{
    public static IReadOnlyList<WidgetDefinition> All { get; } =
    [
        new WidgetDefinition("mail-count", "WidgetMailCountName", "WidgetMailCountDescription", typeof(TotalMailCountWidget), DefaultWidth: 3, DefaultHeight: 2),
        new WidgetDefinition("mails-per-day", "WidgetMailsPerDayName", "WidgetMailsPerDayDescription", typeof(MailsPerDayWidget), DefaultWidth: 6, DefaultHeight: 3),
    ];

    public static WidgetDefinition? GetById(string id) =>
        All.FirstOrDefault(w => w.Id == id);
}
