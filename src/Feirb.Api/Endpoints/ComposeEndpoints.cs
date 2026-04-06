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
        ILogger<MailSendingService> logger,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(httpContext);

        // Explicit validation — Minimal APIs don't enforce DataAnnotations
        if (request.To is null || request.To.Length == 0)
            return Results.BadRequest(new { message = localizer["RecipientRequired"].Value });

        if (string.IsNullOrWhiteSpace(request.Subject))
            return Results.BadRequest(new { message = localizer["SubjectRequired"].Value });

        if (string.IsNullOrWhiteSpace(request.Body))
            return Results.BadRequest(new { message = localizer["BodyRequired"].Value });

        if (request.ContentType is not ("html" or "plain"))
            return Results.BadRequest(new { message = localizer["InvalidContentType"].Value });

        if (request.Body.Length > 1_048_576)
            return Results.BadRequest(new { message = localizer["BodyTooLarge"].Value });

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send mail for user {UserId} via mailbox {MailboxId}", userId, request.MailboxId);
            return Results.Problem(localizer["SendMailFailed"].Value, statusCode: 500);
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
