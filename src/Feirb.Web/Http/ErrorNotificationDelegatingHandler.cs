using Feirb.Web.Resources;
using Feirb.Web.Services;
using Microsoft.Extensions.Localization;

namespace Feirb.Web.Http;

public sealed class ErrorNotificationDelegatingHandler(
    NotificationService notificationService,
    IStringLocalizer<SharedResources> localizer) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            notificationService.Add(localizer["ConnectionLost"], NotificationSeverity.Error);
            throw;
        }

        if ((int)response.StatusCode >= 500)
            notificationService.Add(localizer["ServerError"], NotificationSeverity.Error);

        return response;
    }
}
