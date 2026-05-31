#define MyAppName "yt-dlp Desktop"
#define MyAppExeName "YtDlpGUI.exe"
#define AppPublisherDefault "yt-dlp GUI Contributors"
#define AppPublisherUrlDefault "https://github.com/yt-dlp/yt-dlp"
#define AppSupportUrlDefault "https://github.com/yt-dlp/yt-dlp/issues"
#define AppUpdatesUrlDefault "https://github.com/yt-dlp/yt-dlp/releases"
#define StableUpgradeId "{{7F716A49-2AAB-486A-B7C7-E2CD55E71BB3}"

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

#ifndef InstallerVersionInfo
  #define InstallerVersionInfo AppVersion + ".0"
#endif

#ifndef Runtime
  #define Runtime "win-x64"
#endif

#ifndef SourceDir
  #define SourceDir "..\\..\\artifacts\\publish\\win-x64"
#endif

#ifndef OutputDir
  #define OutputDir "..\\..\\artifacts\\installer"
#endif

#ifndef AppPublisher
  #define AppPublisher AppPublisherDefault
#endif

#ifndef AppPublisherURL
  #define AppPublisherURL AppPublisherUrlDefault
#endif

#ifndef AppSupportURL
  #define AppSupportURL AppSupportUrlDefault
#endif

#ifndef AppUpdatesURL
  #define AppUpdatesURL AppUpdatesUrlDefault
#endif

#if Runtime == "win-x64"
  #define ArchitecturesAllowed "x64compatible"
  #define ArchitecturesInstallIn64BitMode "x64compatible"
#elif Runtime == "win-arm64"
  #define ArchitecturesAllowed "arm64"
  #define ArchitecturesInstallIn64BitMode "arm64"
#else
  #define ArchitecturesAllowed ""
  #define ArchitecturesInstallIn64BitMode ""
#endif

[Setup]
AppId={#StableUpgradeId}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppVerName={#MyAppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppPublisherURL}
AppSupportURL={#AppSupportURL}
AppUpdatesURL={#AppUpdatesURL}
AppCopyright=Copyright (c) {#AppPublisher}
DefaultDirName={autopf}\\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\\..\\LICENSE
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#OutputDir}
OutputBaseFilename=yt-dlp-desktop-{#AppVersion}-{#Runtime}-setup
SetupIconFile=..\\assets\\ytdlpgui.ico
UninstallDisplayIcon={app}\\{#MyAppExeName}
VersionInfoVersion={#InstallerVersionInfo}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed={#ArchitecturesAllowed}
ArchitecturesInstallIn64BitMode={#ArchitecturesInstallIn64BitMode}
UsePreviousAppDir=yes
UsePreviousGroup=yes
CloseApplications=yes
CloseApplicationsFilter=YtDlpGUI.exe
RestartApplications=no
ChangesAssociations=no
DisableProgramGroupPage=yes
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\\{#MyAppExeName}"
Name: "{group}\\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon; IconFilename: "{app}\\{#MyAppExeName}"

[Run]
Filename: "{app}\\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
