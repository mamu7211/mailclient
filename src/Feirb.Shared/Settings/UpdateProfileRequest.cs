using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Settings;

public record UpdateProfileRequest(
    [StringLength(100, MinimumLength = 2)]
    string? Username,
    [EmailAddress, StringLength(256)]
    string? Email);
