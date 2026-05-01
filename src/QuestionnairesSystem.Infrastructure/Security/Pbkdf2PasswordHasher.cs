using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;

namespace QuestionnairesSystem.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;

    private const int KeySize = 32;

    private const int Iterations = 100_000;

    private const string Prefix = "v1";

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            Iterations,
            KeySize);
        return $"{Prefix}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split(':', 3);
        if (parts.Length != 3 || parts[0] != Prefix)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch
        {
            return false;
        }

        var actual = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            Iterations,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

