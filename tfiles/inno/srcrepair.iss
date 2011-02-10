; ������ ��������� (�������) ��������� SRC Repair.
; 
; Copyright 2011 EasyCoding Team (ECTeam).
; Copyright 2005 - 2011 EasyCoding Team.
; 
; ��������: GPL v3 (��. ���� GPL.txt).
;  
; ����������� ������������ ���� ���� ��� ������������� �����
; ��������, �������� �� GNU GPL ������ 3 � � ��� �����������.
; 
; ����������� ���� EasyCoding Team: http://www.easycoding.org/
; ����������� �������� �������: http://www.easycoding.org/projects/srcrepair
; 
; ����� ��������� ��������� � ��������� � readme.txt, � �������� - � GPL.txt.

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{77A71DAB-56AA-4F33-BDE8-F00798468B9D}
AppName=SRC Repair
AppVerName=SRC Repair
AppPublisher=EasyCoding Team
AppPublisherURL=http://www.easycoding.org/projects/srcrepair
; AppVersion ������������ � ���������/�������� �������� � �������������� ����������.
AppVersion=1.5.0.248
AppSupportURL=http://www.easycoding.org/projects/srcrepair
AppUpdatesURL=http://www.easycoding.org/projects/srcrepair
DefaultDirName={pf}\SRC Repair
DefaultGroupName=SRC Repair
AllowNoIcons=yes
LicenseFile=E:\VSBuilds\GPL.txt
;InfoBeforeFile=E:\VSBuilds\readme.txt
OutputDir=E:\VSBuilds
OutputBaseFilename=srcrepair_15_final
;OutputBaseFilename=srcrepair_beta_236
SetupIconFile=E:\SVN\srcrepair\srcrepair\TF2Repair.ico
;UninstallDisplayIcon={app}\MyProg.exe,1
Compression=lzma2
SolidCompression=yes
; "ArchitecturesInstallIn64BitMode=x64" requests that the install be
; done in "64-bit mode" on x64, meaning it should use the native
; 64-bit Program Files directory and the 64-bit view of the registry.
; On all other architectures it will install in "32-bit mode".
ArchitecturesInstallIn64BitMode=x64

; ��� ��������� ������, ������� ����� ��������� � �������� �����������
VersionInfoVersion=1.5.0.248
VersionInfoDescription=SRC Repair Setup
VersionInfoCopyright=(c) 2005-2011 EasyCoding Team. All rights reserved.
VersionInfoCompany=EasyCoding Team

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl,en-US.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl,ru-RU.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
;Name: "copylicence"; Description: "����������� ������������ ���������� � ����� SRC Repair"; GroupDescription: "�������������� �����������:"
;Name: "copyreadme"; Description: "����������� ���� ReadMe � ����� SRC Repair"; GroupDescription: "�������������� �����������:"
Name: "inst7z"; Description: "{cm:InstLZMAPlugin}"; GroupDescription: "{cm:AdvFeatGroupDesc}"
Name: "betashortuts"; Description: "{cm:InstCreateLocShcuts}"; GroupDescription: "{cm:AdvFeatGroupDesc}"

[Files]
; ������������� readme � ���� ��������...
Source: "E:\VSBuilds\GPL.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "E:\VSBuilds\readme.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "E:\VSBuilds\changelog.txt"; DestDir: "{app}"; Flags: ignoreversion
; �������� �������� ����...
Source: "E:\VSBuilds\vitaly_public.asc"; DestDir: "{app}"; Flags: ignoreversion
; ������������� 32-������ ������...
Source: "E:\VSBuilds\srcrepair.exe"; DestDir: "{app}"; Flags: ignoreversion; Check: not Is64BitInstallMode
Source: "E:\VSBuilds\srcrepair.exe.sig"; DestDir: "{app}"; Flags: ignoreversion; Check: not Is64BitInstallMode
Source: "E:\VSBuilds\ru\*"; DestDir: "{app}\ru\"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not Is64BitInstallMode
; ������������� 64-������ ������...
Source: "E:\VSBuilds\x64\srcrepair.exe"; DestDir: "{app}"; Flags: ignoreversion; Check: Is64BitInstallMode
Source: "E:\VSBuilds\x64\srcrepair.exe.sig"; DestDir: "{app}"; Flags: ignoreversion; Check: Is64BitInstallMode
Source: "E:\VSBuilds\x64\ru\*"; DestDir: "{app}\ru\"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: Is64BitInstallMode
; ������������� ��������� �����...
Source: "E:\VSBuilds\cfgs\*"; DestDir: "{app}\cfgs\"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "E:\VSBuilds\7z\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Tasks: inst7z
Source: "E:\VSBuilds\nfx\*"; DestDir: "{app}\nfx\"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
; ������ ����� ��� ����������...
Name: "{group}\SRC Repair"; Filename: "{app}\srcrepair.exe"
; ������ ������ ��� ������ � ������������ ����������� � ReadMe...
Name: "{group}\{cm:ShcLocTexts}\{cm:ShcLicenseAgrr}"; Filename: "{app}\GPL.txt"
Name: "{group}\{cm:ShcLocTexts}\{cm:ShcReadme}"; Filename: "{app}\readme.txt"
Name: "{group}\{cm:ShcLocTexts}\{cm:ShcChlog}"; Filename: "{app}\changelog.txt"
; ������ ������ ��� ������� �������������� ������ (������ ���� ������������ ������ ���� �����)...
Name: "{group}\{cm:ShcLocFldr}\SRC Repair ({cm:ShcMLnRU})"; Filename: "{app}\srcrepair.exe"; Parameters: "/russian"; Tasks: betashortuts
Name: "{group}\{cm:ShcLocFldr}\SRC Repair ({cm:ShcMLnEN})"; Filename: "{app}\srcrepair.exe"; Parameters: "/english"; Tasks: betashortuts
; ������ ����������� ������ ��� ������� � ��������...
Name: "{group}\{cm:ShcLocTexts}\{cm:ProgramOnTheWeb,SRC Repair}"; Filename: "http://www.easycoding.org/projects/srcrepair"
Name: "{group}\{cm:UninstallProgram,SRC Repair}"; Filename: "{uninstallexe}"
; ������ ����� �� ������� ����� (���� �������)...
Name: "{commondesktop}\SRC Repair"; Filename: "{app}\srcrepair.exe"; Tasks: desktopicon
; ������ ����� �� ������ �������� ������� (���� �������)...
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\SRC Repair"; Filename: "{app}\srcrepair.exe"; Tasks: quicklaunchicon
; ������ ����� ��� ����������� ����� Microsoft .NET Framework 4...
Name: "{group}\{cm:ShcNETFx}"; Filename: "{app}\nfx\dotNetFx40_Full_setup.exe"

[Registry]
; ����� ������� ��������� ��������� ��� �������� ������������...
Root: HKCU; Subkey: "Software\SRC Repair"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\SRC Repair"; ValueType: dword; ValueName: "ConfirmExit"; ValueData: "1"
Root: HKCU; Subkey: "Software\SRC Repair"; ValueType: dword; ValueName: "ShowSinglePlayer"; ValueData: "1"
Root: HKCU; Subkey: "Software\SRC Repair"; ValueType: dword; ValueName: "SortGameList"; ValueData: "1"

[Run]
Filename: "{app}\nfx\dotNetFx40_Full_setup.exe"; Description: "{cm:ShcNETFx}"; Flags: nowait postinstall skipifsilent unchecked
Filename: "{app}\srcrepair.exe"; Description: "{cm:LaunchProgram,SRC Repair}"; Flags: nowait postinstall skipifsilent











