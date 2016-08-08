Name "VBoxService"

Var uninstallkey
Var VBOX_USER_HOME

OutFile "vboxservice-0.1.exe"

InstallDir $PROGRAMFILES\VBoxService

RequestExecutionLevel admin

InstallDirRegKey HKLM "Software\VBoxService" "Install_Dir"

Page directory
Page instfiles

Section ""
  StrCpy $uninstallkey "Software\Microsoft\Windows\CurrentVersion\Uninstall\VBoxService"
  ReadEnvStr $VBOX_USER_HOME "VBOX_USER_HOME"
  
  SetOutPath $INSTDIR
  
	StrCmp $VBOX_USER_HOME "" 0 vbox_home_set
	IfFileExists "$PROFILE\.VirtualBox\VirtualBox.xml" 0 vbox_home_set
	DetailPrint "Setting VBOX_USER_HOME to $PROFILE\.VirtualBox..."
	WriteRegStr HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "VBOX_USER_HOME" "$PROFILE\.VirtualBox"
	SetRebootFlag true
	
	vbox_home_set:
	
	File "vboxservice.exe"
	File "readme.txt"
	
	WriteRegStr HKLM "Software\VBoxService" "Install_Dir" "$INSTDIR"
	WriteRegStr HKLM "Software\VBoxService" "Logfile" "$WINDIR\temp\vboxservice.log"
	
	
	WriteRegStr HKLM $uninstallkey "DisplayName" "VirtualBox Service"
	WriteRegStr HKLM $uninstallkey "UninstallString" '"$INSTDIR\uninstall.exe"'
	WriteRegDWORD HKLM $uninstallkey "NoModify" 1
	WriteRegDWORD HKLM $uninstallkey "NoRepair" 1
	WriteUninstaller "uninstall.exe"
	
	DetailPrint "Installing service..."
	SimpleSC::InstallService "vboxservice" "VirtualBox Start Service" "16" "2" "$INSTDIR\vboxservice.exe" "" "" ""
	
	ExecShell "open" "$INSTDIR\readme.txt"
	
	IfRebootFlag 0 noreboot
		MessageBox MB_YESNO "A reboot is required, restart now?" IDNO noreboot
			Reboot
	noreboot:
SectionEnd

Section "Uninstall"
  StrCpy $uninstallkey "Software\Microsoft\Windows\CurrentVersion\Uninstall\VBoxService"
	DetailPrint "Stopping service..."
	SimpleSC::StopService "vboxservice"
	DetailPrint "Removing service..."
	SimpleSC::RemoveService "vboxservice"
	
	DetailPrint "Removing registry keys..."
	DeleteRegKey HKLM $uninstallkey
	DeleteRegKey HKLM "Software\VBoxService"
	DeleteRegValue HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "VBOX_USER_HOME"
	
	Delete "$INSTDIR\*.*"
	
	RMDir "$INSTDIR"
SectionEnd

	