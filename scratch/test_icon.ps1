Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public class Win32IconTester {
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    public const uint IMAGE_ICON = 1;
    public const uint LR_LOADFROMFILE = 0x0010;

    public static void TestLoad(string path, int size) {
        IntPtr hIcon = LoadImage(IntPtr.Zero, path, IMAGE_ICON, size, size, LR_LOADFROMFILE);
        int err = Marshal.GetLastWin32Error();
        Console.WriteLine("Size " + size + ": hIcon=" + hIcon + ", lastError=" + err);
        if (hIcon != IntPtr.Zero) {
            DestroyIcon(hIcon);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
"@

[Win32IconTester]::TestLoad('c:\exambro-windows\Assets\AppIcon.ico', 16)
[Win32IconTester]::TestLoad('c:\exambro-windows\Assets\AppIcon.ico', 32)
[Win32IconTester]::TestLoad('c:\exambro-windows\Assets\AppIcon.ico', 48)
[Win32IconTester]::TestLoad('c:\exambro-windows\Assets\AppIcon.ico', 256)
