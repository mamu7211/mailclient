using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.AddressBook;

public record LinkAddressRequest([Required] Guid ContactId);
