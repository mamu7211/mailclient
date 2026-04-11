namespace Feirb.Shared.Mail;

/// <summary>A recipient entry with an optional display name and an email address.</summary>
public record RecipientEntry(string? Name, string Email);
