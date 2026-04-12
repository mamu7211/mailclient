namespace Feirb.Shared.Mail;

public static class EmailNormalizer
{
    public static string Normalize(string email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();
}
