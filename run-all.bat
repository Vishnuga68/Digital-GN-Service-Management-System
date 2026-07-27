@echo off
title GN-SYSTEM Launcher
echo ===================================================
echo             GN-SYSTEM SERVICES LAUNCHER
echo ===================================================
echo.
echo Starting Backend API and Frontend in separate windows...
echo.

:: 1. Launch Backend API in a separate Command Prompt window
echo [+] Launching Backend Web API on http://localhost:5058 ...
start "GN-SYSTEM Backend API (Port 5058)" cmd /k "cd /d "%~dp0BACKEND\EGN_Service_Management" && dotnet run --launch-profile http"

:: 2. Launch Frontend Angular App in a separate Command Prompt window
echo [+] Launching Frontend Angular App on http://localhost:4200 ...
start "GN-SYSTEM Frontend (Port 4200)" cmd /k "cd /d "%~dp0FRONTEND" && npm start"

echo.
echo ===================================================
echo Done! Both servers have been launched in separate windows.
echo Keep those windows open to keep the servers running.
echo ===================================================
pause