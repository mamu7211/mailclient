using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Settings;

public record UpdateClassificationRuleRequest(
    [Required, StringLength(500)]
    string Instruction);
