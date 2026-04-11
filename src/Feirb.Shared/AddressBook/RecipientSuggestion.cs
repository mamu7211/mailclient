namespace Feirb.Shared.AddressBook;

public enum RecipientStatus
{
    None = 0,
    Known = 1,
    Unknown = 2,
    Important = 3,
    Blocked = 4,
}

public record RecipientSuggestion(
    string DisplayName,
    string Email,
    RecipientStatus Status);
