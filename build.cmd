@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
set GAC=%WINDIR%\Microsoft.NET\assembly\GAC_MSIL
"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 /out:"%~dp0KoEnCaret.exe" ^
  /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Accessibility.dll ^
  /r:"%GAC%\UIAutomationClient\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationClient.dll" ^
  /r:"%GAC%\UIAutomationTypes\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationTypes.dll" ^
  /r:"%GAC%\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll" ^
  "%~dp0KoEnCaret.cs"
exit /b %ERRORLEVEL%
