@echo off
title Victorious Streaming Hub - Church Broadcast Studio
color 0e
cd /d "%~dp0"

echo ===================================================================
echo     VICTORIOUS STREAMING HUB - SUNDAY BROADCAST STUDIO
echo ===================================================================
echo.
echo Architected and Packaged under the leadership of:
echo Victory Saviour Edet, CEO of VICTORIOUS MARKET
echo ===================================================================
echo.

:: 1. Clear any stale node processes on port 3000
for /f "tokens=5" %%a in ('netstat -aon 2^>nul ^| findstr ":3000" ^| findstr "LISTENING"') do (
    echo [*] Clearing previous background server instance (PID %%a)
    taskkill /F /PID %%a >nul 2>&1
    ping 127.0.0.1 -n 2 >nul
)

:: 2. Open Director Dashboard in default browser
echo [*] Opening Director Dashboard in browser...
start http://localhost:3000

:: 3. Run the broadcast server directly in this window
echo [*] Starting broadcast engine...
echo.
node server.js

pause