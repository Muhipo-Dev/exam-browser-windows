Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Drawing;

public class Win32RenderCheck {
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    public const uint IMAGE_ICON = 1;
    public const uint LR_LOADFROMFILE = 0x0010;

    public static void SaveRender(string icoPath, int sz, string outPng) {
        IntPtr h = LoadImage(IntPtr.Zero, icoPath, IMAGE_ICON, sz, sz, LR_LOADFROMFILE);
        if (h != IntPtr.Zero) {
            Icon ico = Icon.FromHandle(h);
            Bitmap bmp = ico.ToBitmap();
            bmp.Save(outPng);
            DestroyIcon(h);
        }
    }
}
"@ -ReferencedAssemblies System.Drawing

[Win32RenderCheck]::SaveRender('c:\exambro-windows\Assets\AppIcon.ico', 48, 'c:\exambro-windows\Assets\rendered_48x48.png')
[Win32RenderCheck]::SaveRender('c:\exambro-windows\Assets\AppIcon.ico', 32, 'c:\exambro-windows\Assets\rendered_32x32.png')
Write-Host "Rendered 48x48 and 32x32 PNGs"
