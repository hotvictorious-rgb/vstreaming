Set WshShell = CreateObject("WScript.Shell")
Set FSO = CreateObject("Scripting.FileSystemObject")

appDir = FSO.GetParentFolderName(WScript.ScriptFullName)
WshShell.CurrentDirectory = appDir

' Launch the self-contained native Studio executable
WshShell.Run """" & appDir & "\Victorious Streaming Studio.exe""", 1, False