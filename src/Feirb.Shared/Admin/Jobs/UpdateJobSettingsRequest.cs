using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Admin.Jobs;

public record UpdateJobSettingsRequest(
    [Required] string Cron,
    bool Enabled,
    string? Configuration,
    [Required] Guid RowVersion);
