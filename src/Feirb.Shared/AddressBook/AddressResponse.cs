namespace Feirb.Shared.AddressBook;

public record AddressResponse(
    Guid Id,
    Guid? ContactId,
    string? ContactDisplayName,
    string NormalizedEmail,
    string DisplayName,
    AddressStatus Status,
    DateTime FirstSeenAt,
    DateTime LastSeenAt,
    int SeenCount);
