@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 /out:"%~dp0KoEnCaret.exe" ^
  /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Accessibility.dll ^
  "%~dp0KoEnCaret.cs"
exit /b %ERRORLEVEL%
