; Inno Setup script for the Commit Dock Windows installer.
; Build it with build/scripts/package.win-installer.ps1, which publishes the app first and passes
; AppVersion, SourceDir, OutputDir and Arch on the command line.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish-win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\installer"
#endif
#ifndef Arch
  #define Arch "x64"
#endif

#define AppName "Commit Dock"
#define AppExe "CommitDock.exe"
#define AppPublisher "Commit Dock"
#define AppUrl "https://github.com/mzaeemnasir/Commit-Dock"

[Setup]
AppId={{7B4E3C2A-6F1D-4E8B-9C5A-2D3F4B6A8E10}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Install for the current user without admin rights; the user may switch to all users.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed={#Arch}compatible
ArchitecturesInstallIn64BitMode={#Arch}compatible
MinVersion=10.0
LicenseFile=..\..\LICENSE
SetupIconFile=..\..\src\App.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
OutputDir={#OutputDir}
OutputBaseFilename=CommitDock-Setup-{#AppVersion}-{#Arch}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
ChangesAssociations=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// Commit Dock drives the git command line, so warn (without blocking) when git is not on PATH.
function IsGitInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec(ExpandConstant('{cmd}'), '/C git --version', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not WizardSilent() and not IsGitInstalled() then
    MsgBox('Git was not found on this computer.' + #13#10 + #13#10 +
      'Commit Dock needs Git for Windows to work. You can install it from https://git-scm.com/download/win ' +
      'before or after installing Commit Dock.', mbInformation, MB_OK);
end;
