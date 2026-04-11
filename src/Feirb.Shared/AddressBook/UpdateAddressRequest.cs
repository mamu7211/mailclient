using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.AddressBook;

public record UpdateAddressRequest(
    [Required, StringLength(256)] string DisplayName,
    bool IsBlocked);
