using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Settings;

public record CreateClassificationRuleRequest(
    [Required, StringLength(500)]
    string Instruction);
