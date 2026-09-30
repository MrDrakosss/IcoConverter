; ICO Konverter telepítő (Inno Setup 6)
; Fordítás: installer\build-installer.ps1  – előbb publikálja az appot, majd ezt a szkriptet fordítja.

#define AppName "ICO Konverter"
#define AppPublisher "Shelter Team Studio"
#define AppExeName "IcoConverter.exe"
#define PublishDir "..\bin\Release\net10.0\win-x64\publish"

; A verzió egyetlen helyen él: az IcoConverter.csproj <Version> eleme, innen az exe-ből olvassuk ki.
#define AppVersion GetStringFileInfo(AddBackslash(SourcePath) + PublishDir + "\" + AppExeName, "ProductVersion")
#if Pos("+", AppVersion) > 0
  #define AppVersion Copy(AppVersion, 1, Pos("+", AppVersion) - 1)
#endif

[Setup]
; Az AppId azonosítja a programot frissítéskor és eltávolításkor – ne változtasd meg.
AppId={{75DE20B7-0FB9-4D1C-979A-C20DBF46790D}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppCopyright=© 2026 {#AppPublisher}
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoProductName={#AppName}
VersionInfoDescription={#AppName} telepítő
DefaultDirName={autopf}\{#AppPublisher}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
SetupIconFile=..\Assets\logo.ico
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Alapból csak az aktuális felhasználónak telepít (nem kell rendszergazdai jog),
; de a telepítő elején választható a minden felhasználónak szóló telepítés is.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
CloseApplications=yes
OutputDir=Output
OutputBaseFilename=IcoKonverter-Setup-{#AppVersion}

[Languages]
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
