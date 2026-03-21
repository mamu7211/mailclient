using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Auth;

public record RegisterRequest(
    [Required, StringLength(100, MinimumLength = 3)]
    string Username,
    [Required, EmailAddress, StringLength(256)]
    string Email,
    [Required, MinLength(8)]
    string Password);
