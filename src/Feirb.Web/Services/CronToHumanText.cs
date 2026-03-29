using Microsoft.Extensions.Localization;

namespace Feirb.Web.Services;

public static class CronToHumanText
{
    private static readonly Dictionary<string, string> _presetKeys = new()
    {
        ["0 * * * * ?"] = "CronEveryMinute",
        ["0 */5 * * * ?"] = "CronEvery5Minutes",
        ["0 */15 * * * ?"] = "CronEvery15Minutes",
        ["0 0 * * * ?"] = "CronEveryHour",
    };

    public static string Resolve(string cron, IStringLocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        return _presetKeys.TryGetValue(cron, out var key) ? localizer[key] : cron;
    }
}
