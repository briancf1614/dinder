using System.Security.Cryptography;
using System.Text;
using Dinder.Application.Common.Interfaces;

namespace Dinder.Infrastructure.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32;  // 256 bits
    private const int Iterations = 600_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            KeySize);

        return $"{Convert.ToHexString(salt)}.{Convert.ToHexString(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 2)
            return false;

        var salt = Convert.FromHexString(parts[0]);
        var storedHash = Convert.FromHexString(parts[1]);

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            KeySize);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }
}
