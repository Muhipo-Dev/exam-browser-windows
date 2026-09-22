; Installer/Exambro-Muhipo-Setup.iss
; Script Inno Setup untuk Exambro-Muhipo
; SMA Muhammadiyah 1 Ponorogo
; Mendukung Windows 7 SP1, Windows 8/8.1, Windows 10, dan Windows 11 (Dual-Engine: WebView2 & Native WebBrowser)

#define MyAppName "Exambro-Muhipo"
#define MyAppVersion "1.3.2-beta"
#define MyAppPublisher "Muhipo Dev"
#define MyAppURL "https://muhipo.sch.id"
#define MyAppExeName "Exambro-Muhipo.exe"

[Setup]
AppId={{C82B1490-95A2-4FA3-9654-8B8DC49D79F1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=Exambro-Muhipo-Setup-v{#MyAppVersion}
OutputDir=..\Output
SetupIconFile=..\Assets\AppIcon.ico
UninstallDisplayIcon={app}\Assets\AppIcon.ico
UninstallDisplayName={#MyAppName} - Muhipo Dev
CreateUninstallRegKey=yes
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=6.1sp1
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
ChangesAssociations=yes
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "indonesian"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
; Konfigurasi IE11 standard emulation mode & performance keys untuk Exambro-Muhipo agar rendering WebBrowser bawaan Windows 7 mulus
Root: HKCU; Subkey: "Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"; ValueType: dword; ValueName: "{#MyAppExeName}"; ValueData: 11001; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_GPU_RENDERING"; ValueType: dword; ValueName: "{#MyAppExeName}"; ValueData: 1; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_NATIVE_XMLHTTP"; ValueType: dword; ValueName: "{#MyAppExeName}"; ValueData: 1; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_AJAX_CONNECTIONPOLICY"; ValueType: dword; ValueName: "{#MyAppExeName}"; ValueData: 1; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION"; ValueType: dword; ValueName: "{#MyAppExeName}"; ValueData: 1; Flags: uninsdeletevalue

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Copot Pemasangan (Uninstall) {#MyAppName}"; Filename: "{uninstallexe}"; IconFilename: "{app}\Assets\AppIcon.ico"
Name: "{app}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"; IconFilename: "{app}\Assets\AppIcon.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Logs"
Type: filesandordirs; Name: "{localappdata}\Exambro-Muhipo"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure SHChangeNotify(wEventId: LongInt; uFlags: Cardinal; dwItem1, dwItem2: Cardinal);
  external 'SHChangeNotify@shell32.dll stdcall';

const
  SHCNE_ASSOCCHANGED = $08000000;
  SHCNF_FLUSH = $1000;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    try
      SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, 0, 0);
    except
      // Fallback
    end;
  end;
end;
