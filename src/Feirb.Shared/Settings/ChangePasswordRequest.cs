using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Settings;

public record ChangePasswordRequest(
    [Required]
    string CurrentPassword,
    [Required, StringLength(256, MinimumLength = 8)]
    string NewPassword);
