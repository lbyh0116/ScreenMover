param([switch]$Test)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet build ScreenMover.slnx
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    dotnet publish ScreenMover\ScreenMover.csproj -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -p:PublishReadyToRun=false -o dist\ScreenMover
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    foreach ($required in @('ScreenMover.exe','ScreenMover.pri','MainWindow.xbf','QuickWindow.xbf','Helpers\ScreenMover.DesktopHelper.exe','Helpers\VirtualDesktop-LICENSE.txt')) {
        if (-not (Test-Path (Join-Path 'dist\ScreenMover' $required))) { throw "Missing release resource: $required" }
    }
    if ($Test) {
        dotnet run --project Diagnostics -- dist\ScreenMover\ScreenMover.exe
        if ($LASTEXITCODE -ne 0) { throw 'Diagnostics failed' }
    }
    Write-Host 'Ready: dist\ScreenMover\ScreenMover.exe'
} finally { Pop-Location }
