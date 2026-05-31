# YtDlpGUI GitHub Deployment Checklist

This repository now includes WinUI GUI deployment hygiene for GitHub.

## 1) What is ready

- WinUI build artifacts are ignored (`.vs`, `artifacts`, `src/**/bin`, `src/**/obj`, packaging outputs).
- Line endings for GUI/.NET files are normalized via `.gitattributes` to avoid mixed-CRLF prompts.
- A dedicated GitHub Actions workflow builds the GUI solution on Windows.

## 2) Build locally before push

```powershell
dotnet restore .\YtDlpGUI.sln
dotnet build .\YtDlpGUI.sln -c Release
```

## 3) Build installer locally

```powershell
powershell -ExecutionPolicy Bypass -File .\installer\scripts\Build-Installer.ps1
```

Expected output:

- `artifacts\installer\yt-dlp-desktop-<version>-win-x64-setup.exe`

## 4) Push and verify CI

After pushing, verify the `YtDlpGUI WinUI CI` workflow is green in GitHub Actions.

## 5) Recommended release flow

1. Tag version (for example `v0.1.1`).
2. Build installer from a clean local environment.
3. Upload setup EXE to a GitHub Release.
4. Add release notes with known requirements (`yt-dlp`, `ffmpeg`, or bundled engine notes).

## License note

This repository currently uses the same license as yt-dlp: **The Unlicense** (see root `LICENSE`).
