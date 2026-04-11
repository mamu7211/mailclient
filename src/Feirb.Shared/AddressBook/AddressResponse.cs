namespace Feirb.Shared.AddressBook;

public record AddressResponse(
    Guid Id,
    Guid? ContactId,
    string? ContactDisplayName,
    string NormalizedEmail,
    string DisplayName,
    bool IsUnknown,
    bool IsBlocked,
    bool IsImportant,
    DateTime FirstSeenAt,
    DateTime LastSeenAt,
    int SeenCount);
