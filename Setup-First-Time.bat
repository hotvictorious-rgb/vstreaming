@echo off
setlocal enabledelayedexpansion
title Victorious Streaming Hub - First Time Setup
color 0b
cd /d "%~dp0"

echo ===================================================================
echo     VICTORIOUS STREAMING HUB - 1-CLICK FIRST TIME SETUP
echo ===================================================================
echo.
echo Architected and Packaged under the leadership of:
echo Victory Saviour Edet, CEO of VICTORIOUS MARKET
echo ===================================================================
echo.

echo [1/4] Checking Node.js runtime...
where node >nul 2>&1
if %errorlevel% neq 0 (
    echo [!] Node.js is not installed on this computer!
    echo [*] Attempting to install Node.js via winget...
    winget install OpenJS.NodeJS.LTS --accept-package-agreements --accept-source-agreements --silent
    where node >nul 2>&1
    if %errorlevel% neq 0 (
        echo [!] Please install Node.js from https://nodejs.org and rerun this setup.
        pause
        exit /b 1
    )
    echo [OK] Node.js successfully installed!
) else (
    for /f "tokens=*" %%i in ('node -v') do set NODE_VER=%%i
    echo [OK] Node.js is ready: !NODE_VER!
)

echo.
echo [2/4] Verifying production dependencies...
if not exist "node_modules\" (
    echo [*] Installing required packages...
    call npm install --production
    echo [OK] Packages installed!
) else (
    echo [OK] Dependencies already present.
)

echo.
echo [3/4] Configuring Windows Defender Firewall...
netsh advfirewall firewall add rule name="Victorious Streaming Hub (HTTP 3000)" dir=in action=allow protocol=TCP localport=3000 profile=any >nul 2>&1
netsh advfirewall firewall add rule name="Victorious Streaming Hub (HTTPS 3443)" dir=in action=allow protocol=TCP localport=3443 profile=any >nul 2>&1
echo [OK] Firewall rules configured for Ports 3000 and 3443.

echo.
echo [4/4] Creating Windows Desktop Shortcut...
powershell -ExecutionPolicy Bypass -File "%~dp0create_desktop_shortcut.ps1"

echo.
echo ===================================================================
echo   SUCCESS: VICTORIOUS STREAMING HUB IS READY FOR SUNDAY!
echo ===================================================================
echo.
pause