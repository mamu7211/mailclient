namespace Feirb.Web.Components.Widgets;

public static class WidgetRegistry
{
    public static IReadOnlyList<WidgetDefinition> All { get; } =
    [
        new WidgetDefinition("mail-count", "WidgetMailCountName", "WidgetMailCountDescription", typeof(TotalMailCountWidget), Icon: "bi-envelope", DefaultWidth: 3, DefaultHeight: 2),
        new WidgetDefinition("mails-per-day", "WidgetMailsPerDayName", "WidgetMailsPerDayDescription", typeof(MailsPerDayWidget), Icon: "bi-bar-chart", DefaultWidth: 6, DefaultHeight: 3, DefaultConfig: "7"),
    ];

    public static WidgetDefinition? GetById(string id) =>
        All.FirstOrDefault(w => w.Id == id);
}
