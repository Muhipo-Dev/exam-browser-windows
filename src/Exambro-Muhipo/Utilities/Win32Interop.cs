using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ExambroMuhipo.Utilities;

/// <summary>
/// Interop Win32 API resmi untuk Low-Level Keyboard Hook guna mengimplementasikan Kiosk Mode.
/// Menangkap dan memblokir tombol-tombol sistem (Win Key, Alt+Tab, Alt+Esc, Ctrl+Esc, Alt+F4, F11, dsb)
/// selama sesi ujian aktif tanpa menggunakan teknik berbahaya/driver kernel.
/// </summary>
public static class Win32Interop
{
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    public const int WH_KEYBOARD_LL = 13;
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_KEYUP = 0x0101;
    public const int WM_SYSKEYDOWN = 0x0104;
    public const int WM_SYSKEYUP = 0x0105;

    public const int VK_TAB = 0x09;
    public const int VK_ESCAPE = 0x1B;
    public const int VK_LWIN = 0x5B;
    public const int VK_RWIN = 0x5C;
    public const int VK_APPS = 0x5D;
    public const int VK_F4 = 0x73;
    public const int VK_F11 = 0x7A;
    public const int VK_F12 = 0x7B;
    public const int VK_CONTROL = 0x11;
    public const int VK_LCONTROL = 0xA2;
    public const int VK_RCONTROL = 0xA3;
    public const int VK_MENU = 0x12; // Alt key

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    /// <summary>
    /// Memeriksa apakah tombol Control sedang ditekan.
    /// </summary>
    public static bool IsCtrlPressed()
    {
        return (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0 ||
               (GetAsyncKeyState(VK_LCONTROL) & 0x8000) != 0 ||
               (GetAsyncKeyState(VK_RCONTROL) & 0x8000) != 0;
    }

    /// <summary>
    /// Memeriksa apakah tombol Alt sedang ditekan.
    /// </summary>
    public static bool IsAltPressed(uint flags)
    {
        const uint LLKHF_ALTDOWN = 0x20;
        return (flags & LLKHF_ALTDOWN) != 0;
    }

    /// <summary>
    /// Memeriksa apakah tombol Escape (Esc) sedang ditekan.
    /// </summary>
    public static bool IsEscPressed()
    {
        return (GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0;
    }
}
