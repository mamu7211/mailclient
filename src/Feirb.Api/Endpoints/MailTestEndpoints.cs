using System.Net.Sockets;
using Feirb.Shared.Settings;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Feirb.Api.Endpoints;

public static class MailTestEndpoints
{
    public static RouteGroupBuilder MapMailTestEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/test/imap", TestImapAsync);
        group.MapPost("/test/smtp", TestSmtpAsync);
        return group;
    }

    private static async Task<IResult> TestImapAsync(TestImapRequest request)
    {
        try
        {
            using var client = new ImapClient();

            try
            {
                var tlsOptions = request.UseTls ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
                await client.ConnectAsync(request.Host, request.Port, tlsOptions);
            }
            catch (Exception ex) when (ex is SocketException or IOException)
            {
                return Results.Ok(new TestConnectionResponse(false, ex.Message, "dns"));
            }
            catch (SslHandshakeException ex)
            {
                return Results.Ok(new TestConnectionResponse(false, ex.Message, "tls"));
            }

            try
            {
                if (!string.IsNullOrEmpty(request.Username))
                    await client.AuthenticateAsync(request.Username, request.Password);
            }
            catch (AuthenticationException ex)
            {
                return Results.Ok(new TestConnectionResponse(false, ex.Message, "auth"));
            }

            await client.DisconnectAsync(quit: true);
            return Results.Ok(new TestConnectionResponse(true, null, null));
        }
        catch (Exception ex)
        {
            return Results.Ok(new TestConnectionResponse(false, ex.Message, null));
        }
    }

    private static async Task<IResult> TestSmtpAsync(TestSmtpMailboxRequest request)
    {
        try
        {
            using var client = new SmtpClient();

            try
            {
                var tlsOptions = request.UseTls ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
                await client.ConnectAsync(request.Host, request.Port, tlsOptions);
            }
            catch (Exception ex) when (ex is SocketException or IOException)
            {
                return Results.Ok(new TestConnectionResponse(false, ex.Message, "dns"));
            }
            catch (SslHandshakeException ex)
            {
                return Results.Ok(new TestConnectionResponse(false, ex.Message, "tls"));
            }

            try
            {
                if (request.RequiresAuth)
                    await client.AuthenticateAsync(request.Username, request.Password);
            }
            catch (AuthenticationException ex)
            {
                return Results.Ok(new TestConnectionResponse(false, ex.Message, "auth"));
            }

            await client.DisconnectAsync(quit: true);
            return Results.Ok(new TestConnectionResponse(true, null, null));
        }
        catch (Exception ex)
        {
            return Results.Ok(new TestConnectionResponse(false, ex.Message, null));
        }
    }
}
