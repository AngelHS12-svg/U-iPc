@echo off
setlocal
set "PCFIX_PROJECT=%~dp0PCRepairLab"
if exist "%~dp0proyecto-activo.local.txt" set /p "PCFIX_PROJECT="<"%~dp0proyecto-activo.local.txt"
for %%I in ("%PCFIX_PROJECT%\..") do set "PCFIX_BASE=%%~fI"
set "PCFIX_EXE=%PCFIX_BASE%\Builds\U-iPC\U-iPC.exe"
if not exist "%PCFIX_EXE%" (
  echo Compila el juego primero desde Unity.
  pause
  exit /b 1
)
start "" /wait "%PCFIX_EXE%" --smoke-test --verify-output "%PCFIX_BASE%\VerificationStandalone" -logFile "%PCFIX_BASE%\player-verification.log"
echo Consulta VerificationStandalone\smoke-test-v2.txt.
pause
endlocal
