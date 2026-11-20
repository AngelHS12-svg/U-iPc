@echo off
setlocal
set "PCFIX_PROJECT=%~dp0PCRepairLab"
if exist "%~dp0proyecto-activo.local.txt" set /p "PCFIX_PROJECT="<"%~dp0proyecto-activo.local.txt"
set "PCFIX_UNITY=%UIPC_EDITOR%"
if not defined PCFIX_UNITY set "PCFIX_UNITY=%ProgramFiles%\Unity 6000.0.37f1\Editor\Unity.exe"
if not exist "%PCFIX_UNITY%" set "PCFIX_UNITY=%ProgramFiles%\Unity\Hub\Editor\6000.0.37f1\Editor\Unity.exe"
if not exist "%PCFIX_UNITY%" (
  echo Usa PC FIX LAB - Compilar para Windows desde Unity.
  pause
  exit /b 1
)
for %%I in ("%PCFIX_PROJECT%\..") do set "PCFIX_BASE=%%~fI"
echo Cierra el editor antes de compilar por consola.
"%PCFIX_UNITY:~0,-4%.com" -batchmode -nographics -quit -projectPath "%PCFIX_PROJECT%" -executeMethod BuildAutomation.BuildWindows -logFile "%PCFIX_BASE%\unity-build.log"
if errorlevel 1 (
  echo Consulta unity-build.log o compila desde el menu de Unity.
  pause
  exit /b 1
)
echo Ejecutable: %PCFIX_BASE%\Builds\U-iPC\U-iPC.exe
pause
endlocal
