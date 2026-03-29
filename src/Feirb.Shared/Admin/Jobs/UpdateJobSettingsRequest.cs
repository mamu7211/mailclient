using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Admin.Jobs;

public record UpdateJobSettingsRequest(
    [Required] string Cron,
    bool Enabled,
    [Required] Guid RowVersion);
