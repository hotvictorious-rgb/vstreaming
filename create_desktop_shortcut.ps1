$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = $PSScriptRoot }
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }

$desktop = [Environment]::GetFolderPath('Desktop')
$startMenu = [Environment]::GetFolderPath('Programs')
$wsh = New-Object -ComObject WScript.Shell

$exePath = "$scriptDir\Victorious Streaming Studio.exe"
$iconPath = "$scriptDir\app.ico"
if (-not (Test-Path $iconPath)) { $iconPath = "$exePath,0" }

# 1. Desktop Shortcut
$s1 = $wsh.CreateShortcut("$desktop\Victorious Streaming Studio.lnk")
$s1.TargetPath = $exePath
$s1.WorkingDirectory = $scriptDir
$s1.IconLocation = "$iconPath,0"
$s1.Description = "Victorious Streaming Studio - Professional Church Multi-Camera Broadcast System"
$s1.Save()

# 2. Start Menu Shortcut (Searchable in Windows Start)
if (Test-Path $startMenu) {
    $s2 = $wsh.CreateShortcut("$startMenu\Victorious Streaming Studio.lnk")
    $s2.TargetPath = $exePath
    $s2.WorkingDirectory = $scriptDir
    $s2.IconLocation = "$iconPath,0"
    $s2.Description = "Victorious Streaming Studio - Professional Church Multi-Camera Broadcast System"
    $s2.Save()
}

# 3. Stop Studio Shortcut
$s3 = $wsh.CreateShortcut("$desktop\Stop Victorious Studio.lnk")
$s3.TargetPath = "$env:SystemRoot\System32\cmd.exe"
$s3.Arguments = "/c `"$scriptDir\Stop-Sunday-Studio.bat`""
$s3.WorkingDirectory = $scriptDir
$s3.IconLocation = "$env:SystemRoot\System32\imageres.dll,98"
$s3.Description = "Stop Victorious Streaming Studio Safely"
$s3.Save()

Write-Host "Desktop and Start Menu shortcuts configured successfully!"