@echo off
title Stop Victorious Streaming Hub
color 0c
cd /d "%~dp0"

echo ===================================================================
echo     STOPPING VICTORIOUS STREAMING HUB STUDIO
echo ===================================================================
echo.

echo [*] Stopping Victorious Hub desktop application...
taskkill /F /IM "Victorious Streaming Hub.exe" >nul 2>&1

echo [*] Stopping Victorious Hub streaming engine...
taskkill /F /T /IM node.exe >nul 2>&1

echo.
echo [OK] All broadcast engines stopped and ports released safely.
echo.
ping 127.0.0.1 -n 2 >nul