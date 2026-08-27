#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif

[Setup]
AppId={{8D096A63-01BE-4E89-A08F-13D86E5E4976}
AppName=Reptile Desktop Pet
AppVersion={#AppVersion}
AppPublisher=cheng-yi-cc
AppPublisherURL=https://github.com/cheng-yi-cc/zhuochong
AppSupportURL=https://github.com/cheng-yi-cc/zhuochong/issues
AppUpdatesURL=https://github.com/cheng-yi-cc/zhuochong/releases
DefaultDirName={localappdata}\Programs\ReptileDesktopPet
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0.10240
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
OutputDir=..\dist
OutputBaseFilename=ReptileDesktopPet-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName=Reptile Desktop Pet
UninstallDisplayIcon={app}\ReptileDesktopPet.exe
CloseApplications=yes
RestartApplications=no

[Files]
Source: "..\dist\ReptileDesktopPet.exe"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
; Enable login startup on the first install only. Upgrades preserve the choice
; the user made later from the tray menu.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ReptileDesktopPet"; ValueData: """{app}\ReptileDesktopPet.exe"""; Flags: uninsdeletevalue; Check: IsFirstInstall
Root: HKCU; Subkey: "Software\ReptileDesktopPet"; ValueType: dword; ValueName: "Installed"; ValueData: "1"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\ReptileDesktopPet.exe"; Description: "Launch Reptile Desktop Pet"; Flags: nowait postinstall skipifsilent

[Code]
function IsFirstInstall: Boolean;
begin
  Result := not RegValueExists(HKCU, 'Software\ReptileDesktopPet', 'Installed');
end;
