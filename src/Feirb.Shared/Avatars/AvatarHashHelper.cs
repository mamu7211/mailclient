using System.Security.Cryptography;
using System.Text;

namespace Feirb.Shared.Avatars;

public static class AvatarHashHelper
{
    public static string ComputeEmailHash(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        var normalized = email.Trim().ToLowerInvariant();
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexStringLower(bytes);
    }
}
