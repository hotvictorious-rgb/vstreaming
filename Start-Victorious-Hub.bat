@echo off
title Victorious Streaming Hub - Church Broadcast Studio
cd /d "%~dp0"

echo =============================================================
echo    VICTORIOUS STREAMING HUB - CHURCH BROADCAST STUDIO       
echo =============================================================
echo.
echo Starting local broadcast server...
echo.

:: Launch Node server in background and open browser
start "" "http://localhost:3000"
node server.js

pause
