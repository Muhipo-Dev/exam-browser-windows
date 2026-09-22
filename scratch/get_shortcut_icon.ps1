Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Drawing;

public class ShellIconHelper {
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFILEINFO {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    public const uint SHGFI_ICON = 0x000000100;
    public const uint SHGFI_LARGEICON = 0x000000000;

    public static void SaveIcon(string path, string outPath) {
        SHFILEINFO shinfo = new SHFILEINFO();
        IntPtr hImg = SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);
        if (shinfo.hIcon != IntPtr.Zero) {
            Icon icon = Icon.FromHandle(shinfo.hIcon);
            Bitmap bmp = icon.ToBitmap();
            bmp.Save(outPath);
        }
    }
}
"@ -ReferencedAssemblies System.Drawing

[ShellIconHelper]::SaveIcon('C:\Users\LENOVO\Desktop\Exambro-Muhipo.lnk', 'c:\exambro-windows\Assets\shortcut_actual.png')
Write-Host "Done saving shortcut_actual.png"
