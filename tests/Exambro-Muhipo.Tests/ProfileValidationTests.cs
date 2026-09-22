using System.Collections.Generic;
using ExambroMuhipo.Models;
using Xunit;

namespace ExambroMuhipo.Tests;

public class ProfileValidationTests
{
    [Fact]
    public void Validate_ShouldReturnErrors_WhenFieldsAreInvalid()
    {
        var invalidProfile = new ExamProfile
        {
            ExamName = "", // Empty
            StartUrl = "invalid-url",
            AllowedDomains = new List<string>(), // Empty
            SessionTimeout = -5 // Negative
        };

        var errors = invalidProfile.Validate();

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Nama ujian"));
        Assert.Contains(errors, e => e.Contains("Start URL"));
        Assert.Contains(errors, e => e.Contains("AllowedDomains"));
        Assert.Contains(errors, e => e.Contains("timeout"));
    }

    [Fact]
    public void Validate_ShouldPass_WhenProfileIsValid()
    {
        var validProfile = new ExamProfile
        {
            ExamName = "Ujian Tengah Semester Gasal",
            StartUrl = "https://example.com/uts",
            AllowedDomains = new List<string> { "example.com" },
            SessionTimeout = 90
        };

        var errors = validProfile.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void Clone_ShouldCreateIndependentDeepCopy()
    {
        var original = new ExamProfile
        {
            ExamName = "Asli",
            AllowedDomains = new List<string> { "example.com" }
        };

        var copy = original.Clone();
        copy.ExamName = "Salinan";
        copy.AllowedDomains.Add("extra.com");

        Assert.Equal("Asli", original.ExamName);
        Assert.Single(original.AllowedDomains);
        Assert.Equal(2, copy.AllowedDomains.Count);
    }
}
