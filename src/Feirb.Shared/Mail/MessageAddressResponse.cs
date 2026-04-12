using Feirb.Shared.AddressBook;

namespace Feirb.Shared.Mail;

public record MessageAddressResponse(string Name, string Email, AddressStatus? Status = null);
