@echo off
setlocal
set "ROOT=%~dp0"
echo Opening ThemeDemo with theme.xaml (no transition)
start "" "%ROOT%ThemeDemo.exe"
endlocal
