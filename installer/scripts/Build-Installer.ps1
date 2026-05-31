[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [ValidateSet("win-x64", "win-x86", "win-arm64")]
    [string]$Runtime = "win-x64",
    [string]$Version,
    [string]$ProjectPath = "src/YtDlpGUI/YtDlpGUI.csproj",
    [string]$OutputRoot = "artifacts",
    [string]$InnoCompilerPath,
    [switch]$SkipPublish,
    [switch]$SkipInstaller,
    [switch]$SkipEngineBundle,
    [switch]$SkipFfmpegBundle
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$projectFullPath = Join-Path $repoRoot $ProjectPath
$issFile = Join-Path $repoRoot "installer/inno/YtDlpGUI.iss"
$publishDir = Join-Path $repoRoot "$OutputRoot/publish/$Runtime"
$installerDir = Join-Path $repoRoot "$OutputRoot/installer"

if (-not (Test-Path $projectFullPath)) {
    throw "Project file not found: $projectFullPath"
}

if (-not $Version) {
    [xml]$projectXml = Get-Content $projectFullPath
    $Version = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($Version)) {
        throw "Could not infer <Version> from $projectFullPath. Pass -Version explicitly."
    }
}

Write-Host "Repository root: $repoRoot"
Write-Host "Building version: $Version"
Write-Host "Runtime: $Runtime"

if (-not $SkipPublish) {
    if (Test-Path $publishDir) {
        Remove-Item $publishDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

    $publishArgs = @(
        "publish", $projectFullPath,
        "-c", $Configuration,
        "-r", $Runtime,
        "--self-contained", "true",
        "-p:PublishSingleFile=false",
        "-p:PublishReadyToRun=true",
        "-p:PublishTrimmed=false",
        "-p:WindowsAppSDKSelfContained=true",
        "-p:Version=$Version",
        "-o", $publishDir
    )

    Write-Host "Running: dotnet $($publishArgs -join ' ')"
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}

if (-not (Test-Path (Join-Path $publishDir "YtDlpGUI.exe"))) {
    throw "Publish output missing YtDlpGUI.exe in $publishDir"
}

$ytDlpExePath = Join-Path $publishDir "yt-dlp.exe"
if (-not $SkipEngineBundle -and -not (Test-Path $ytDlpExePath)) {
    $ytDlpDownloadUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
    Write-Host "Bundling yt-dlp engine from $ytDlpDownloadUrl"
    try {
        Invoke-WebRequest -Uri $ytDlpDownloadUrl -OutFile $ytDlpExePath -UseBasicParsing
    }
    catch {
        throw "Failed to download yt-dlp.exe for bundling. Install aborted. Error: $($_.Exception.Message)"
    }
}

$ffmpegExePath = Join-Path $publishDir "ffmpeg.exe"
$ffprobeExePath = Join-Path $publishDir "ffprobe.exe"
if (-not $SkipFfmpegBundle -and ((-not (Test-Path $ffmpegExePath)) -or (-not (Test-Path $ffprobeExePath)))) {
    $ffmpegZipUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
    $ffmpegTempRoot = Join-Path $env:TEMP ("ytdlpgui-ffmpeg-" + [Guid]::NewGuid().ToString("N"))
    $ffmpegZipPath = Join-Path $ffmpegTempRoot "ffmpeg.zip"
    $ffmpegExtractPath = Join-Path $ffmpegTempRoot "extract"

    try {
        New-Item -ItemType Directory -Path $ffmpegExtractPath -Force | Out-Null
        Write-Host "Bundling ffmpeg binaries from $ffmpegZipUrl"
        Invoke-WebRequest -Uri $ffmpegZipUrl -OutFile $ffmpegZipPath -UseBasicParsing
        Expand-Archive -Path $ffmpegZipPath -DestinationPath $ffmpegExtractPath -Force

        $ffmpegSource = Get-ChildItem -Path $ffmpegExtractPath -Filter "ffmpeg.exe" -Recurse | Select-Object -First 1
        $ffprobeSource = Get-ChildItem -Path $ffmpegExtractPath -Filter "ffprobe.exe" -Recurse | Select-Object -First 1

        if ($null -eq $ffmpegSource -or $null -eq $ffprobeSource) {
            throw "Downloaded archive does not contain ffmpeg.exe and ffprobe.exe."
        }

        Copy-Item -Path $ffmpegSource.FullName -Destination $ffmpegExePath -Force
        Copy-Item -Path $ffprobeSource.FullName -Destination $ffprobeExePath -Force
    }
    catch {
        throw "Failed to bundle ffmpeg binaries. Install aborted. Error: $($_.Exception.Message)"
    }
    finally {
        if (Test-Path $ffmpegTempRoot) {
            Remove-Item $ffmpegTempRoot -Recurse -Force
        }
    }
}

if ($SkipInstaller) {
    Write-Host "Publish completed. Installer step skipped by -SkipInstaller."
    Write-Host "Publish output: $publishDir"
    exit 0
}

if (-not $InnoCompilerPath) {
    $candidatePaths = @(
        (Join-Path $env:LOCALAPPDATA "Programs\\Inno Setup 6\\ISCC.exe"),
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )

    $InnoCompilerPath = ($candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1)
}

if (-not $InnoCompilerPath -or -not (Test-Path $InnoCompilerPath)) {
    throw "Inno Setup compiler not found. Install Inno Setup 6 and pass -InnoCompilerPath if needed."
}

New-Item -ItemType Directory -Path $installerDir -Force | Out-Null

$isccArgs = @(
    "/DAppVersion=$Version",
    "/DRuntime=$Runtime",
    "/DSourceDir=`"$publishDir`"",
    "/DOutputDir=`"$installerDir`"",
    $issFile
)

Write-Host "Running: $InnoCompilerPath $($isccArgs -join ' ')"
& $InnoCompilerPath @isccArgs
if ($LASTEXITCODE -ne 0) {
    throw "Installer compilation failed with exit code $LASTEXITCODE"
}

$artifacts = Get-ChildItem -Path $installerDir -Filter "*.exe" | Sort-Object LastWriteTime -Descending
if ($artifacts.Count -eq 0) {
    throw "Installer build completed but no .exe artifact was found in $installerDir"
}

Write-Host "Installer artifacts:"
$artifacts | ForEach-Object { Write-Host " - $($_.FullName)" }
