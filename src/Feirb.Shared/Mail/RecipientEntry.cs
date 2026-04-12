using Feirb.Shared.AddressBook;

namespace Feirb.Shared.Mail;

/// <summary>A recipient entry with an optional display name, an email address, and an optional address-book status.</summary>
public record RecipientEntry(string? Name, string Email, AddressStatus? Status = null);
