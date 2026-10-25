@echo off
setlocal
set "ROOT=%~dp0"
if not exist "%ROOT%logs\plain" mkdir "%ROOT%logs\plain"

echo [plain] running ThemeDemo.exe --selftest
start /wait "" "%ROOT%ThemeDemo.exe" --selftest "--out=%ROOT%logs\plain"
set "RC=%ERRORLEVEL%"
echo [plain] exit=%RC%
if exist "%ROOT%logs\plain\selftest.log" (
  echo ----- plain selftest.log -----
  type "%ROOT%logs\plain\selftest.log"
)
endlocal
