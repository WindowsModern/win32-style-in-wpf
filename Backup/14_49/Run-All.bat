@echo off
setlocal
set "ROOT=%~dp0"
if not exist "%ROOT%logs" mkdir "%ROOT%logs"

echo ============================================================
echo  PresentationFramework.Theme - self-test
echo  Root: %ROOT%
echo ============================================================

rem Run a native process first: on XP this warms the visual-style
rem service before the first WPF process starts.
call "%ROOT%Run-Native.bat"
call "%ROOT%Run-Plain.bat"
call "%ROOT%Run-Transition.bat"

echo.
echo ============================================================
echo  Done.
echo  Logs:   %ROOT%logs
echo  Plain:  %ROOT%logs\plain\selftest.log
echo  Trans:  %ROOT%logs\transition\selftest.log
echo  Native: %ROOT%logs\native-win32.png
echo ============================================================
start "" "%ROOT%logs"
endlocal