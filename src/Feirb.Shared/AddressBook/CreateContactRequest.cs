using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.AddressBook;

public record CreateContactRequest(
    [Required, StringLength(256)] string DisplayName,
    [StringLength(2048)] string? Notes,
    bool IsImportant,
    IReadOnlyList<string>? Emails);
