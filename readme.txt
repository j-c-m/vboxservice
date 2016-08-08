vboxservice 0.1
===============

!!WARNING!! - 
This software works me, it might work for you.
There is no warranty.
Use at your own risk.

Install:

This service relies on proper setting of VBOX_USER_HOME and proper 
permissions for reading all xml config files and disks etc.
If this service fails to start your VM(s) it is likely because
the user the service runs as does not have full permissions to
the config or disk image files, or VBOX_USER_HOME enivronment
variable is not set to the directory containing the main
VirtualBox.xml config file.

The installer makes a best guess on what to set the environment
VBOX_USER_HOME variable to, you can change it using regedit.

The installer installs the service "VirtualBox Start Service" by
defalt to run as 'local service' user.  A lot of permission problems
can be fixed by setting this to a user in the Administrator group.
(Control Panel->Administrative Tools->Services->Properties->Log On Tab
Note: If your machines/images/config files are stored on a network
drive, you will need to change the log on account for the service
to an account with permissions to r/w the network share.


Registry Keys:

\HKEY_LOCAL_MACHINE\SOFTWARE\VBoxService
 Logfile - string - Name of logfile.
 
\HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment
 VBOX_USER_HOME - string - VBOX_USER_HOME environment variable.

Additonal XML to add to machine config <ExtraData> section.

      <ExtraDataItem name="SERVICE/AutoStart" value="on"/>
      <ExtraDataItem name="SERVICE/ShutdownMethod" value="savestate"/>
      
      AutoStart - "on" start on boot, any other value or missing do not
       start on boot.
       
      ShutdownMethod - Method used to shutdown VM on service stop, 
        acpibowerbutton or savestate are typical, default if not
        specified is savestate.  See vboxmanage controlvm for other
        methods. 
