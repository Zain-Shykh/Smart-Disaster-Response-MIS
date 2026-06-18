using System.Security.Cryptography;
using System.Text;

namespace Database_Backend.Services;

public static class ConcurrencyTokenService
{
    public static string Compute(params object?[] values)
    {
        var payload = string.Join("|", values.Select(v => v?.ToString() ?? "<null>"));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(bytes);
    }
}
