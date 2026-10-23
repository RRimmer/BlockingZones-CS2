@echo off
rem Builds BlockingZones in Release and writes the log next to this file.
cd /d "%~dp0"
dotnet build BlockingZones.csproj -c Release -nologo > build.log 2>&1
echo exit %ERRORLEVEL% >> build.log
type build.log
pause
