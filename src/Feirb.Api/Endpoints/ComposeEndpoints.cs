using System.Security.Claims;
using Feirb.Api.Resources;
using Feirb.Api.Services;
using Feirb.Shared.Mail;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class ComposeEndpoints
{
    public static RouteGroupBuilder MapComposeEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/send", SendMailAsync);
        return group;
    }

    private static async Task<IResult> SendMailAsync(
        HttpContext httpContext,
        SendMailRequest request,
        IMailSendingService mailSendingService,
        IStringLocalizer<ApiMessages> localizer,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(httpContext);

        if (request.To.Length == 0)
            return Results.BadRequest(new { message = localizer["InvalidEmailAddress"].Value });

        // Validate email addresses
        var allRecipients = request.To
            .Concat(request.Cc ?? [])
            .Concat(request.Bcc ?? []);

        foreach (var email in allRecipients)
        {
            if (!IsValidEmail(email.Trim()))
                return Results.BadRequest(new { message = localizer["InvalidEmailAddress"].Value, email });
        }

        try
        {
            var messageId = await mailSendingService.SendMailAsync(userId, request, cancellationToken);
            return Results.Ok(new SendMailResponse(messageId));
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound(new { message = localizer["MailboxNotFound"].Value });
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing NameIdentifier claim. Ensure the endpoint requires authorization.");
        return Guid.Parse(claim.Value);
    }
}
