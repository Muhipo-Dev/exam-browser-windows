# Scripts/build.ps1
# Build script for Exambro-Muhipo
$ErrorActionPreference = "Stop"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  EXAMBRO-MUHIPO - BUILD PIPELINE" -ForegroundColor Cyan
Write-Host "  SMA Muhammadiyah 1 Ponorogo" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$solutionPath = "Exambro-Muhipo.sln"

Write-Host "`n[1/4] Restoring NuGet Packages..." -ForegroundColor Yellow
dotnet restore $solutionPath
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

Write-Host "`n[2/4] Building Debug Configuration..." -ForegroundColor Yellow
dotnet build $solutionPath -c Debug
if ($LASTEXITCODE -ne 0) { throw "Debug build failed." }

Write-Host "`n[3/4] Building Release Configuration..." -ForegroundColor Yellow
dotnet build $solutionPath -c Release
if ($LASTEXITCODE -ne 0) { throw "Release build failed." }

Write-Host "`n[4/4] Executing Automated Tests..." -ForegroundColor Yellow
dotnet test $solutionPath -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Automated tests failed." }

Write-Host "`nBuild and tests completed successfully!" -ForegroundColor Green
