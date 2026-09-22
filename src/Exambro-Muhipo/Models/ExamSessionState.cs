using System;
using System.Text.Json.Serialization;

namespace ExambroMuhipo.Models;

/// <summary>
/// Status siklus sesi ujian sesuai arsitektur aplikasi.
/// </summary>
public enum ExamSessionStatus
{
    Idle,
    Preparing,
    Running,
    Finishing,
    Completed,
    Error
}

/// <summary>
/// Informasi sesi aktif untuk crash recovery dan audit.
/// </summary>
public class SessionRecoveryInfo
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("examName")]
    public string ExamName { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastHeartbeat")]
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("profileFile")]
    public string ProfileFile { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public ExamSessionStatus Status { get; set; } = ExamSessionStatus.Idle;

    [JsonPropertyName("isGracefulShutdown")]
    public bool IsGracefulShutdown { get; set; } = false;
}
