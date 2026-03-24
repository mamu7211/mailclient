using System.Globalization;
using System.Resources;
using FluentAssertions;

namespace Feirb.Web.Tests.Localization;

public class ResourceCompletenessTests
{
    private static readonly string[] _expectedKeys =
    [
        "AppName",
        "NavHome",
        "HomeTagline",
        "PageTitleHome",
        "PageTitleRegister",
        "RegisterHeading",
        "RegisterSubheading",
        "LabelUsername",
        "LabelEmail",
        "LabelPassword",
        "LabelConfirmPassword",
        "PlaceholderUsername",
        "PlaceholderEmail",
        "PlaceholderPassword",
        "PlaceholderConfirmPassword",
        "ButtonCreateAccount",
        "AlreadyHaveAccount",
        "LinkLogIn",
        "ErrorPasswordsDoNotMatch",
        "ErrorUsernameOrEmailTaken",
        "ErrorRegistrationFailed",
        "NotFoundTitle",
        "NotFoundMessage",
        "LanguageSwitcherLabel",
        "SettingsMailTitle",
        "SettingsMailboxesTitle",
        "SettingsMailboxesSubtitle",
        "PageTitleMailboxes",
        "PageTitleNewMailbox",
        "PageTitleEditMailbox",
        "BreadcrumbMailboxes",
        "BreadcrumbNewMailbox",
        "MailboxesHeading",
        "MailboxSectionGeneral",
        "MailboxSectionImap",
        "MailboxSectionSmtp",
        "LabelMailboxName",
        "LabelEmailAddress",
        "LabelDisplayName",
        "LabelImapHost",
        "LabelImapPort",
        "LabelImapUsername",
        "LabelImapPassword",
        "LabelImapUseTls",
        "LabelSmtpHostMailbox",
        "LabelSmtpPortMailbox",
        "LabelSmtpUsernameMailbox",
        "LabelSmtpPasswordMailbox",
        "LabelSmtpUseTlsMailbox",
        "LabelSmtpRequiresAuthMailbox",
        "ButtonAddMailbox",
        "TestConnectionSuccess",
        "TestConnectionFailedDns",
        "TestConnectionFailedTls",
        "TestConnectionFailedAuth",
        "TestConnectionFailed",
        "PasswordPlaceholderKeep",
        "ConfirmDeleteMailboxTitle",
        "ConfirmDeleteMailboxMessage",
        "SuccessMailboxCreated",
        "SuccessMailboxUpdated",
        "SuccessMailboxDeleted",
        "ErrorMailboxFailed",
        "NoMailboxesFound",
        "LoadingMailboxes",
        "PlaceholderMailboxName",
        "PlaceholderEmailAddress",
        "PlaceholderDisplayName",
        "PlaceholderImapHost",
        "PlaceholderSmtpHostMailbox",
        "PageTitleMessageDetail",
        "ColumnMailbox",
        "ColumnFrom",
        "ColumnSubject",
        "ColumnDate",
        "LoadingMessages",
        "NoMessagesFound",
        "ErrorLoadFailed",
        "LoadingMessage",
        "ErrorMessageNotFound",
        "LabelFrom",
        "LabelTo",
        "LabelCc",
        "LabelReplyTo",
        "LabelDate",
        "LabelSubject",
        "AttachmentsHeading",
        "PaginationPrevious",
        "PaginationNext",
        "PaginationPageOf",
        "NoBodyContent",
        "ConnectionLost",
        "ServerError",
    ];

    /// <summary>Keys that may be identical across languages (brand names, universal terms).</summary>
    private static readonly HashSet<string> _skipDiffCheck =
    [
        "AppName", "PageTitleHome", "NavHome", "LabelPassword",
        "LabelMailboxName", "LabelImapPassword", "LabelSmtpPasswordMailbox",
        "LabelCc", "ColumnDate", "LabelDate", "PageTitleMessageDetail",
    ];

    private static readonly ResourceManager _resourceManager = new(
        "Feirb.Web.Resources.SharedResources",
        typeof(Feirb.Web.Resources.SharedResources).Assembly);

    [Fact]
    public void SharedResources_EnUs_AllKeysHaveValues()
    {
        var culture = new CultureInfo("en-US");

        foreach (var key in _expectedKeys)
        {
            var value = _resourceManager.GetString(key, culture);
            value.Should().NotBeNullOrWhiteSpace($"key '{key}' should have a non-empty value in en-US");
        }
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("it-IT")]
    public void SharedResources_AllKeysHaveTranslations(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        var fallback = new CultureInfo("en-US");

        foreach (var key in _expectedKeys)
        {
            var value = _resourceManager.GetString(key, culture);
            value.Should().NotBeNullOrWhiteSpace($"key '{key}' should have a non-empty value in {cultureName}");

            if (!_skipDiffCheck.Contains(key))
            {
                var fallbackValue = _resourceManager.GetString(key, fallback);
                value.Should().NotBe(fallbackValue,
                    $"key '{key}' in {cultureName} should differ from en-US fallback (actual translation expected)");
            }
        }
    }
}
