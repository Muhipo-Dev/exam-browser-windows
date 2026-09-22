using System;
using ExambroMuhipo.Utilities;
using Xunit;

namespace ExambroMuhipo.Tests;

public class PasswordHashTests
{
    [Fact]
    public void HashPassword_ShouldGenerateSaltAndHash()
    {
        var (salt, hash) = PasswordHasher.HashPassword("adminMuhipo2026!");

        Assert.False(string.IsNullOrEmpty(salt));
        Assert.False(string.IsNullOrEmpty(hash));

        // Memastikan salt dan hash adalah Base64 yang valid
        var saltBytes = Convert.FromBase64String(salt);
        var hashBytes = Convert.FromBase64String(hash);

        Assert.Equal(16, saltBytes.Length);
        Assert.Equal(32, hashBytes.Length);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatches()
    {
        string password = "SecretSchoolPassword#123";
        var (salt, hash) = PasswordHasher.HashPassword(password);

        bool isValid = PasswordHasher.VerifyPassword(password, salt, hash);
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordIsIncorrect()
    {
        var (salt, hash) = PasswordHasher.HashPassword("correctPassword");

        bool isValid = PasswordHasher.VerifyPassword("wrongPassword", salt, hash);
        Assert.False(isValid);
    }

    [Fact]
    public void HashPassword_ShouldGenerateUniqueSalts_ForSamePassword()
    {
        string pwd = "samePassword";
        var (salt1, hash1) = PasswordHasher.HashPassword(pwd);
        var (salt2, hash2) = PasswordHasher.HashPassword(pwd);

        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);
    }
}
