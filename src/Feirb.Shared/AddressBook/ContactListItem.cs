namespace Feirb.Shared.AddressBook;

public record ContactListItem(
    Guid Id,
    string DisplayName,
    bool IsImportant,
    int AddressCount,
    string? PrimaryEmail,
    DateTime UpdatedAt);
