@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."
set "EXAMPLE=%REPO_ROOT%\.env.example"
set "ENV_FILE=%REPO_ROOT%\.env"

if not exist "%EXAMPLE%" (
  echo Missing environment example: %EXAMPLE%
  exit /b 1
)

if exist "%ENV_FILE%" (
  echo .env already exists. Delete it first if you want to recreate it.
  exit /b 0
)

copy "%EXAMPLE%" "%ENV_FILE%" >nul
echo Created local environment: %ENV_FILE%
