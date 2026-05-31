# Windows Installer and Distribution

This project ships a **conventional Windows installer EXE** built with **Inno Setup 6**.

## Why this strategy

- Provides the familiar installer UX expected by typical Windows users.
- Supports install-directory selection, Start Menu shortcuts, optional Desktop shortcut, and uninstall registration.
- Produces a single distributable installer EXE.
- Keeps release engineering simple and repeatable for open-source distribution.

## Packaging architecture

1. Publish WinUI 3 app with all dependencies:
   - self-contained runtime
   - Windows App SDK self-contained
2. Package publish output with Inno Setup into installer EXE.

## Prerequisites

- .NET SDK 8+
- Inno Setup 6 (`ISCC.exe`)

## Build installer (recommended)

From repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\installer\scripts\Build-Installer.ps1 -Configuration Release -Runtime win-x64
```

Optional arguments:

- `-Version 0.1.0`
- `-InnoCompilerPath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"`
- `-SkipInstaller` (publish only)
- `-SkipPublish` (installer only, using existing publish output)

## Manual steps

1. Publish app:

```powershell
dotnet publish .\src\YtDlpGUI\YtDlpGUI.csproj -c Release -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true -p:PublishReadyToRun=true -p:PublishTrimmed=false -o .\artifacts\publish\win-x64
```

2. Compile installer:

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DAppVersion=0.1.0 /DRuntime=win-x64 /DSourceDir="D:\Coding 2.0\yt-dlp-master\artifacts\publish\win-x64" /DOutputDir="D:\Coding 2.0\yt-dlp-master\artifacts\installer" .\installer\inno\YtDlpGUI.iss
```

## Output artifacts

- Publish folder: `artifacts\publish\<runtime>`
- Installer EXE: `artifacts\installer\yt-dlp-desktop-<version>-<runtime>-setup.exe`

## Upgrade and uninstall behavior

- Stable installer `AppId` is used for all future versions.
- Installing a newer version upgrades in place and preserves install directory preference.
- Uninstall entry is registered in Windows Apps & Features automatically by Inno Setup.
- Uninstall removes installed application files and shortcuts.

## Installer file

- Inno script: `installer\inno\YtDlpGUI.iss`

Key UX features implemented:

- install location selection
- Start Menu shortcut
- optional Desktop shortcut task
- post-install launch option
- uninstall shortcut + system uninstall entry
- setup icon + uninstall display icon
