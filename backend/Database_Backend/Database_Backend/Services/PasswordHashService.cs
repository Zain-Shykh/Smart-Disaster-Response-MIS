using System.Security.Cryptography;
using System.Text;

namespace Database_Backend.Services;

public static class PasswordHashService
{
    private const string Prefix = "pbkdf2";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int DefaultIterations = 120_000;

    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, DefaultIterations, HashAlgorithmName.SHA256, KeySize);

        return $"{Prefix}${DefaultIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string inputPassword, string storedHash, out bool needsRehash)
    {
        needsRehash = false;

        if (string.IsNullOrWhiteSpace(inputPassword) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        if (TryVerifyPbkdf2(inputPassword, storedHash, out var shouldUpgradeIterations))
        {
            needsRehash = shouldUpgradeIterations;
            return true;
        }

        if (string.Equals(inputPassword, storedHash, StringComparison.Ordinal))
        {
            needsRehash = true;
            return true;
        }

        var inputBytes = Encoding.UTF8.GetBytes(inputPassword);
        var sha256Bytes = SHA256.HashData(inputBytes);
        var sha256Hex = Convert.ToHexString(sha256Bytes);
        var sha256Base64 = Convert.ToBase64String(sha256Bytes);

        if (string.Equals(storedHash, sha256Hex, StringComparison.OrdinalIgnoreCase)
            || string.Equals(storedHash, sha256Base64, StringComparison.Ordinal))
        {
            needsRehash = true;
            return true;
        }

        return false;
    }

    private static bool TryVerifyPbkdf2(string inputPassword, string storedHash, out bool shouldUpgradeIterations)
    {
        shouldUpgradeIterations = false;

        var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !parts[0].Equals(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(inputPassword, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        var verified = CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
        if (!verified)
        {
            return false;
        }

        shouldUpgradeIterations = iterations < DefaultIterations;
        return true;
    }
}
