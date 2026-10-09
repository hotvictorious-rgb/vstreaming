Set WshShell = CreateObject("WScript.Shell")
Set FSO = CreateObject("Scripting.FileSystemObject")

' Dynamically detect the folder this script is running in (100% portable on any laptop)
appDir = FSO.GetParentFolderName(WScript.ScriptFullName)
WshShell.CurrentDirectory = appDir

' 1. Clear any stale process on port 3000
WshShell.Run "cmd /c for /f ""tokens=5"" %a in ('netstat -aon 2^>nul ^| findstr "":3000"" ^| findstr ""LISTENING""') do taskkill /F /PID %a >nul 2>&1", 0, True

' 2. Launch Node server completely hidden (0 = SW_HIDE)
WshShell.Run "node server.js", 0, False

' 3. Wait 1.2 seconds for server to initialize
WScript.Sleep 1200

' 4. Locate Edge (32-bit or 64-bit on any Windows PC)
edgePath = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
If Not FSO.FileExists(edgePath) Then
    edgePath = "C:\Program Files\Microsoft\Edge\Application\msedge.exe"
End If
If Not FSO.FileExists(edgePath) Then
    edgePath = "C:\Program Files\Google\Chrome\Application\chrome.exe"
End If

profileDir = appDir & "\.profile"
appArgs = "--app=http://localhost:3000 --user-data-dir=""" & profileDir & """ --app-id=VictoriousStreamingHub --window-size=1366,850 --autoplay-policy=no-user-gesture-required --disable-features=WebRtcHideLocalIpsWithMdns --allow-insecure-localhost --no-first-run --no-default-browser-check"

WshShell.Run """" & edgePath & """ " & appArgs, 1, False