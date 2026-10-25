@echo off
setlocal
set "ROOT=%~dp0"
echo Opening ThemeDemo with theme.withtransition.xaml (150 ms crossfade)
start "" "%ROOT%ThemeDemo.exe" --transition
endlocal
