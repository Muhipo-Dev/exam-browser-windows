using System;
using System.Collections.Generic;
using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Kontrak layanan logging lokal terstruktur.
/// </summary>
public interface ILoggingService
{
    void Log(LogLevel level, AuditEventType eventType, string message, string? details = null);
    void LogInfo(AuditEventType eventType, string message, string? details = null);
    void LogWarning(AuditEventType eventType, string message, string? details = null);
    void LogError(AuditEventType eventType, string message, string? details = null, Exception? exception = null);
    IReadOnlyList<LogEntry> GetRecentLogs(int count = 100);
    string GetLogsDirectory();
}
