using Feirb.Api.Resources;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Services;

public static class EmailTemplates
{
    public static string BuildPasswordResetEmail(
        string username,
        string resetLink,
        IStringLocalizer<ApiMessages> localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        </head>
        <body style="margin:0; padding:0; background-color:#f5f5f5; font-family:Arial, Helvetica, sans-serif;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f5f5f5; padding:32px 0;">
                <tr>
                    <td align="center">
                        <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="background-color:#ffffff; border-radius:8px; overflow:hidden;">
                            <tr>
                                <td style="background-color:#0d6efd; padding:24px 32px;">
                                    <h1 style="margin:0; color:#ffffff; font-size:24px; font-weight:700;">Feirb</h1>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding:32px;">
                                    <p style="margin:0 0 16px; font-size:16px; color:#333333;">
                                        {localizer["ResetEmailGreeting", username]}
                                    </p>
                                    <p style="margin:0 0 24px; font-size:16px; color:#333333;">
                                        {localizer["ResetEmailBody"]}
                                    </p>
                                    <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 0 24px;">
                                        <tr>
                                            <td style="background-color:#0d6efd; border-radius:6px;">
                                                <a href="{resetLink}" style="display:inline-block; padding:12px 24px; color:#ffffff; text-decoration:none; font-size:16px; font-weight:600;">
                                                    {localizer["ResetEmailButtonText"]}
                                                </a>
                                            </td>
                                        </tr>
                                    </table>
                                    <p style="margin:0 0 16px; font-size:14px; color:#666666;">
                                        {localizer["ResetEmailExpiry"]}
                                    </p>
                                    <p style="margin:0; font-size:14px; color:#666666;">
                                        {localizer["ResetEmailDisclaimer"]}
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }
}
