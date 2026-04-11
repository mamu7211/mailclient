namespace Feirb.Shared.AddressBook;

public record ContactResponse(
    Guid Id,
    string DisplayName,
    string? Notes,
    bool IsImportant,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<AddressResponse> Addresses);
