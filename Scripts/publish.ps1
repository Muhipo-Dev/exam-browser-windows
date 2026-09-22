# Scripts/publish.ps1
# Publish script for Exambro-Muhipo (win-x64)
$ErrorActionPreference = "Stop"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  EXAMBRO-MUHIPO - PUBLISH PIPELINE (win-x64)" -ForegroundColor Cyan
Write-Host "  SMA Muhammadiyah 1 Ponorogo" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$projectPath = "src\Exambro-Muhipo\Exambro-Muhipo.csproj"
$publishDir = "publish\win-x64"

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Write-Host "`nPublishing Release win-x64 (Self-Contained)..." -ForegroundColor Yellow
dotnet publish $projectPath -c Release -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

# Copy default Profiles and Configuration to ensure fresh bundle
Write-Host "Copying configuration and profiles to output..." -ForegroundColor Yellow
Copy-Item "Configuration" "$publishDir\" -Recurse -Force
Copy-Item "Profiles" "$publishDir\" -Recurse -Force
Copy-Item "Assets" "$publishDir\" -Recurse -Force

# Create Logs directory in publish directory
New-Item -ItemType Directory -Force -Path "$publishDir\Logs" | Out-Null

$mainExe = "$publishDir\Exambro-Muhipo.exe"
if (-not (Test-Path $mainExe)) {
    throw "Fatal: $mainExe was not generated!"
}

Write-Host "`nPublish succeeded!" -ForegroundColor Green
Write-Host "Main Executable: $mainExe" -ForegroundColor Green
