@echo off
setlocal

REM Usage:
REM   run-pakistan-seed.bat
REM   run-pakistan-seed.bat localhost\SQLEXPRESS

set "SERVER=%~1"
if "%SERVER%"=="" set "SERVER=localhost\SQLEXPRESS"

set "ROOT=%~dp0"
set "BOOTSTRAP_SQL=%ROOT%NEW_DB\bootstrap_auth_seed.sql"
set "SEED_SQL=%ROOT%NEW_DB\seed_donations_expenses_approvals.sql"

where sqlcmd >nul 2>nul
if errorlevel 1 (
  echo [ERROR] sqlcmd not found.
  echo Install SQL Server Command Line Utilities and rerun this script.
  exit /b 1
)

if not exist "%BOOTSTRAP_SQL%" (
  echo [ERROR] Missing file: %BOOTSTRAP_SQL%
  exit /b 1
)

if not exist "%SEED_SQL%" (
  echo [ERROR] Missing file: %SEED_SQL%
  exit /b 1
)

echo [1/2] Applying bootstrap auth seed on server: %SERVER%
sqlcmd -S "%SERVER%" -E -d master -b -i "%BOOTSTRAP_SQL%"
if errorlevel 1 (
  echo [ERROR] bootstrap_auth_seed.sql failed.
  exit /b 1
)

echo [2/2] Applying Pakistani finance and approval seed on server: %SERVER%
sqlcmd -S "%SERVER%" -E -d master -b -i "%SEED_SQL%"
if errorlevel 1 (
  echo [ERROR] seed_donations_expenses_approvals.sql failed.
  exit /b 1
)

echo [OK] Seed completed successfully.
echo You can now run backend and frontend, then open Dashboard and Approval Workflow.

endlocal
