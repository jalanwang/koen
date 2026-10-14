# Removes KoEnCaret: stops the process, deletes the auto-start entry and the installed files.
Get-Process KoEnCaret -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'KoEnCaret' -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300
Remove-Item -Recurse -Force (Join-Path $env:LOCALAPPDATA 'KoEnCaret') -ErrorAction SilentlyContinue
Write-Host 'KoEnCaret removed.'
