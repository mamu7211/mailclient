using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Shared.Mail;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Endpoints;

public static class MailStatsEndpoints
{
    private static readonly int[] _allowedDays = [7, 14, 30, 90, 180, 365];

    public static RouteGroupBuilder MapMailStatsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/stats", GetStatsAsync);
        return group;
    }

    private static async Task<IResult> GetStatsAsync(
        HttpContext httpContext,
        FeirbDbContext db,
        int days = 7)
    {
        var userId = GetCurrentUserId(httpContext);

        if (!_allowedDays.Contains(days))
            days = 7;

        var mailboxes = await db.Mailboxes
            .Where(m => m.UserId == userId)
            .Select(m => new MailboxInfo(m.Id, m.Name, m.BadgeColor))
            .ToListAsync();

        var mailsPerMailbox = await db.CachedMessages
            .Where(m => m.Mailbox.UserId == userId)
            .GroupBy(m => m.MailboxId)
            .Select(g => new MailboxCount(g.Key, g.Count()))
            .ToListAsync();

        var perMailboxStats = mailboxes
            .Select(mb => new MailboxMailStats(
                mb.Id,
                mb.Name,
                mb.BadgeColor,
                mailsPerMailbox.FirstOrDefault(c => c.MailboxId == mb.Id)?.Count ?? 0))
            .ToList();

        var totalCount = perMailboxStats.Sum(s => s.Count);

        var granularity = days switch
        {
            <= 14 => StatsGranularity.Daily,
            <= 90 => StatsGranularity.Weekly,
            _ => StatsGranularity.Monthly
        };

        var todayUtc = DateTimeOffset.UtcNow.Date;
        var startDate = new DateTimeOffset(todayUtc.AddDays(-(days - 1)), TimeSpan.Zero);

        var rawCounts = await db.CachedMessages
            .Where(m => m.Mailbox.UserId == userId && m.Date >= startDate)
            .GroupBy(m => new { Date = m.Date.Date, m.MailboxId })
            .Select(g => new DailyMailboxCount(g.Key.Date, g.Key.MailboxId, g.Count()))
            .ToListAsync();

        var timeSeries = BuildTimeSeries(rawCounts, mailboxes, todayUtc, days, granularity);

        return Results.Ok(new MailStatsResponse(totalCount, perMailboxStats, timeSeries, granularity));
    }

    private static List<MailStats> BuildTimeSeries(
        List<DailyMailboxCount> rawCounts,
        List<MailboxInfo> mailboxes,
        DateTime todayUtc,
        int days,
        StatsGranularity granularity)
    {
        var startDate = todayUtc.AddDays(-(days - 1));
        var allDates = Enumerable.Range(0, days)
            .Select(i => startDate.AddDays(i))
            .ToList();

        var buckets = granularity switch
        {
            StatsGranularity.Daily => allDates
                .Select(d => new Bucket(d.ToString("MM/dd"), [d]))
                .ToList(),
            StatsGranularity.Weekly => allDates
                .GroupBy(d => System.Globalization.ISOWeek.GetWeekOfYear(d))
                .Select(g => new Bucket($"W{g.Key}", g.ToList()))
                .ToList(),
            StatsGranularity.Monthly => allDates
                .GroupBy(d => new { d.Year, d.Month })
                .Select(g => new Bucket($"{g.Key.Year}-{g.Key.Month:D2}", g.ToList()))
                .ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity))
        };

        return buckets.Select(bucket =>
        {
            var mailboxCounts = mailboxes.Select(mb =>
            {
                var count = rawCounts
                    .Where(r => bucket.Dates.Contains(r.Date) && r.MailboxId == mb.Id)
                    .Sum(r => r.Count);
                return new MailboxMailStats(mb.Id, mb.Name, mb.BadgeColor, count);
            }).ToList();

            return new MailStats(bucket.Label, mailboxCounts);
        }).ToList();
    }

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing NameIdentifier claim. Ensure the endpoint requires authorization.");
        return Guid.Parse(claim.Value);
    }

    private sealed record MailboxInfo(Guid Id, string Name, string? BadgeColor);
    private sealed record MailboxCount(Guid MailboxId, int Count);
    private sealed record DailyMailboxCount(DateTime Date, Guid MailboxId, int Count);
    private sealed record Bucket(string Label, List<DateTime> Dates);
}
