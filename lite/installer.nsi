Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
Name "Steam Share Master 精简版"
OutFile "Steam-Share-Master-Lite-Setup-0.2.1.exe"
InstallDir "$PROGRAMFILES64\Steam Share Master Lite"
RequestExecutionLevel admin
!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "SimpChinese"
Section "程序文件（必需）"
  SectionIn RO
  SetShellVarContext all
  SetOutPath "$INSTDIR"
  File "Steam-Share-Master-Lite.exe"
  File "..\LICENSE"
  CreateDirectory "$SMPROGRAMS\Steam Share Master 精简版"
  CreateShortcut "$SMPROGRAMS\Steam Share Master 精简版\Steam Share Master 精简版.lnk" "$INSTDIR\Steam-Share-Master-Lite.exe"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SteamShareMasterLite" "DisplayName" "Steam Share Master 精简版"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SteamShareMasterLite" "DisplayVersion" "0.2.1"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SteamShareMasterLite" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SteamShareMasterLite" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SteamShareMasterLite" "NoRepair" 1
SectionEnd
Section "创建桌面快捷方式（可选）"
  SetShellVarContext all
  IfFileExists "$DESKTOP\Steam Share Master 精简版.lnk" done
  CreateShortcut "$DESKTOP\Steam Share Master 精简版.lnk" "$INSTDIR\Steam-Share-Master-Lite.exe"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SteamShareMasterLite" "DesktopShortcutCreated" 1
  done:
SectionEnd
Section "Uninstall"
  SetShellVarContext all
  nsExec::ExecToLog '"$SYSDIR\netsh.exe" advfirewall firewall delete rule name="SteamShareMasterLite"'
  Pop $0
  ReadRegDWORD $1 HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SteamShareMasterLite" "DesktopShortcutCreated"
  ${If} $1 == 1
    Delete "$DESKTOP\Steam Share Master 精简版.lnk"
  ${EndIf}
  Delete "$SMPROGRAMS\Steam Share Master 精简版\Steam Share Master 精简版.lnk"
  RMDir "$SMPROGRAMS\Steam Share Master 精简版"
  Delete "$INSTDIR\Steam-Share-Master-Lite.exe"
  Delete "$INSTDIR\LICENSE"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SteamShareMasterLite"
SectionEnd
