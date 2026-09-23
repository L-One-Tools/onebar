#define MyAppName "OneBar"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "L-One"
#define MyAppExeName "OneBar.exe"

[Setup]
AppId={{6E8412B5-91E9-4F52-A4A8-12D08BC0B30E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\OneBar
DefaultGroupName=OneBar
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
OutputDir=..\artifacts\installer
OutputBaseFilename=OneBar_Setup_v{#MyAppVersion}
SetupIconFile=..\src\OneBar\Assets\OneBar.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
UsePreviousAppDir=yes
SetupLogging=yes

[Tasks]
Name: "autostart"; Description: "开机自动启动"; GroupDescription: "安装与运行选项："; Flags: checkedonce
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "安装与运行选项："; Flags: checkedonce
Name: "startmenuicon"; Description: "创建开始菜单入口"; GroupDescription: "安装与运行选项："; Flags: checkedonce
Name: "showtaskbar"; Description: "运行时显示任务栏 OneBar 图标"; GroupDescription: "安装与运行选项："; Flags: checkedonce
Name: "showtray"; Description: "运行时显示系统托盘图标"; GroupDescription: "安装与运行选项："; Flags: checkedonce

[Files]
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\Workflows"

[Icons]
Name: "{autodesktop}\OneBar"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{group}\OneBar"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: startmenuicon
Name: "{group}\OneBar 设置"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--settings"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: startmenuicon
Name: "{group}\卸载 OneBar"; Filename: "{uninstallexe}"; Tasks: startmenuicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "--installer-config --autostart={code:TaskBool|autostart} --desktop={code:TaskBool|desktopicon} --startmenu={code:TaskBool|startmenuicon} --taskbar={code:TaskBool|showtaskbar} --tray={code:TaskBool|showtray} --workflowRoot=""{app}\Workflows"""; Flags: runhidden waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "启动 OneBar"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v OneBar /f >nul 2>&1"; Flags: runhidden

[UninstallDelete]
Type: files; Name: "{userdesktop}\OneBar.lnk"
Type: filesandordirs; Name: "{userprograms}\OneBar"

[Code]
function TaskBool(Param: string): string;
begin
  if WizardIsTaskSelected(Param) then
    Result := '1'
  else
    Result := '0';
end;
