using System;
using System.Threading.Tasks;
using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Kontrak manajemen siklus hidup sesi ujian dan crash recovery.
/// </summary>
public interface IExamSessionService
{
    ExamSessionStatus Status { get; }
    ExamProfile? CurrentProfile { get; }
    TimeSpan RemainingTime { get; }
    DateTime? StartTime { get; }

    event Action<ExamSessionStatus>? StatusChanged;
    event Action<TimeSpan>? TimeRemainingUpdated;
    event Action? SessionTimedOut;

    Task StartSessionAsync(ExamProfile profile);
    Task<bool> RequestExitSessionAsync(string adminPassword);
    Task ForceEndSessionAsync();
    SessionRecoveryInfo? CheckUnfinishedSession();
    void ClearRecoveryMarker();
    void MarkHeartbeat();
}
