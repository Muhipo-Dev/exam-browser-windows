using System;
using System.Security.Cryptography;

namespace ExambroMuhipo.Utilities;

/// <summary>
/// Utility untuk hashing dan verifikasi password administrator menggunakan PBKDF2 (HMAC-SHA256).
/// Mengamankan credential administrator agar tidak disimpan dalam plaintext.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int HashSize = 32; // 256 bit
    private const int Iterations = 100_000;

    /// <summary>
    /// Menghasilkan salt dan hash PBKDF2 dari password teks mentah.
    /// </summary>
    public static (string SaltBase64, string HashBase64) HashPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            throw new ArgumentException("Password tidak boleh kosong.", nameof(plainPassword));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            plainPassword,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    /// <summary>
    /// Memverifikasi apakah password cocok dengan hash dan salt yang tersimpan.
    /// Menggunakan perbandingan waktu konstan (FixedTimeEquals) untuk mencegah timing attack.
    /// </summary>
    public static bool VerifyPassword(string plainPassword, string saltBase64, string expectedHashBase64)
    {
        if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(saltBase64) || string.IsNullOrEmpty(expectedHashBase64))
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(saltBase64);
            byte[] expectedHash = Convert.FromBase64String(expectedHashBase64);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                plainPassword,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }
}
