@echo off
setlocal
set "ROOT=%~dp0"
if not exist "%ROOT%logs" mkdir "%ROOT%logs"

echo [native] running NativeComparison.exe
start /wait "" "%ROOT%NativeComparison.exe" "--out=%ROOT%logs\native-win32.png"
start /wait "" "%ROOT%NativeComparison.exe" --form=sliders "--out=%ROOT%logs\native-sliders.png"
start /wait "" "%ROOT%NativeComparison.exe" --form=scrollbars "--out=%ROOT%logs\native-scrollbars.png"
start /wait "" "%ROOT%NativeComparison.exe" --form=progress "--out=%ROOT%logs\native-progress.png"
echo [native] exit=%ERRORLEVEL%
if exist "%ROOT%logs\native-win32.png" (
  echo [native] screenshots: native-win32.png native-sliders.png native-scrollbars.png native-progress.png
)
endlocal
