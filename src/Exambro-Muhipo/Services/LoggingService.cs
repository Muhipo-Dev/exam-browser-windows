using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Implementasi layanan logging audit terstruktur dengan rotasi file harian.
/// Menjamin privasi dengan tidak mencatat password, token, cookie, kredensial, atau jawaban siswa.
/// </summary>
public class LoggingService : ILoggingService
{
    private readonly string _logsDirectory;
    private readonly object _fileLock = new();
    private readonly ConcurrentQueue<LogEntry> _memoryLogs = new();
    private const int MaxMemoryLogCount = 500;

    // Pola sanitasi regex untuk menyaring token/password jika tidak sengaja terbawa di URL atau pesan
    private static readonly Regex SensitiveParamRegex = new(
        @"(password|token|secret|credential|pwd|auth|cookie)=([^&;\s]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public LoggingService(string? customLogsDirectory = null)
    {
        if (!string.IsNullOrEmpty(customLogsDirectory))
        {
            _logsDirectory = customLogsDirectory;
        }
        else
        {
            // Simpan di direktori aplikasi / Logs
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _logsDirectory = Path.Combine(baseDir, "Logs");
        }

        try
        {
            if (!Directory.Exists(_logsDirectory))
            {
                Directory.CreateDirectory(_logsDirectory);
            }
        }
        catch
        {
            // Fallback ke LocalAppData jika direktori aplikasi diproteksi
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _logsDirectory = Path.Combine(appData, "Exambro-Muhipo", "Logs");
            Directory.CreateDirectory(_logsDirectory);
        }
    }

    public void Log(LogLevel level, AuditEventType eventType, string message, string? details = null)
    {
        string sanitizedMsg = Sanitize(message);
        string? sanitizedDetails = details != null ? Sanitize(details) : null;

        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            EventType = eventType,
            Message = sanitizedMsg,
            Details = sanitizedDetails
        };

        _memoryLogs.Enqueue(entry);
        while (_memoryLogs.Count > MaxMemoryLogCount)
        {
            _memoryLogs.TryDequeue(out _);
        }

        WriteToFile(entry);
    }

    public void LogInfo(AuditEventType eventType, string message, string? details = null)
    {
        Log(LogLevel.Info, eventType, message, details);
    }

    public void LogWarning(AuditEventType eventType, string message, string? details = null)
    {
        Log(LogLevel.Warning, eventType, message, details);
    }

    public void LogError(AuditEventType eventType, string message, string? details = null, Exception? exception = null)
    {
        string combinedDetails = details ?? string.Empty;
        if (exception != null)
        {
            combinedDetails += (combinedDetails.Length > 0 ? " | " : "") + $"Exception: {exception.GetType().Name}: {exception.Message}";
        }
        Log(LogLevel.Error, eventType, message, combinedDetails);
    }

    public IReadOnlyList<LogEntry> GetRecentLogs(int count = 100)
    {
        return _memoryLogs.TakeLast(count).Reverse().ToList();
    }

    public string GetLogsDirectory() => _logsDirectory;

    private void WriteToFile(LogEntry entry)
    {
        try
        {
            string fileName = $"exambro-audit-{DateTime.UtcNow:yyyyMMdd}.log";
            string filePath = Path.Combine(_logsDirectory, fileName);

            lock (_fileLock)
            {
                using var sw = new StreamWriter(filePath, append: true, System.Text.Encoding.UTF8);
                sw.WriteLine(entry.ToString());
            }
        }
        catch
        {
            // Hindari melempar exception dari logger agar aplikasi tidak crash
        }
    }

    private static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Masking parameter sensitif
        return SensitiveParamRegex.Replace(input, "$1=[REDACTED]");
    }
}
