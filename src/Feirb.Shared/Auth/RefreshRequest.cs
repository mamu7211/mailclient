using System.ComponentModel.DataAnnotations;

namespace Feirb.Shared.Auth;

public record RefreshRequest(
    [Required] string RefreshToken);
