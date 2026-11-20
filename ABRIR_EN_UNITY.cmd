@echo off
setlocal
set "PCFIX_PROJECT=%~dp0PCRepairLab"
if exist "%~dp0proyecto-activo.local.txt" set /p "PCFIX_PROJECT="<"%~dp0proyecto-activo.local.txt"
set "PCFIX_UNITY=%UIPC_EDITOR%"
if not defined PCFIX_UNITY set "PCFIX_UNITY=%ProgramFiles%\Unity 6000.0.37f1\Editor\Unity.exe"
if not exist "%PCFIX_UNITY%" set "PCFIX_UNITY=%ProgramFiles%\Unity\Hub\Editor\6000.0.37f1\Editor\Unity.exe"
if not exist "%PCFIX_UNITY%" (
  echo Agrega PCRepairLab a Unity Hub o define UIPC_EDITOR con la ruta de Unity.exe.
  pause
  exit /b 1
)
start "" "%PCFIX_UNITY%" -projectPath "%PCFIX_PROJECT%"
endlocal
