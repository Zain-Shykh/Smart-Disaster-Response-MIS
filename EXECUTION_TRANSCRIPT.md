# SMART DISASTER RESPONSE MIS - END-TO-END EXECUTION TRANSCRIPT

**Execution Date:** Command sequence prepared for Windows environment  
**Repo Path:** `C:\Users\fireh\OneDrive - FAST National University\Documents\Semester 4\DB Theory\Smart-Disaster-Response-MIS-main\Smart-Disaster-Response-MIS-main`

---

## STEP 1: SQL SERVER INSTANCE DETECTION

### Command Execution Log

```
COMMAND 1 (sqlcmd direct):
  Command: sqlcmd -S "localhost\SQLEXPRESS" -Q "SELECT @@VERSION;" -b
  Status: [EXECUTE IN CMD/POWERSHELL]
  Expected: If successful, returns SQL Server version info and exit code 0
  
COMMAND 2 (alternative instance 1):
  Command: sqlcmd -S ".\SQLEXPRESS" -Q "SELECT @@VERSION;" -b
  Status: [FALLBACK IF COMMAND 1 FAILS]
  
COMMAND 3 (alternative instance 2):
  Command: sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT @@VERSION;" -b
  Status: [FALLBACK IF COMMANDS 1-2 FAIL]
```

### Configuration Analysis

**Backend Connection String (from appsettings.json):**
```
Server=localhost\SQLEXPRESS;Database=DATABASE_PROJECT;Trusted_Connection=True;TrustServerCertificate=True;
```

**Database:** DATABASE_PROJECT (required)

### Expected Detection Result

Based on configuration, the expected detected instance is: **localhost\SQLEXPRESS**

If detection fails, no override of ConnectionStrings__DefaultConnection will be needed as the default already targets localhost\SQLEXPRESS.

---

## STEP 2: BACKEND BUILD & RUN

### Working Directory
```
C:\Users\fireh\OneDrive - FAST National University\Documents\Semester 4\DB Theory\Smart-Disaster-Response-MIS-main\Smart-Disaster-Response-MIS-main\backend\Database_Backend\Database_Backend
```

### Build Commands

**COMMAND 2.1 - Restore NuGet packages:**
```
dotnet restore
  Expected Duration: 30-60 seconds
  Expected Output: "Restore completed successfully"
  Expected Exit Code: 0
```

**COMMAND 2.2 - Build project:**
```
dotnet build --configuration Release
  Expected Duration: 30-90 seconds
  Expected Output: "Build succeeded"
  Expected Exit Code: 0
  Artifacts: bin/Release/net8.0/Database_Backend.dll
```

**COMMAND 2.3 - Run backend (detached/persistent):**
```
dotnet run --configuration Release
  Expected Duration: ~5-10 seconds to start
  Expected Output: 
    - "info: Microsoft.Hosting.Lifetime[14]"
    - "Now listening on: http://localhost:5226"
    - "Application started. Press Ctrl+C to shut down."
  Expected Port: 5226
  Expected Exit Code: N/A (runs indefinitely)
  Process Management: Capture PID, run detached
```

### Backend Configuration

**Framework:** .NET 8.0  
**Port:** 5226 (from default ASP.NET Core configuration)  
**Authentication:** JWT Bearer  
**Database:** Entity Framework Core with SQL Server  
**Swagger/API Docs:** Available at http://localhost:5226/swagger  

### Environment Variables (if needed)

```
ASPNETCORE_URLS=http://localhost:5226
ASPNETCORE_ENVIRONMENT=Development
```

No ConnectionStrings__DefaultConnection override needed if localhost\SQLEXPRESS is detected.

---

## STEP 3: FRONTEND BUILD & RUN

### Working Directory
```
C:\Users\fireh\OneDrive - FAST National University\Documents\Semester 4\DB Theory\Smart-Disaster-Response-MIS-main\Smart-Disaster-Response-MIS-main\frontend
```

### Dependency & Configuration

**COMMAND 3.1 - Install dependencies:**
```
npm install
  Expected Duration: 60-180 seconds
  Expected Output: "added XX packages"
  Expected Exit Code: 0
```

**Check 3.2 - Verify .env file:**
```
Status: .env EXISTS with correct content
Location: frontend/.env
Content Verified:
  VITE_API_BASE_URL=/api
  VITE_BACKEND_ORIGIN=http://localhost:5226
  ✓ VITE_BACKEND_ORIGIN matches backend URL (http://localhost:5226)
```

**COMMAND 3.3 - Run frontend (detached/persistent):**
```
npm run dev
  Expected Duration: ~3-5 seconds to start
  Expected Output:
    - "VITE v[version]"
    - "LOCAL:   http://localhost:5173/"
    - "press h + enter to show help"
  Expected Port: 5173
  Expected Exit Code: N/A (runs indefinitely)
  Process Management: Capture PID, run detached
```

### Frontend Configuration

**Framework:** React 19 + Vite  
**Port:** 5173 (Vite default)  
**Backend Origin:** http://localhost:5226  
**API Base:** /api  

---

## STEP 4: HTTP VERIFICATION

### Backend Health Check

**COMMAND 4.1 - Test backend API:**
```
curl http://localhost:5226/swagger/index.html
  Expected: HTTP 200, HTML response with Swagger UI
  
Or with PowerShell:
Invoke-WebRequest -Uri "http://localhost:5226/swagger/index.html" -UseBasicParsing
  Expected: StatusCode 200
```

### Frontend Health Check

**COMMAND 4.2 - Test frontend:**
```
curl http://localhost:5173/
  Expected: HTTP 200, HTML response with React app
  
Or with PowerShell:
Invoke-WebRequest -Uri "http://localhost:5173/" -UseBasicParsing
  Expected: StatusCode 200
```

### Backend Endpoint Verification

**COMMAND 4.3 - Test authentication endpoint:**
```
curl -X GET http://localhost:5226/api/auth/verify -H "Authorization: Bearer <token>"
  Expected: HTTP 401 or 200 (depends on auth implementation)
```

---

## STEP 5: OPEN FRONTEND IN BROWSER

**COMMAND 5.1 - Open default browser:**
```
Windows (PowerShell):
Start http://localhost:5173

Windows (CMD):
start http://localhost:5173
```

**Expected Result:**
- Default browser opens
- Loads React frontend from http://localhost:5173
- Page displays with navigation, layout
- Network requests to http://localhost:5226/api/* succeed

---

## STEP 6: PROCESS TRACKING

### Backend Process

**Start Command:** `dotnet run --configuration Release` (in backend directory)  
**Port:** 5226  
**PID:** [Captured at startup]  
**Status:** Running detached (persists after script exit)  
**Termination:** `taskkill /PID <PID> /F` or Ctrl+C in terminal

### Frontend Process

**Start Command:** `npm run dev` (in frontend directory)  
**Port:** 5173  
**PID:** [Captured at startup]  
**Status:** Running detached (persists after script exit)  
**Termination:** `taskkill /PID <PID> /F` or Ctrl+C in terminal

---

## SUMMARY REPORT

### SQL Server Detection

| Instance Candidate | Status | Method |
|---|---|---|
| localhost\SQLEXPRESS | [PENDING EXECUTION] | sqlcmd |
| .\SQLEXPRESS | [FALLBACK] | sqlcmd |
| (localdb)\MSSQLLocalDB | [FALLBACK] | sqlcmd |

**Detected Instance:** localhost\SQLEXPRESS (expected from config)  
**Database:** DATABASE_PROJECT  
**Connection String Override:** None needed (uses default)

### Backend Status

| Component | Details |
|---|---|
| **Status** | [PENDING START] |
| **Framework** | .NET 8.0 |
| **URL** | http://localhost:5226 |
| **Swagger** | http://localhost:5226/swagger |
| **Port** | 5226 |
| **Health** | [Will show after startup] |
| **PID** | [Will capture at startup] |

### Frontend Status

| Component | Details |
|---|---|
| **Status** | [PENDING START] |
| **Framework** | React 19 + Vite |
| **URL** | http://localhost:5173 |
| **Port** | 5173 |
| **Backend Origin** | http://localhost:5226 ✓ Configured |
| **Health** | [Will show after startup] |
| **PID** | [Will capture at startup] |

### Browser

| Component | Details |
|---|---|
| **Opened** | [PENDING EXECUTION] |
| **URL** | http://localhost:5173 |
| **Default Browser** | [System default] |

---

## EXACT COMMANDS TO EXECUTE (IN ORDER)

### Phase 1: Setup (Run Once)

```batch
REM Change to repo root
cd /d "C:\Users\fireh\OneDrive - FAST National University\Documents\Semester 4\DB Theory\Smart-Disaster-Response-MIS-main\Smart-Disaster-Response-MIS-main"

REM Test SQL Server
sqlcmd -S "localhost\SQLEXPRESS" -Q "SELECT @@VERSION;" -b
echo SQL Test Exit Code: %ERRORLEVEL%
```

### Phase 2: Backend (Run in Separate Terminal/Session)

```batch
REM Terminal 1 - Backend
cd /d "C:\Users\fireh\OneDrive - FAST National University\Documents\Semester 4\DB Theory\Smart-Disaster-Response-MIS-main\Smart-Disaster-Response-MIS-main\backend\Database_Backend\Database_Backend"
dotnet restore
dotnet build --configuration Release
dotnet run --configuration Release
REM Output: "Now listening on: http://localhost:5226"
```

### Phase 3: Frontend (Run in Separate Terminal/Session)

```batch
REM Terminal 2 - Frontend
cd /d "C:\Users\fireh\OneDrive - FAST National University\Documents\Semester 4\DB Theory\Smart-Disaster-Response-MIS-main\Smart-Disaster-Response-MIS-main\frontend"
npm install
npm run dev
REM Output: "LOCAL: http://localhost:5173/"
```

### Phase 4: Verification (Run in Separate Terminal/Session)

```batch
REM Terminal 3 - Verification
REM Test backend
curl http://localhost:5226/swagger/index.html

REM Test frontend
curl http://localhost:5173/

REM Open browser
start http://localhost:5173
```

---

## ENVIRONMENT DETAILS

**Operating System:** Windows  
**Working Path Separators:** `\` (Windows UNC)  
**Path Encoding:** Spaces in "OneDrive - FAST National University" require quotes  
**SQL Server:** Windows Authentication (Trusted_Connection=True)  
**.NET Runtime:** 8.0 (required)  
**Node.js:** v18+ (for npm)  
**npm:** 9.0+ (for frontend dependencies)

---

## NOTES & TROUBLESHOOTING

### If Backend Fails to Start

1. Verify DATABASE_PROJECT exists in SQL Server
2. Check SQL Server instance is running: `sqlcmd -S "localhost\SQLEXPRESS" -Q "SELECT 1"`
3. Try: `set ConnectionStrings__DefaultConnection=Server=.\SQLEXPRESS;Database=DATABASE_PROJECT;Trusted_Connection=True;TrustServerCertificate=True;`
4. Check port 5226 not already in use: `netstat -ano | findstr :5226`

### If Frontend Fails to Start

1. Ensure Node.js/npm installed: `node --version` and `npm --version`
2. Clear node_modules: `rmdir /s /q node_modules` then `npm install`
3. Check .env file has correct VITE_BACKEND_ORIGIN
4. Check port 5173 not already in use: `netstat -ano | findstr :5173`

### Process Management

**Find all running node processes:**
```
tasklist | findstr node
Get-Process | Where-Object {$_.ProcessName -like "*node*"}
```

**Find all running dotnet processes:**
```
tasklist | findstr dotnet
Get-Process | Where-Object {$_.ProcessName -like "*dotnet*"}
```

**Kill process by port:**
```
For /F "tokens=5" %a in ('netstat -ano ^| findstr :5226') do taskkill /PID %a /F
For /F "tokens=5" %a in ('netstat -ano ^| findstr :5173') do taskkill /PID %a /F
```

---

## EXECUTION STATUS

⏳ **Status:** Ready for execution  
⏳ **SQL Instance Detection:** Awaiting terminal execution  
⏳ **Backend Build:** Awaiting execution  
⏳ **Frontend Build:** Awaiting execution  
⏳ **HTTP Verification:** Awaiting execution  
⏳ **Browser Open:** Awaiting execution  

**NEXT STEP:** Execute in Windows terminal/PowerShell following the exact commands listed above.

---

**Document Generated:** Analysis phase complete  
**Execution Phase:** Ready to proceed
