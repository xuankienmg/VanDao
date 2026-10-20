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
if (Test-Path "$OutputDir\WebView2Loader.dll") { Copy-Item "$OutputDir\WebView2Loader.dll" "$GameRoot\WebView2Loader.dll" -Force }
if (Test-Path "$OutputDir\e_sqlite3.dll") { Copy-Item "$OutputDir\e_sqlite3.dll" "$GameRoot\e_sqlite3.dll" -Force }

if (Test-Path "$GameRoot\launcher_wwwroot") { Remove-Item "$GameRoot\launcher_wwwroot" -Recurse -Force }
if (Test-Path "$OutputDir\wwwroot") {
    Copy-Item "$OutputDir\wwwroot" "$GameRoot\launcher_wwwroot" -Recurse -Force
} else {
    Copy-Item "$PSScriptRoot\wwwroot" "$GameRoot\launcher_wwwroot" -Recurse -Force
}

if (Test-Path "$OutputDir\runtimes") {
    if (Test-Path "$GameRoot\runtimes") { Remove-Item "$GameRoot\runtimes" -Recurse -Force }
    Copy-Item "$OutputDir\runtimes" "$GameRoot\runtimes" -Recurse -Force
}

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
$s = "{0:N0} KB" -f ((Get-Item "$GameRoot\VanDaoLauncher.exe").Length / 1KB)
Write-Host "  $GameRoot\VanDaoLauncher.exe ($s)" -ForegroundColor Cyan
