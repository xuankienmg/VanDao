# NguoiLuoiTuTien Launcher - Build & Deploy Script
# Publishes a compressed self-contained exe and copies to game root

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "$PSScriptRoot\publish",
    [string]$GameRoot = (Resolve-Path "$PSScriptRoot\..").Path
)

Write-Host "=== NguoiLuoiTuTien Launcher Build ===" -ForegroundColor Cyan
Write-Host ""

# Clean previous
if (Test-Path $OutputDir) {
    $resolvedOutput = (Resolve-Path -LiteralPath $OutputDir).Path
    $launcherRoot = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\') + '\'
    if (!$resolvedOutput.StartsWith($launcherRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'OutputDir must be inside the Launcher directory.' }
    Remove-Item -Recurse -Force $OutputDir
}

# Build
Write-Host "Building ($Configuration)..." -ForegroundColor Yellow
dotnet build "$PSScriptRoot\NguoiLuoiTuTienLauncher.csproj" -c $Configuration
if ($LASTEXITCODE -ne 0) { Write-Host "Build FAILED!" -ForegroundColor Red; exit 1 }

# Publish
Write-Host "Publishing..." -ForegroundColor Yellow
dotnet publish "$PSScriptRoot\NguoiLuoiTuTienLauncher.csproj" -c $Configuration -r win-x64 -o $OutputDir --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
if ($LASTEXITCODE -ne 0) { Write-Host "Publish FAILED!" -ForegroundColor Red; exit 1 }

# Deploy to game root
Write-Host "`n=== Deploying ===" -ForegroundColor Cyan
Get-Process -Name "NguoiLuoiTuTienLauncher" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500
Copy-Item "$OutputDir\NguoiLuoiTuTienLauncher.exe" "$GameRoot\NguoiLuoiTuTienLauncher.exe" -Force

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
$s = "{0:N0} KB" -f ((Get-Item "$GameRoot\NguoiLuoiTuTienLauncher.exe").Length / 1KB)
Write-Host "  $GameRoot\NguoiLuoiTuTienLauncher.exe ($s)" -ForegroundColor Cyan
