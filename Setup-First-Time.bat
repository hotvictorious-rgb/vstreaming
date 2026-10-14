@echo off
setlocal enabledelayedexpansion
title Victorious Streaming Studio - Quick Setup
color 0b
cd /d "%~dp0"

echo ===================================================================
echo     VICTORIOUS STREAMING STUDIO - 1-CLICK QUICK SETUP
echo ===================================================================
echo.
echo Architected and Packaged under the leadership of:
echo Victory Saviour Edet, CEO of VICTORIOUS MARKET
echo ===================================================================
echo.

echo [1/3] Verifying portable runtime engine...
if exist "runtime\node\node.exe" (
    echo [OK] Self-contained runtime engine is present and ready.
) else (
    echo [!] Portable runtime not detected in runtime\node. Searching system...
    where node >nul 2>&1
    if !errorlevel! neq 0 (
        echo [!] Please ensure the runtime folder is included.
        pause
        exit /b 1
    )
    echo [OK] System runtime detected.
)

if not exist "Victorious Streaming Studio.exe" (
    echo [*] Compiling native Windows Studio executable...
    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /win32icon:app.ico /win32manifest:app.manifest /out:"Victorious Streaming Studio.exe" Launcher.cs >nul 2>&1
)

echo.
echo [2/3] Configuring Windows Defender Firewall for local camera devices...
netsh advfirewall firewall add rule name="Victorious Streaming Studio (HTTP)" dir=in action=allow protocol=TCP localport=3000-3010 profile=private >nul 2>&1
netsh advfirewall firewall add rule name="Victorious Streaming Studio (HTTPS)" dir=in action=allow protocol=TCP localport=3443-3453 profile=private >nul 2>&1
echo [OK] Firewall configured for Private Networks (LAN camera ingest).

echo.
echo [3/3] Creating Windows Desktop Shortcuts...
powershell -ExecutionPolicy Bypass -File "%~dp0create_desktop_shortcut.ps1"

echo.
echo ===================================================================
echo   SUCCESS: VICTORIOUS STREAMING STUDIO IS READY FOR LIVE BROADCAST!
echo ===================================================================
echo.
pause