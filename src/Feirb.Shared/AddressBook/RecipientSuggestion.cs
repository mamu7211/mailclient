namespace Feirb.Shared.AddressBook;

public record RecipientSuggestion(
    string DisplayName,
    string Email,
    AddressStatus Status);
