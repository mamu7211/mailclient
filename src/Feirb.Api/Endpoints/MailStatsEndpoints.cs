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

        var totalCount = await db.CachedMessages
            .Where(m => m.Mailbox.UserId == userId)
            .CountAsync();

        var todayUtc = DateTimeOffset.UtcNow.Date;
        var startDate = new DateTimeOffset(todayUtc.AddDays(-(days - 1)), TimeSpan.Zero);

        var dailyCounts = await db.CachedMessages
            .Where(m => m.Mailbox.UserId == userId && m.Date >= startDate)
            .GroupBy(m => m.Date.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var mailsPerDay = Enumerable.Range(0, days)
            .Select(i => todayUtc.AddDays(-(days - 1) + i))
            .Select(date => new DailyMailCountItem(
                DateOnly.FromDateTime(date),
                dailyCounts.FirstOrDefault(d => d.Date == date)?.Count ?? 0))
            .ToList();

        return Results.Ok(new MailStatsResponse(totalCount, mailsPerDay));
    }

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing NameIdentifier claim. Ensure the endpoint requires authorization.");
        return Guid.Parse(claim.Value);
    }
}
