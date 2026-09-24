# Scripts/package.ps1
# Script to compile Inno Setup installer for Exambro-Muhipo
$ErrorActionPreference = "Stop"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  EXAMBRO-MUHIPO - PACKAGING PIPELINE" -ForegroundColor Cyan
Write-Host "  SMA Muhammadiyah 1 Ponorogo" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

# 1. Pastikan publish win-x64 siap
$mainExe = "publish\win-x64\Exambro-Muhipo.exe"
if (-not (Test-Path $mainExe)) {
    Write-Host "Publish win-x64 belum ditemukan. Menjalankan publish.ps1..." -ForegroundColor Yellow
    & ".\Scripts\publish.ps1"
}

# 2. Cari ISCC.exe (Inno Setup Compiler)
$isccCandidates = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Users\Raza Gopo\AppData\Local\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$isccPath = $null
foreach ($c in $isccCandidates) {
    if (Test-Path $c) {
        $isccPath = $c
        break
    }
}

if (-not $isccPath) {
    $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        $isccPath = $cmd.Source
    }
}

if (-not $isccPath) {
    throw "Inno Setup Compiler (ISCC.exe) tidak ditemukan di sistem."
}

Write-Host "Menggunakan Inno Setup Compiler: $isccPath" -ForegroundColor Green

# 3. Pastikan folder Output dan Installer\Output ada
$outputDir = "Output"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
}
$installerOutputDir = "Installer\Output"
if (-not (Test-Path $installerOutputDir)) {
    New-Item -ItemType Directory -Force -Path $installerOutputDir | Out-Null
}

# 4. Jalankan kompilasi installer
$issScript = "Installer\Exambro-Muhipo-Setup.iss"
Write-Host "`nMenjalankan kompilasi installer dari $issScript..." -ForegroundColor Yellow

& $isccPath $issScript
if ($LASTEXITCODE -ne 0) {
    throw "Kompilasi installer Inno Setup gagal."
}

$latest = Get-ChildItem -Path "Output\Exambro-Muhipo-Setup-*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $latest) {
    # Cek di Installer\Output sebagai fallback
    $latest = Get-ChildItem -Path "Installer\Output\Exambro-Muhipo-Setup-*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
}

if (-not $latest) {
    throw "Installer tidak ditemukan di direktori Output."
}

$installerExe = $latest.FullName
try {
    if ($installerExe -ne (Join-Path (Get-Location) "Output\$($latest.Name)")) {
        Copy-Item -Force $installerExe "Output\$($latest.Name)" -ErrorAction SilentlyContinue
    }
    Copy-Item -Force $installerExe "Installer\Output\$($latest.Name)" -ErrorAction SilentlyContinue
    Copy-Item -Force $installerExe "$($latest.Name)" -ErrorAction SilentlyContinue
} catch {
    # Ignored if file in root is locked by explorer
}

$targetFile = "Output\$($latest.Name)"
$installerSizeMB = [Math]::Round((Get-Item $targetFile).Length / 1MB, 2)
Write-Host "`nInstaller Windows berhasil dibuat!" -ForegroundColor Green
Write-Host "Path Installer: $targetFile ($installerSizeMB MB)" -ForegroundColor Green
Write-Host "Salinan Root: $($latest.Name)" -ForegroundColor Green

