AC0KG-MineShell
===============
MineShell is a Microsoft Windows service shell for Minecraft.

Features
--------------
* Hosts the Java Minecraft process in a console window or as a Windows service.
* Provides multi-user console access via raw TCP (or Telnet) session.
* Offers scriptable response to console events.

Service Shell
--------------
When running on Windows the Minecraft server process runs
as a console application on the desktop. The service shell
feature allows the Minecraft server process to be started
as a Windows service, so that it can be seamlessly and
automatically started and stopped with Windows, and run in
the background without requiring a logged-in user.

The shell can also be run on the desktop as a console app
to provide the remote console access and script features
without the Windows service features.

Raw TCP Access
--------------
Server admins requiring access to the Minecraft console
may open a raw TCP or Telnet connection to the console
(using, e.g., PuTTY or netcat/socat). The remote access
server supports either open access or username/password
authentication (via clear-text settings in the application
config file). Users connected via the remote access server
will see commands issued by other remote access server users.

Script Support
--------------
Each line of console text received by the shell from the 
Minecraft server is routed to a user-editable C# language 
script for processing. The script may issue zero or more 
console commands in response to console lines.
