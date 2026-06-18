@echo off
setlocal enabledelayedexpansion

REM Set working directory
set "REPO_ROOT=C:\Users\fireh\OneDrive - FAST National University\Documents\Semester 4\DB Theory\Smart-Disaster-Response-MIS-main\Smart-Disaster-Response-MIS-main"
set "BACKEND_DIR=%REPO_ROOT%\backend\Database_Backend\Database_Backend"
set "FRONTEND_DIR=%REPO_ROOT%\frontend"

cd /d "%REPO_ROOT%"

echo.
echo ========== STEP 1: DETECT SQL SERVER INSTANCE ==========
echo.

REM Try sqlcmd to find Final_DB
set "DETECTED_INSTANCE=localhost\SQLEXPRESS"
set "SQL_FOUND=0"

for %%I in ("localhost\SQLEXPRESS" ".\SQLEXPRESS" "(localdb)\MSSQLLocalDB") do (
    echo Checking instance: %%I
    sqlcmd -S %%I -Q "SELECT name FROM sys.databases WHERE name='Final_DB'" 2>nul | findstr /R "Final_DB" >nul 2>&1
    if !errorlevel! equ 0 (
        echo Found Final_DB in %%I
        set "DETECTED_INSTANCE=%%I"
        set "SQL_FOUND=1"
        goto found_db
    )
)

:found_db
if %SQL_FOUND% equ 1 (
    echo SQL Instance detected: %DETECTED_INSTANCE%
) else (
    echo Final_DB not found, using default: %DETECTED_INSTANCE%
)

REM Export for logging
set "DETECTED_SQL_INSTANCE=%DETECTED_INSTANCE%"

echo.
echo ========== STEP 2: BUILD AND RUN BACKEND ==========
echo.

cd /d "%BACKEND_DIR%"
echo Current directory: %cd%

REM Check if .csproj exists
if not exist "Database_Backend.csproj" (
    echo ERROR: Database_Backend.csproj not found in %cd%
    goto backend_error
)

REM Build the backend
echo Building backend...
dotnet build Database_Backend.csproj -c Release 2>&1 | findstr /V "^$" > "%TEMP%\backend_build.log"
if !errorlevel! neq 0 (
    echo Build failed. See log:
    type "%TEMP%\backend_build.log"
    goto backend_error
)
echo Build succeeded!

REM Start backend in detached background process
echo Starting backend on http://localhost:5226 ...
set "ConnectionStrings__DefaultConnection=Server=%DETECTED_INSTANCE%;Database=Final_DB;Trusted_Connection=True;TrustServerCertificate=True;"
start /B dotnet run --no-build --configuration Release >"%TEMP%\backend.log" 2>&1

REM Get backend PID
set "BACKEND_PID="
for /f "tokens=2" %%A in ('tasklist ^| findstr dotnet') do (
    set "BACKEND_PID=%%A"
)

echo Backend started (PID: %BACKEND_PID%)
timeout /t 3 /nobreak

echo.
echo ========== STEP 3: SETUP FRONTEND ==========
echo.

cd /d "%FRONTEND_DIR%"
echo Current directory: %cd%

REM Check if .env exists, if not copy from .env.example
if not exist ".env" (
    echo Creating .env from .env.example...
    copy ".env.example" ".env" >nul
    echo .env created
) else (
    echo .env already exists
)

REM Install dependencies
echo Installing frontend dependencies...
call npm install >"%TEMP%\npm_install.log" 2>&1
if !errorlevel! neq 0 (
    echo npm install failed
    goto frontend_error
)
echo npm install succeeded!

REM Start frontend dev server in detached background
echo Starting frontend dev server...
start /B cmd /c "npm run dev >"%TEMP%\frontend.log" 2>&1"

REM Get frontend PID (if possible)
set "FRONTEND_PID=node"
timeout /t 2 /nobreak

echo.
echo ========== STEP 4: VERIFY SERVICES ==========
echo.

REM Wait a moment for services to start
timeout /t 3 /nobreak

REM Check backend
echo Checking backend...
curl -s http://localhost:5226/swagger/index.html >nul 2>&1
if !errorlevel! equ 0 (
    echo ✓ Backend responding at http://localhost:5226
) else (
    echo Checking alternate health endpoint...
    curl -s http://localhost:5226/ >nul 2>&1
    if !errorlevel! equ 0 (
        echo ✓ Backend responding at http://localhost:5226
    ) else (
        echo ✗ Backend not responding yet
    )
)

REM Check frontend
echo Checking frontend...
curl -s http://localhost:5173/ >nul 2>&1
if !errorlevel! equ 0 (
    echo ✓ Frontend responding at http://localhost:5173
) else (
    echo ✗ Frontend not responding yet
)

echo.
echo ========== STEP 5: OPEN BROWSER ==========
echo.

start http://localhost:5173

echo Browser opened!

echo.
echo ========== SETUP COMPLETE ==========
echo.
echo Configuration:
echo   Detected SQL Instance: %DETECTED_INSTANCE%
echo   Backend URL: http://localhost:5226
echo   Frontend URL: http://localhost:5173
echo   Backend PID: %BACKEND_PID%
echo   Backend Log: %TEMP%\backend.log
echo   Frontend Log: %TEMP%\frontend.log
echo.

REM Save report
(
    echo Detected SQL Instance: %DETECTED_INSTANCE%
    echo Backend URL: http://localhost:5226
    echo Frontend URL: http://localhost:5173
    echo Backend PID: %BACKEND_PID%
    echo Backend Log: %TEMP%\backend.log
    echo Frontend Log: %TEMP%\frontend.log
) > "%TEMP%\setup_report.txt"

echo Report saved to: %TEMP%\setup_report.txt

goto end

:backend_error
echo ERROR: Backend setup failed
goto end

:frontend_error
echo ERROR: Frontend setup failed
goto end

:end
endlocal
