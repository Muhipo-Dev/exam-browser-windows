using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;
using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Implementasi manajemen siklus sesi ujian, timer, state machine, dan crash recovery.
/// </summary>
public class ExamSessionService : IExamSessionService
{
    private readonly IConfigurationService _configService;
    private readonly ILoggingService _logger;
    private readonly string _recoveryFilePath;
    private DispatcherTimer? _sessionTimer;
    private DateTime? _endTime;

    public ExamSessionStatus Status { get; private set; } = ExamSessionStatus.Idle;
    public ExamProfile? CurrentProfile { get; private set; }
    public TimeSpan RemainingTime { get; private set; } = TimeSpan.Zero;
    public DateTime? StartTime { get; private set; }

    public event Action<ExamSessionStatus>? StatusChanged;
    public event Action<TimeSpan>? TimeRemainingUpdated;
    public event Action? SessionTimedOut;

    public ExamSessionService(IConfigurationService configService, ILoggingService logger)
    {
        _configService = configService;
        _logger = logger;

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(appData, "Exambro-Muhipo");
        Directory.CreateDirectory(appFolder);
        _recoveryFilePath = Path.Combine(appFolder, "session_lock.json");
    }

    public Task StartSessionAsync(ExamProfile profile)
    {
        CurrentProfile = profile ?? throw new ArgumentNullException(nameof(profile));
        SetStatus(ExamSessionStatus.Preparing);

        StartTime = DateTime.UtcNow;

        if (profile.SessionTimeout > 0)
        {
            RemainingTime = TimeSpan.FromMinutes(profile.SessionTimeout);
            _endTime = DateTime.UtcNow.AddMinutes(profile.SessionTimeout);
            StartTimer();
        }
        else
        {
            RemainingTime = TimeSpan.Zero;
            _endTime = null;
        }

        // Tulis recovery marker untuk mendeteksi crash/power failure
        WriteRecoveryMarker(ExamSessionStatus.Running);

        SetStatus(ExamSessionStatus.Running);
        _logger.LogInfo(AuditEventType.ExamStarted, $"Sesi ujian '{profile.ExamName}' dimulai.", $"URL: {profile.StartUrl} | Durasi: {profile.SessionTimeout} menit");

        return Task.CompletedTask;
    }

    private void StartTimer()
    {
        StopTimer();
        _sessionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _sessionTimer.Tick += OnTimerTick;
        _sessionTimer.Start();
    }

    private void StopTimer()
    {
        if (_sessionTimer != null)
        {
            _sessionTimer.Stop();
            _sessionTimer.Tick -= OnTimerTick;
            _sessionTimer = null;
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_endTime == null)
            return;

        var remaining = _endTime.Value - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            RemainingTime = TimeSpan.Zero;
            TimeRemainingUpdated?.Invoke(RemainingTime);
            StopTimer();
            _logger.LogWarning(AuditEventType.ExamCompleted, "Batas waktu sesi ujian telah habis (Session Timeout).");
            SessionTimedOut?.Invoke();
        }
        else
        {
            RemainingTime = remaining;
            TimeRemainingUpdated?.Invoke(RemainingTime);
        }
    }

    public Task<bool> RequestExitSessionAsync(string adminPassword)
    {
        _logger.LogInfo(AuditEventType.ExitAttempt, "Percobaan keluar dari sesi ujian diinisiasi.");

        bool isAuthorized = _configService.VerifyAdminPassword(adminPassword);
        if (isAuthorized)
        {
            _logger.LogInfo(AuditEventType.ExitAuthorized, "Otorisasi keluar dari sesi ujian disetujui administrator.");
            EndSessionInternal(ExamSessionStatus.Completed);
            return Task.FromResult(true);
        }
        else
        {
            _logger.LogWarning(AuditEventType.ExitDenied, "Otorisasi keluar ditolak: password administrator tidak cocok.");
            return Task.FromResult(false);
        }
    }

    public Task ForceEndSessionAsync()
    {
        EndSessionInternal(ExamSessionStatus.Completed);
        return Task.CompletedTask;
    }

    private void EndSessionInternal(ExamSessionStatus finalStatus)
    {
        SetStatus(ExamSessionStatus.Finishing);
        StopTimer();
        ClearRecoveryMarker();

        SetStatus(finalStatus);
        _logger.LogInfo(AuditEventType.ExamCompleted, "Sesi ujian berakhir dengan aman.");
    }

    public SessionRecoveryInfo? CheckUnfinishedSession()
    {
        try
        {
            if (!File.Exists(_recoveryFilePath))
                return null;

            string json = File.ReadAllText(_recoveryFilePath);
            var info = JsonSerializer.Deserialize<SessionRecoveryInfo>(json);

            // Jika sesi sebelumnya masih 'Running' dan tidak graceful shutdown
            if (info != null && info.Status == ExamSessionStatus.Running && !info.IsGracefulShutdown)
            {
                _logger.LogWarning(AuditEventType.ApplicationError, "Terdeteksi sesi ujian sebelumnya belum berakhir secara normal (Crash/Unfinished Session).");
                return info;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(AuditEventType.ApplicationError, "Gagal memeriksa status recovery sesi.", exception: ex);
        }

        return null;
    }

    public void ClearRecoveryMarker()
    {
        try
        {
            if (File.Exists(_recoveryFilePath))
            {
                File.Delete(_recoveryFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(AuditEventType.ApplicationError, "Gagal menghapus recovery marker file.", exception: ex);
        }
    }

    public void MarkHeartbeat()
    {
        if (Status == ExamSessionStatus.Running)
        {
            WriteRecoveryMarker(ExamSessionStatus.Running);
        }
    }

    private void WriteRecoveryMarker(ExamSessionStatus currentStatus)
    {
        try
        {
            var info = new SessionRecoveryInfo
            {
                ExamName = CurrentProfile?.ExamName ?? "Unknown Exam",
                ProfileFile = _configService.Config.ActiveProfileFile,
                StartTime = StartTime ?? DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow,
                Status = currentStatus,
                IsGracefulShutdown = false
            };

            string json = JsonSerializer.Serialize(info);
            File.WriteAllText(_recoveryFilePath, json);
        }
        catch
        {
            // Ignored to avoid crashing during heartbeat write
        }
    }

    private void SetStatus(ExamSessionStatus newStatus)
    {
        if (Status != newStatus)
        {
            Status = newStatus;
            StatusChanged?.Invoke(Status);
        }
    }
}
