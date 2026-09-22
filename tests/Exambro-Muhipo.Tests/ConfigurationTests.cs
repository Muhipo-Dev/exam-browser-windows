using System;
using System.IO;
using System.Text.Json;
using ExambroMuhipo.Models;
using ExambroMuhipo.Services;
using Xunit;

namespace ExambroMuhipo.Tests;

public class ConfigurationTests : IDisposable
{
    private readonly string _testDir;
    private readonly LoggingService _logger;

    public ConfigurationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "ExambroTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _logger = new LoggingService(Path.Combine(_testDir, "Logs"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch
        {
            // Ignored in cleanup
        }
    }

    [Fact]
    public void LoadConfiguration_ShouldCreateDefaultConfig_WhenFileNotExists()
    {
        var configService = new ConfigurationService(_logger, _testDir);
        configService.LoadConfiguration();

        Assert.NotNull(configService.Config);
        Assert.Equal("Exambro-Muhipo", configService.Config.AppName);
        Assert.Equal("SMA Muhammadiyah 1 Ponorogo", configService.Config.SchoolName);
        Assert.False(string.IsNullOrEmpty(configService.Config.AdminPasswordHash));
        Assert.False(string.IsNullOrEmpty(configService.Config.AdminPasswordSalt));
    }

    [Fact]
    public void LoadProfile_ShouldThrowException_WhenJsonIsMalformed()
    {
        var configService = new ConfigurationService(_logger, _testDir);
        string badProfilePath = Path.Combine(_testDir, "bad_profile.json");
        File.WriteAllText(badProfilePath, "{ this is not valid json! }");

        Assert.ThrowsAny<Exception>(() => configService.LoadProfile(badProfilePath));
    }

    [Fact]
    public void SaveAndLoadProfile_ShouldPreserveAllProperties()
    {
        var configService = new ConfigurationService(_logger, _testDir);
        var original = new ExamProfile
        {
            ExamName = "Ujian Matematika Peminatan",
            ExamDescription = "Ujian semester genap kelas XII",
            StartUrl = "https://example.com/math",
            AllowedDomains = new System.Collections.Generic.List<string> { "example.com", "math.example.com" },
            FullScreen = true,
            KioskMode = true,
            SessionTimeout = 120,
            AllowZoom = true,
            EnforceHttps = true
        };

        string targetPath = Path.Combine(_testDir, "math_exam.json");
        configService.SaveProfile(original, targetPath);

        var loaded = configService.LoadProfile(targetPath);
        Assert.Equal(original.ExamName, loaded.ExamName);
        Assert.Equal(original.StartUrl, loaded.StartUrl);
        Assert.Equal(original.SessionTimeout, loaded.SessionTimeout);
        Assert.Equal(original.AllowZoom, loaded.AllowZoom);
        Assert.Equal(original.AllowedDomains.Count, loaded.AllowedDomains.Count);
    }
}
