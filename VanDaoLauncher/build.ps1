# VanDao Launcher - Build & Deploy Script
# Publishes framework-dependent exe and copies to game root

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "$PSScriptRoot\publish",
    [string]$GameRoot = (Resolve-Path "$PSScriptRoot\..").Path
)

Write-Host "=== Van Dao Launcher Build ===" -ForegroundColor Cyan
Write-Host ""

# Clean previous
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}

# Build
Write-Host "Building ($Configuration)..." -ForegroundColor Yellow
dotnet build "$PSScriptRoot\VanDaoLauncher.csproj" -c $Configuration
if ($LASTEXITCODE -ne 0) { Write-Host "Build FAILED!" -ForegroundColor Red; exit 1 }

# Publish
Write-Host "Publishing..." -ForegroundColor Yellow
dotnet publish "$PSScriptRoot\VanDaoLauncher.csproj" -c $Configuration -r win-x64 -o $OutputDir
if ($LASTEXITCODE -ne 0) { Write-Host "Publish FAILED!" -ForegroundColor Red; exit 1 }

# Deploy to game root
Write-Host "`n=== Deploying ===" -ForegroundColor Cyan
Get-Process -Name "VanDaoLauncher" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500
Copy-Item "$OutputDir\VanDaoLauncher.exe" "$GameRoot\VanDaoLauncher.exe" -Force

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
$s = "{0:N0} KB" -f ((Get-Item "$GameRoot\VanDaoLauncher.exe").Length / 1KB)
Write-Host "  $GameRoot\VanDaoLauncher.exe ($s)" -ForegroundColor Cyan
