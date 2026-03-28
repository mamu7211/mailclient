using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Settings;

public record UpdatePreferencesRequest(
    [Required, StringLength(32)]
    string Theme);
