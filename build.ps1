param([switch]$Test,[switch]$PureTest,[string]$OutputPath='dist\ScreenMover')
$ErrorActionPreference = 'Stop'
if ($Test -and $PureTest) { throw 'Choose either -Test (window integration) or -PureTest (no UI)' }
Push-Location $PSScriptRoot
try {
    $releasePath = if ([IO.Path]::IsPathRooted($OutputPath)) { [IO.Path]::GetFullPath($OutputPath) } else { [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $OutputPath)) }
    dotnet build ScreenMover.slnx
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    if ($PureTest) {
        dotnet run --project Diagnostics -- --pure
        if ($LASTEXITCODE -ne 0) { throw 'Pure diagnostics failed' }
    }
    dotnet publish ScreenMover\ScreenMover.csproj -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -p:PublishReadyToRun=false -o $releasePath
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    foreach ($required in @('ScreenMover.exe','ScreenMover.pri','MainWindow.xbf','QuickWindow.xbf','Helpers\ScreenMover.DesktopHelper.exe','Helpers\VirtualDesktop-LICENSE.txt')) {
        if (-not (Test-Path (Join-Path $releasePath $required))) { throw "Missing release resource: $required" }
    }
    foreach ($document in @('README.md','CHANGELOG.md','docs\MANUAL_TEST.md','docs\ROADMAP.md')) {
        if (Test-Path $document) { Copy-Item -LiteralPath $document -Destination $releasePath -Force }
    }
    if ($Test) {
        dotnet run --project Diagnostics -- (Join-Path $releasePath 'ScreenMover.exe')
        if ($LASTEXITCODE -ne 0) { throw 'Diagnostics failed' }
    }
    Write-Host "Ready: $releasePath\ScreenMover.exe"
} finally { Pop-Location }
