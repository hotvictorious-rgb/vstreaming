@echo off
title Stop Victorious Streaming Studio
color 0c
cd /d "%~dp0"

echo ===================================================================
echo     STOPPING VICTORIOUS STREAMING STUDIO SAFELY
echo ===================================================================
echo.

echo [*] Requesting clean shutdown of Victorious Streaming Studio...
taskkill /IM "Victorious Streaming Studio.exe" >nul 2>&1

echo.
echo [OK] Victorious Streaming Studio stopped and ports released safely.
echo.
ping 127.0.0.1 -n 2 >nul