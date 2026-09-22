@echo off
title Uninstaller Exambro-Muhipo - SMA Muhammadiyah 1 Ponorogo
echo ========================================================
echo   Copot Pemasangan (Uninstall) Exambro-Muhipo
echo   SMA Muhammadiyah 1 Ponorogo
echo ========================================================
echo.
cd /d "%~dp0"
if exist "unins000.exe" (
    echo Menjalankan wizard pencopotan aplikasi...
    start "" "unins000.exe"
) else (
    echo [PERINGATAN] Berkas unins000.exe tidak ditemukan di folder ini.
    echo Silakan copot pemasangan melalui Windows Settings / Control Panel.
    pause
)
