namespace Feirb.Shared.AddressBook;

public record ContactListItem(
    Guid Id,
    string DisplayName,
    int AddressCount,
    string? PrimaryEmail,
    DateTime UpdatedAt);
