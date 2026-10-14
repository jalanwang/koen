# Installs KoEnCaret for the current user and registers it to start at every logon.
$ErrorActionPreference = 'Stop'
$src = Join-Path $PSScriptRoot 'KoEnCaret.exe'
if (-not (Test-Path $src)) {
    & (Join-Path $PSScriptRoot 'build.cmd')
    if (-not (Test-Path $src)) { throw 'Build failed.' }
}

$dir = Join-Path $env:LOCALAPPDATA 'KoEnCaret'
$exe = Join-Path $dir 'KoEnCaret.exe'

Get-Process KoEnCaret -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300
New-Item -ItemType Directory -Force $dir | Out-Null
Copy-Item $src $exe -Force

$run = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
Set-ItemProperty -Path $run -Name 'KoEnCaret' -Value "`"$exe`""

Start-Process $exe
Write-Host "Installed: $exe"
Write-Host 'Registered for auto-start at logon (HKCU Run).'
