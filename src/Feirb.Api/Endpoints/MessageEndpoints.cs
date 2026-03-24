using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Api.Resources;
using Feirb.Shared.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class MessageEndpoints
{
    public static RouteGroupBuilder MapMessageEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/messages", ListMessagesAsync);
        group.MapGet("/messages/{id:guid}", GetMessageAsync);
        return group;
    }

    private static async Task<IResult> ListMessagesAsync(
        HttpContext httpContext,
        FeirbDbContext db,
        int page = 1,
        int pageSize = 25)
    {
        var userId = GetCurrentUserId(httpContext);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var query = db.CachedMessages
            .Where(m => m.Mailbox.UserId == userId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(m => m.Date)
            .Skip(skip)
            .Take(pageSize)
            .Select(m => new MessageListItemResponse(
                m.Id,
                m.Mailbox.Name,
                m.Mailbox.BadgeColor,
                m.From,
                m.Subject,
                m.Date,
                m.Attachments.Count > 0))
            .ToListAsync();

        return Results.Ok(new PaginatedResponse<MessageListItemResponse>(items, page, pageSize, totalCount));
    }

    private static async Task<IResult> GetMessageAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);

        var message = await db.CachedMessages
            .Include(m => m.Mailbox)
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == id && m.Mailbox.UserId == userId);

        if (message is null)
            return Results.NotFound(new { message = localizer["MessageNotFound"].Value });

        var response = new MessageDetailResponse(
            message.Id,
            message.Mailbox.Name,
            message.Mailbox.BadgeColor,
            message.From,
            message.To,
            message.Cc,
            message.ReplyTo,
            message.Date,
            message.Subject,
            message.BodyHtml,
            message.BodyPlainText,
            message.Attachments.Select(a => new AttachmentResponse(a.Id, a.Filename, a.Size, a.MimeType)).ToList());

        return Results.Ok(response);
    }

    private static Guid GetCurrentUserId(HttpContext httpContext) =>
        Guid.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
