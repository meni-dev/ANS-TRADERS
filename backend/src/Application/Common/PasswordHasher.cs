using System.Security.Cryptography;

namespace Application.Common;

/// <summary>
/// Turns a password into something safe to store, and checks one against it.
/// <para>
/// PBKDF2 from the standard library rather than an Identity package: the app needs exactly two
/// functions, and pulling in a framework for them would bring a user store, a claims model and a
/// migration set the shop has no use for.
/// </para>
/// <para>
/// Format is <c>iterations.salt.hash</c>, all base64. The iteration count travels with the hash so
/// it can be raised later without invalidating every existing password.
/// </para>
/// </summary>
public static class PasswordHasher
{
    // OWASP's floor for PBKDF2-SHA256 at the time of writing. Raising it later only affects
    // passwords set after the change, which is why the count is stored per hash.
    private const int Iterations = 600_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Derive(password, salt, Iterations);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.');

        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt, expected;

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        // Fixed-time comparison: a byte-by-byte one leaks how much of the hash matched, which is
        // enough to reconstruct it one byte at a time.
        return CryptographicOperations.FixedTimeEquals(
            Derive(password, salt, iterations), expected);
    }

    /// <summary>
    /// A password nobody chose, for a new account. Read once from the log or handed over in person,
    /// then replaced — <see cref="Domain.Entities.User.MustChangePassword"/> makes sure of that.
    /// </summary>
    public static string GenerateTemporary()
    {
        // Deliberately no l/1/I or O/0 — this gets read off a screen and typed by hand.
        const string alphabet = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var chars = new char[12];

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(chars);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashBytes);
}
