using System;
using System.IO;
using System.Threading.Tasks;
using ExambroMuhipo.Models;
using ExambroMuhipo.Services;
using Xunit;

namespace ExambroMuhipo.Tests;

public class SessionStateTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LoggingService _logger;
    private readonly ConfigurationService _configService;
    private readonly ExamSessionService _sessionService;

    public SessionStateTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ExambroSessionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _logger = new LoggingService(Path.Combine(_tempDir, "Logs"));
        _configService = new ConfigurationService(_logger, _tempDir);
        _configService.LoadConfiguration();
        _sessionService = new ExamSessionService(_configService, _logger);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Cleanup
        }
    }

    [Fact]
    public async Task SessionLifecycle_ShouldTransitionStatesCorrectly()
    {
        Assert.Equal(ExamSessionStatus.Idle, _sessionService.Status);

        var profile = new ExamProfile
        {
            ExamName = "Ujian Biologi",
            StartUrl = "https://example.com/bio",
            SessionTimeout = 45
        };

        // Mulai sesi
        await _sessionService.StartSessionAsync(profile);
        Assert.Equal(ExamSessionStatus.Running, _sessionService.Status);
        Assert.Equal(profile.ExamName, _sessionService.CurrentProfile?.ExamName);

        // Otorisasi keluar dengan password salah
        bool exitFailed = await _sessionService.RequestExitSessionAsync("wrong_password");
        Assert.False(exitFailed);
        Assert.Equal(ExamSessionStatus.Running, _sessionService.Status);

        // Otorisasi keluar dengan password benar (default exit password "MUHIPO23")
        bool exitSuccess = await _sessionService.RequestExitSessionAsync("MUHIPO23");
        Assert.True(exitSuccess);
        Assert.Equal(ExamSessionStatus.Completed, _sessionService.Status);
    }
}
