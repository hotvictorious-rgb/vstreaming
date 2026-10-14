$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = $PSScriptRoot }
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }

$desktop = [Environment]::GetFolderPath('Desktop')
$wsh = New-Object -ComObject WScript.Shell

$s1 = $wsh.CreateShortcut("$desktop\Victorious Streaming Studio.lnk")
$s1.TargetPath = "$scriptDir\Victorious Streaming Studio.exe"
$s1.WorkingDirectory = $scriptDir
$s1.IconLocation = "$scriptDir\Victorious Streaming Studio.exe,0"
$s1.Description = "Victorious Streaming Studio - Church Broadcast Center"
$s1.Save()

$s2 = $wsh.CreateShortcut("$desktop\Stop Victorious Studio.lnk")
$s2.TargetPath = "$env:SystemRoot\System32\cmd.exe"
$s2.Arguments = "/c `"$scriptDir\Stop-Sunday-Studio.bat`""
$s2.WorkingDirectory = $scriptDir
$s2.IconLocation = "$env:SystemRoot\System32\imageres.dll,98"
$s2.Description = "Stop Victorious Streaming Studio Safely"
$s2.Save()

Write-Host "Desktop shortcuts configured successfully!"