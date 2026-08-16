#Requires -Version 5.1

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Repository = 'https://github.com/devmobasa/QuickOTP'

if ($env:OS -ne 'Windows_NT') {
    throw 'This installer supports Windows only.'
}

$Architecture = if ($env:PROCESSOR_ARCHITEW6432) {
    $env:PROCESSOR_ARCHITEW6432
} else {
    $env:PROCESSOR_ARCHITECTURE
}

if ($Architecture -ne 'AMD64') {
    throw 'The prebuilt release requires an x64 version of Windows.'
}

if ([string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
    throw 'LOCALAPPDATA is not available.'
}

$InstallDirectory = Join-Path $env:LOCALAPPDATA 'QuickOTP'
$BinDirectory = Join-Path $InstallDirectory 'bin'
$DownloadDirectory = Join-Path ([IO.Path]::GetTempPath()) ("QuickOTP-{0}" -f [Guid]::NewGuid())
$StageDirectory = Join-Path $DownloadDirectory 'stage'

New-Item -ItemType Directory -Force -Path $InstallDirectory, $BinDirectory, $StageDirectory | Out-Null

try {
    [Net.ServicePointManager]::SecurityProtocol =
        [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    foreach ($App in @('Popup', 'Editor')) {
        $Archive = "QuickOTP.$App-win-x64.zip"
        $ArchivePath = Join-Path $DownloadDirectory $Archive

        Write-Host "Downloading $Archive..."
        Invoke-WebRequest `
            -Uri "$Repository/releases/latest/download/$Archive" `
            -OutFile $ArchivePath `
            -UseBasicParsing
        Expand-Archive -LiteralPath $ArchivePath -DestinationPath $StageDirectory -Force

        $Executable = Join-Path $StageDirectory "QuickOTP.$App/QuickOTP.$App.exe"
        if (-not (Test-Path -LiteralPath $Executable -PathType Leaf)) {
            throw "$Archive does not contain the expected executable."
        }
    }

    foreach ($App in @('Popup', 'Editor')) {
        $SourceDirectory = Join-Path $StageDirectory "QuickOTP.$App"
        $AppDirectory = Join-Path $InstallDirectory "QuickOTP.$App"
        New-Item -ItemType Directory -Force -Path $AppDirectory | Out-Null
        Copy-Item -Path "$SourceDirectory/*" -Destination $AppDirectory -Recurse -Force
    }
} finally {
    if (Test-Path -LiteralPath $DownloadDirectory) {
        Remove-Item -LiteralPath $DownloadDirectory -Recurse -Force
    }
}

$Utf8NoBom = New-Object Text.UTF8Encoding -ArgumentList $false
$PopupExecutable = Join-Path $InstallDirectory 'QuickOTP.Popup/QuickOTP.Popup.exe'
$EditorExecutable = Join-Path $InstallDirectory 'QuickOTP.Editor/QuickOTP.Editor.exe'
[IO.File]::WriteAllText(
    (Join-Path $BinDirectory 'quickotp-popup.cmd'),
    "@echo off`r`n`"$PopupExecutable`" %*`r`n",
    $Utf8NoBom)
[IO.File]::WriteAllText(
    (Join-Path $BinDirectory 'quickotp-editor.cmd'),
    "@echo off`r`n`"$EditorExecutable`" %*`r`n",
    $Utf8NoBom)

$UserPath = [Environment]::GetEnvironmentVariable('Path', 'User')
$PathEntries = @($UserPath -split ';' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$BinDirectoryInPath = $PathEntries | Where-Object {
    $_.TrimEnd('\') -ieq $BinDirectory.TrimEnd('\')
}

if (-not $BinDirectoryInPath) {
    $NewUserPath = if ([string]::IsNullOrWhiteSpace($UserPath)) {
        $BinDirectory
    } else {
        "$($UserPath.TrimEnd(';'));$BinDirectory"
    }
    [Environment]::SetEnvironmentVariable('Path', $NewUserPath, 'User')
}

if (($env:Path -split ';') -notcontains $BinDirectory) {
    $env:Path = "$env:Path;$BinDirectory"
}

Write-Host ''
Write-Host 'QuickOTP Popup and Editor are installed.'
Write-Host 'Run: quickotp-popup'
Write-Host 'Run: quickotp-editor'
