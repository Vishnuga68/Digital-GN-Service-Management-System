# Get the directory of this script
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "            GN-SYSTEM SERVICES LAUNCHER" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Starting Backend API and Frontend in separate windows..." -ForegroundColor Yellow
Write-Host ""

# 1. Launch Backend API in a separate PowerShell window
Write-Host "[+] Launching Backend Web API on http://localhost:5058 ..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$scriptDir\BACKEND\EGN_Service_Management'; dotnet run --launch-profile http"

# 2. Launch Frontend Angular App in a separate PowerShell window
Write-Host "[+] Launching Frontend Angular App on http://localhost:4200 ..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$scriptDir\FRONTEND'; npm start"

Write-Host ""
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "Done! Both servers have been launched in separate windows." -ForegroundColor Yellow
Write-Host "Keep those windows open to keep the servers running." -ForegroundColor Yellow
Write-Host "===================================================" -ForegroundColor Cyan
