using System;
using System.Text.Json.Serialization;

namespace ExambroMuhipo.Models;

/// <summary>
/// Jenis event audit sistem Exambro-Muhipo.
/// </summary>
public enum AuditEventType
{
    ApplicationStarted,
    ConfigurationLoaded,
    ConfigurationError,
    AdminLoginSuccess,
    AdminLoginFailed,
    ExamStarted,
    NavigationAllowed,
    NavigationBlocked,
    PopupBlocked,
    NewWindowBlocked,
    DownloadBlocked,
    PermissionBlocked,
    ExitAttempt,
    ExitDenied,
    ExitAuthorized,
    ExamCompleted,
    WebViewCrashed,
    ApplicationError
}

/// <summary>
/// Tingkat keparahan log.
/// </summary>
public enum LogLevel
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Entri log audit terstruktur.
/// </summary>
public class LogEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("level")]
    public LogLevel Level { get; set; } = LogLevel.Info;

    [JsonPropertyName("eventType")]
    public AuditEventType EventType { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("sessionTimeRemaining")]
    public string? SessionTimeRemaining { get; set; }

    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level.ToString().ToUpper()}] [{EventType}] {Message}{(string.IsNullOrEmpty(Details) ? "" : " | " + Details)}";
    }
}
