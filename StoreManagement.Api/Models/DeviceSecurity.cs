using System.Security.Cryptography;
using System.Text;

namespace StoreManagement.Api.Services.Devices;

public static class DeviceSecurity
{
    public static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string HashSecret(string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    public static bool VerifySecret(
        string suppliedSecret,
        string storedHash)
    {
        try
        {
            var suppliedHash = Convert.FromHexString(
                HashSecret(suppliedSecret));

            var expectedHash = Convert.FromHexString(
                storedHash);

            return CryptographicOperations.FixedTimeEquals(
                suppliedHash,
                expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}