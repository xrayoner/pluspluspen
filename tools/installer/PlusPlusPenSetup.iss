#define MyAppName "++PEN"
#define MyAppVersion "26.1"
#define MyAppPublisher "++PEN"
#define MyAppExeName "PlusPlusPen.exe"
#define MyAppSetupName "++PENSetup_26.1"
#define MyAppId "{{4E9479E6-2A6A-4BC7-9C5A-6A6C96FE8E31}"
#define PublishDir "..\..\src\PlusPlusPen\bin\Release\net8.0-windows\publish"
#define AppIcon "..\..\src\PlusPlusPen\Assets\AppIcon.ico"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\PlusPlusPen
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\..\artifacts\installer
OutputBaseFilename={#MyAppSetupName}
SetupIconFile={#AppIcon}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\++PEN"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\++PEN"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "++PEN'i çalıştır"; Flags: nowait postinstall skipifsilent
