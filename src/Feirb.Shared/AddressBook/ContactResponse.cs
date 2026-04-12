namespace Feirb.Shared.AddressBook;

public record ContactResponse(
    Guid Id,
    string DisplayName,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<AddressResponse> Addresses);
