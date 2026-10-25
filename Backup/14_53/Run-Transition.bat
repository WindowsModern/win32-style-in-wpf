@echo off
setlocal
set "ROOT=%~dp0"
if not exist "%ROOT%logs\transition" mkdir "%ROOT%logs\transition"

echo [transition] running ThemeDemo.exe --transition --selftest
start /wait "" "%ROOT%ThemeDemo.exe" --transition --selftest "--out=%ROOT%logs\transition"
set "RC=%ERRORLEVEL%"
echo [transition] exit=%RC%
if exist "%ROOT%logs\transition\selftest.log" (
  echo ----- transition selftest.log -----
  type "%ROOT%logs\transition\selftest.log"
)
endlocal
