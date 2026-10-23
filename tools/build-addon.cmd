@echo off
rem Compiles the BlockingZones layout (bz) + stylesheet into YOUR client addon (asks for its name). Log: build-addon.log
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-addon.ps1" %*
pause
