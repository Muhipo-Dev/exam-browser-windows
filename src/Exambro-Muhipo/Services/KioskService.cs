using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using ExambroMuhipo.Models;
using ExambroMuhipo.Utilities;

namespace ExambroMuhipo.Services;

/// <summary>
/// Implementasi resmi Kiosk Mode menggunakan User32 API (Low-Level Keyboard Hook)
/// dan kontrol window WPF tanpa menyentuh kernel/driver.
/// </summary>
public class KioskService : IKioskService
{
    private readonly ILoggingService _logger;
    private IntPtr _hookId = IntPtr.Zero;
    private Win32Interop.LowLevelKeyboardProc? _proc;
    private bool _isKioskActive;
    private ExamProfile? _currentProfile;

    private WindowStyle _savedStyle = WindowStyle.SingleBorderWindow;
    private ResizeMode _savedResize = ResizeMode.CanResize;
    private WindowState _savedState = WindowState.Normal;
    private bool _savedTopmost = false;

    public bool IsKioskActive => _isKioskActive;
    public event Action? AltF4Pressed;

    public KioskService(ILoggingService logger)
    {
        _logger = logger;
    }

    public void EnableKiosk(Window window, ExamProfile profile)
    {
        if (_isKioskActive)
            return;

        _currentProfile = profile;
        _isKioskActive = true;

        // 1. Simpan konfigurasi window asli dan ubah ke Kiosk Fullscreen
        Application.Current.Dispatcher.Invoke(() =>
        {
            _savedStyle = window.WindowStyle;
            _savedResize = window.ResizeMode;
            _savedState = window.WindowState;
            _savedTopmost = window.Topmost;

            if (profile.RequireFullscreen || profile.FullScreen)
            {
                window.WindowStyle = WindowStyle.None;
                window.ResizeMode = ResizeMode.NoResize;
                window.WindowState = WindowState.Normal; // reset first for proper borderless maximize
                window.WindowState = WindowState.Maximized;
            }

            if (profile.KioskMode)
            {
                window.Topmost = true;
            }
        });

        // 2. Pasang Low-Level Keyboard Hook jika KioskMode diaktifkan
        if (profile.KioskMode)
        {
            InstallKeyboardHook();
        }

        _logger.LogInfo(AuditEventType.ExamStarted, "Kiosk Mode dan penguncian sistem berhasil diaktifkan.", $"Profil: {profile.ExamName}");
    }

    public void DisableKiosk(Window window)
    {
        if (!_isKioskActive)
            return;

        // 1. Lepas hook keyboard
        UninstallKeyboardHook();

        // 2. Kembalikan kondisi window
        Application.Current.Dispatcher.Invoke(() =>
        {
            window.Topmost = false;
            window.WindowStyle = _savedStyle;
            window.ResizeMode = _savedResize;
            window.WindowState = _savedState;
        });

        _isKioskActive = false;
        _currentProfile = null;
        _logger.LogInfo(AuditEventType.ExamCompleted, "Kiosk Mode dinonaktifkan. Window dikembalikan ke mode normal.");
    }

    private void InstallKeyboardHook()
    {
        if (_hookId != IntPtr.Zero)
            return;

        _proc = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        IntPtr hMod = Win32Interop.GetModuleHandle(curModule?.ModuleName);
        _hookId = Win32Interop.SetWindowsHookEx(Win32Interop.WH_KEYBOARD_LL, _proc, hMod, 0);

        if (_hookId == IntPtr.Zero)
        {
            int err = Marshal.GetLastWin32Error();
            _logger.LogError(AuditEventType.ApplicationError, $"Gagal memasang low-level keyboard hook (Win32 Error: {err}).");
        }
    }

    private void UninstallKeyboardHook()
    {
        if (_hookId != IntPtr.Zero)
        {
            Win32Interop.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _proc = null;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var kbd = Marshal.PtrToStructure<Win32Interop.KBDLLHOOKSTRUCT>(lParam);
            uint vk = kbd.vkCode;
            bool isAlt = Win32Interop.IsAltPressed(kbd.flags);
            bool isCtrl = Win32Interop.IsCtrlPressed();

            // 1. Tombol Windows (LWIN, RWIN, APPS)
            if (vk == Win32Interop.VK_LWIN || vk == Win32Interop.VK_RWIN || vk == Win32Interop.VK_APPS)
            {
                return (IntPtr)1; // Blokir
            }

            // 2. Alt+Tab, Alt+Esc
            if (isAlt && (vk == Win32Interop.VK_TAB || vk == Win32Interop.VK_ESCAPE))
            {
                return (IntPtr)1; // Blokir
            }

            // Shortcut Alt+F4 untuk keluar dari aplikasi (dikonfirmasi otorisasi pengawas)
            if (isAlt && vk == Win32Interop.VK_F4)
            {
                if (wParam == (IntPtr)Win32Interop.WM_SYSKEYDOWN || wParam == (IntPtr)Win32Interop.WM_KEYDOWN)
                {
                    Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        AltF4Pressed?.Invoke();
                    }));
                }
                return (IntPtr)1; // Blokir default OS close agar alur konfirmasi dan pembersihan berjalan tertib
            }

            // 3. Ctrl+Esc
            if (isCtrl && vk == Win32Interop.VK_ESCAPE)
            {
                return (IntPtr)1; // Blokir
            }

            // 4. F11 (Fullscreen toggle ilegal)
            if (vk == Win32Interop.VK_F11)
            {
                return (IntPtr)1; // Blokir
            }

            // 5. F12 (DevTools shortcut)
            if (vk == Win32Interop.VK_F12 && (_currentProfile?.DisableDevTools ?? true))
            {
                return (IntPtr)1; // Blokir
            }

            // 6. Browser Shortcuts saat tombol Ctrl ditekan
            if (isCtrl)
            {
                switch (vk)
                {
                    case 0x4E: // N (New Window)
                    case 0x54: // T (New Tab)
                    case 0x57: // W (Close Tab)
                    case 0x48: // H (History)
                    case 0x4A: // J (Downloads)
                    case 0x55: // U (View Source)
                    case 0x50: // P (Print - jika printing diblokir)
                        if (vk == 0x50 && (_currentProfile?.AllowPrinting ?? false))
                            break;
                        return (IntPtr)1;

                    case 0x4F: // O (Open file)
                    case 0x53: // S (Save as)
                        return (IntPtr)1;

                    case 0x43: // C (Copy)
                        if (!(_currentProfile?.AllowCopy ?? true) || !(_currentProfile?.AllowClipboard ?? true))
                            return (IntPtr)1;
                        break;

                    case 0x56: // V (Paste)
                        if (!(_currentProfile?.AllowPaste ?? true) || !(_currentProfile?.AllowClipboard ?? true))
                            return (IntPtr)1;
                        break;
                }
            }
        }

        return Win32Interop.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}
