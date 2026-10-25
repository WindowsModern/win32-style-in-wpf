PresentationFramework.Theme - portable test folder
==================================================

Contents
--------
ThemeDemo.exe                 WPF theme gallery + self-test
PresentationFramework.Theme.dll
ThemeExplorer.Core.dll        required by ThemeDemo
NativeComparison.exe          native Win32/WinForms comparison window
Run-All.bat                   run both themes + native screenshot
Run-Plain.bat                 theme.xaml only
Run-Transition.bat            theme.withtransition.xaml only
Run-Native.bat                native Win32 screenshot
Run-Demo.bat                  open the demo normally (plain theme)
Run-Demo-Plain.bat            open the demo with theme.xaml
Run-Demo-Transition.bat       open the demo with theme.withtransition.xaml (150 ms crossfade)

Quick start on Windows XP
-------------------------
1. Install .NET Framework 4.0 (or newer).
2. Copy this whole TestDemo folder to the XP machine.
3. Double-click Run-All.bat.
4. Send back:

   logs\plain\selftest.log
   logs\transition\selftest.log
   logs\native-win32.png
   logs\plain\theme-plain.png

Notes
-----
- Do not move ThemeDemo.exe out of this folder: it needs the two DLLs.
- ThemeDemo writes selftest.log plus screenshots into the folder passed
  with --out=...
- NativeComparison uses the real Win32 common controls (comctl32 v6)
  and is the reference for "what the current XP visual style looks like".
- If the app does not start, check that .NET Framework 4.0 is installed
  and that the DLLs are next to the EXE.
