$targetDir = "C:\Users\LENOVO\AppData\Local\Programs\Exambro-Muhipo"
if (Test-Path $targetDir) {
    Write-Host "Updating local installed application files in $targetDir..."
    Copy-Item "c:\exambro-windows\publish\win-x64\Exambro-Muhipo.exe" "$targetDir\Exambro-Muhipo.exe" -Force
    Copy-Item "c:\exambro-windows\publish\win-x64\Exambro-Muhipo.dll" "$targetDir\Exambro-Muhipo.dll" -Force
    Copy-Item "c:\exambro-windows\publish\win-x64\Assets\*" "$targetDir\Assets\" -Recurse -Force
    Write-Host "Installed files updated successfully."
}

# Update Desktop Shortcut
$desktopPath = [System.Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktopPath "Exambro-Muhipo.lnk"

$sh = New-Object -ComObject WScript.Shell
$lnk = $sh.CreateShortcut($shortcutPath)
$lnk.TargetPath = "$targetDir\Exambro-Muhipo.exe"
$lnk.WorkingDirectory = $targetDir
$lnk.Description = "Exambro-Muhipo - SMA Muhammadiyah 1 Ponorogo"
# Point directly to exe to extract embedded icon or to AppIcon.ico
$lnk.IconLocation = "$targetDir\Exambro-Muhipo.exe,0"
$lnk.Save()
Write-Host "Desktop shortcut updated to point directly to $targetDir\Exambro-Muhipo.exe,0"

# Also update Start Menu shortcut
$startMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Exambro-Muhipo\Exambro-Muhipo.lnk"
if (Test-Path (Split-Path $startMenuPath)) {
    $lnkSm = $sh.CreateShortcut($startMenuPath)
    $lnkSm.TargetPath = "$targetDir\Exambro-Muhipo.exe"
    $lnkSm.WorkingDirectory = $targetDir
    $lnkSm.Description = "Exambro-Muhipo - SMA Muhammadiyah 1 Ponorogo"
    $lnkSm.IconLocation = "$targetDir\Exambro-Muhipo.exe,0"
    $lnkSm.Save()
    Write-Host "Start menu shortcut updated."
}

# Flush shell icon cache and notify Windows Explorer
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public class ShellNotify {
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    public const int SHCNE_ASSOCCHANGED = 0x08000000;
    public const uint SHCNF_FLUSH = 0x1000;

    public static void Flush() {
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
    }
}
"@

[ShellNotify]::Flush()
Write-Host "Sent SHChangeNotify to Windows Explorer to flush icon cache!"
