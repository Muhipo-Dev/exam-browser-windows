using System;
using System.Windows;
using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Kontrak layanan Kiosk Mode untuk mengunci window dan mengintersepsi tombol sistem.
/// </summary>
public interface IKioskService
{
    bool IsKioskActive { get; }
    event Action? AltF4Pressed;
    void EnableKiosk(Window window, ExamProfile profile);
    void DisableKiosk(Window window);
}
