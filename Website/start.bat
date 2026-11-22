@echo off
cd /d "%~dp0"
echo Starting FrogRunner download site...
echo.
echo PC:    http://localhost:3000
echo Phone: http://192.168.0.150:3000
echo.
node server.js
pause