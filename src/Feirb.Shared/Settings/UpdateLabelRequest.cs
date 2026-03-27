using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Settings;

public record UpdateLabelRequest(
    [Required, StringLength(50)]
    string Name,
    [StringLength(7)]
    string? Color,
    string? Description);
