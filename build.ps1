<# CommandMan Build Script #>
param(
    [switch]$SkipInstaller,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CommandMan Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "[1/5] Cleaning previous builds..." -ForegroundColor Yellow
    Remove-Item -Path "$root\CommandMan.Shell\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$root\CommandMan.Shell\obj" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$root\CommandMan.Shell\wwwroot" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$root\output" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Cleaned!" -ForegroundColor Green
} else {
    Write-Host "[1/5] Skipping clean (use -Clean to force)" -ForegroundColor DarkGray
}

# Build Angular
Write-Host ""
Write-Host "[2/5] Building Angular frontend..." -ForegroundColor Yellow
Push-Location "$root\CommandMan.UI"
try {
    if (-not (Test-Path "node_modules")) {
        Write-Host "  Installing npm dependencies..." -ForegroundColor DarkGray
        npm install
        if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
    }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "Angular build failed" }
    Write-Host "  Angular build complete!" -ForegroundColor Green
} finally {
    Pop-Location
}

# Publish .NET
Write-Host ""
Write-Host "[3/5] Publishing .NET application..." -ForegroundColor Yellow
Push-Location "$root\CommandMan.Shell"
try {
    dotnet publish -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
    Write-Host "  .NET publish complete!" -ForegroundColor Green
} finally {
    Pop-Location
}

# Create installer
if (-not $SkipInstaller) {
    Write-Host ""
    Write-Host "[4/5] Creating installer..." -ForegroundColor Yellow
    
    $iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (-not (Test-Path $iscc)) {
        $iscc = "C:\Program Files\Inno Setup 6\ISCC.exe"
    }
    
    if (Test-Path $iscc) {
        & $iscc "$root\installer.iss"
        if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed" }
        Write-Host "  Installer created!" -ForegroundColor Green
    } else {
        Write-Host "  Inno Setup not found. Skipping installer creation." -ForegroundColor DarkYellow
        Write-Host "  Download from: https://jrsoftware.org/isdl.php" -ForegroundColor DarkGray
    }
} else {
    Write-Host ""
    Write-Host "[4/5] Skipping installer (use without -SkipInstaller to create)" -ForegroundColor DarkGray
}

# Summary
Write-Host ""
Write-Host "[5/5] Build complete!" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Output Locations:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Executable: $root\CommandMan.Shell\bin\Release\net10.0-windows\win-x64\publish\" -ForegroundColor White
if (-not $SkipInstaller -and (Test-Path "$root\output")) {
    Write-Host "  Installer:  $root\output\" -ForegroundColor White
}
Write-Host ""
