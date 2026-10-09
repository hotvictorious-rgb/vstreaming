$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = $PSScriptRoot }
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }

$desktop = [Environment]::GetFolderPath('Desktop')
$wsh = New-Object -ComObject WScript.Shell

$s1 = $wsh.CreateShortcut("$desktop\Victorious Streaming Hub.lnk")
$s1.TargetPath = "$env:SystemRoot\System32\wscript.exe"
$s1.Arguments = "`"$scriptDir\Launch-Studio.vbs`""
$s1.WorkingDirectory = $scriptDir
$s1.IconLocation = "$env:SystemRoot\System32\imageres.dll,25"
$s1.Description = "Victorious Streaming Hub - Church Broadcast Studio (Victory Saviour Edet, CEO of VICTORIOUS MARKET)"
$s1.Save()

$s2 = $wsh.CreateShortcut("$desktop\Stop Victorious Hub.lnk")
$s2.TargetPath = "$env:SystemRoot\System32\cmd.exe"
$s2.Arguments = "/c `"$scriptDir\Stop-Sunday-Studio.bat`""
$s2.WorkingDirectory = $scriptDir
$s2.IconLocation = "$env:SystemRoot\System32\imageres.dll,98"
$s2.Description = "Stop Victorious Streaming Hub"
$s2.Save()

Write-Host "Portable desktop shortcuts configured successfully!"