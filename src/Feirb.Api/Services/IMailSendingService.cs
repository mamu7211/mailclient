using Feirb.Shared.Mail;

namespace Feirb.Api.Services;

public interface IMailSendingService
{
    Task<string> SendMailAsync(Guid userId, SendMailRequest request, CancellationToken cancellationToken = default);
}
