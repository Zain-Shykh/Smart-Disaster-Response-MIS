# Smart Disaster Response MIS - Setup Guide

This README is a teammate-focused setup guide for running the project locally.

## 0. Fast First-Run Checklist (New Laptop)

1. Clone the repository.
2. Run SQL scripts (including `bootstrap_auth_seed.sql`).
3. Update backend connection string in `appsettings.json`.
4. Create frontend `.env` from `.env.example`.
5. Run backend, then run frontend.
6. Log in with one of the bootstrap accounts below.

If you follow this checklist in order, the project should run on a fresh machine.

## 1. Project Structure

```text
project/
  backend/
    Database_Backend/
      Database_Backend.sln
      Database_Backend/
        Database_Backend.csproj
        Program.cs
        appsettings.json
        Properties/launchSettings.json
  frontend/
    package.json
  SQL_SCRIPTS/
    DDL.sql
    bootstrap_auth_seed.sql
    views.sql
    triggers.sql
    indexes.sql
    Drop_indexes.sql
    testing_Triggers.sql
    transaction_demos.sql
    performance_benchmarks.sql
```

## 2. Prerequisites

Install these before setup:

- .NET SDK 8.0 (for `net8.0` backend)
- Node.js 18+ and npm
- SQL Server (LocalDB / SQL Express / Developer edition)
- SQL Server Management Studio (SSMS) (recommended)
- Visual Studio 2022 (optional, for backend run/debug)

## 3. Backend Dependencies (NuGet)

From `backend/Database_Backend/Database_Backend/Database_Backend.csproj`:

- `Microsoft.AspNetCore.Authentication.JwtBearer` `8.0.10`
- `Microsoft.EntityFrameworkCore.Design` `8.0.10`
- `Microsoft.EntityFrameworkCore.SqlServer` `8.0.10`
- `Microsoft.EntityFrameworkCore.Tools` `8.0.10`
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` `8.0.6`
- `Swashbuckle.AspNetCore` `6.6.2`

## 4. Frontend Dependencies (NPM)

From `frontend/package.json`:

### Runtime dependencies
- `axios` `^1.15.2`
- `bootstrap` `^5.3.8`
- `react` `^19.2.5`
- `react-dom` `^19.2.5`
- `react-router-dom` `^7.14.2`

### Dev dependencies
- `@eslint/js` `^10.0.1`
- `@types/react` `^19.2.14`
- `@types/react-dom` `^19.2.3`
- `@vitejs/plugin-react` `^6.0.1`
- `eslint` `^10.2.1`
- `eslint-plugin-react-hooks` `^7.1.1`
- `eslint-plugin-react-refresh` `^0.5.2`
- `globals` `^17.5.0`
- `vite` `^8.0.10`

## 5. SQL Setup (Run Scripts)

Use the scripts inside `SQL_SCRIPTS/`.

### Recommended execution order (fresh setup)

1. `DDL.sql`
2. `bootstrap_auth_seed.sql` (**required for login + role access**)
3. `views.sql`
4. `triggers.sql`
5. `indexes.sql`
6. `testing_Triggers.sql` (optional validation)
7. `transaction_demos.sql` (optional demo)
8. `performance_benchmarks.sql` (optional benchmark)

`Drop_indexes.sql` is only for performance comparison/reset scenarios.

### Option A: Run in SSMS

1. Open SSMS and connect to your SQL Server instance.
2. Open each script from `SQL_SCRIPTS/` and execute in the order above.

Note:
- `DDL.sql` already creates `Final_DB`.
- `bootstrap_auth_seed.sql` creates default roles/users and role mappings.

### Option B: Run with sqlcmd (Windows)

```powershell
cd C:\Users\nepra\Desktop\project\SQL_SCRIPTS

sqlcmd -S <YOUR_SERVER> -d master -E -i DDL.sql
sqlcmd -S <YOUR_SERVER> -d Final_DB -E -i bootstrap_auth_seed.sql
sqlcmd -S <YOUR_SERVER> -d Final_DB -E -i views.sql
sqlcmd -S <YOUR_SERVER> -d Final_DB -E -i triggers.sql
sqlcmd -S <YOUR_SERVER> -d Final_DB -E -i indexes.sql
```

Notes:
- Replace `<YOUR_SERVER>` with your instance name (example: `LAPTOP-XXXX\\SQLEXPRESS`).
- Use SQL auth if needed:

```powershell
sqlcmd -S <YOUR_SERVER> -d Final_DB -U <USER> -P <PASSWORD> -i DDL.sql
```

## 5.1 Bootstrap Login Accounts

After running `bootstrap_auth_seed.sql`, these accounts are available:

- `admin1` / `Admin@1234` (Administrator)
- `ops2` / `Ops@1234` (EmergencyOperator)
- `field1` / `Field@1234` (FieldOfficer)
- `warehouse1` / `Warehouse@1234` (WarehouseManager)
- `finance1` / `Finance@1234` (FinanceOfficer)

Security note:
- These are bootstrap credentials for local setup only.
- Change them after first successful login in shared/team environments.

## 6. Backend Configuration (Connection String)

Connection string location:

- File: `backend/Database_Backend/Database_Backend/appsettings.json`
- JSON path: `ConnectionStrings:DefaultConnection`

Current sample value:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=Final_DB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### How to update for a new local SSMS/SQL instance

Replace only the `Server=` value (and optionally database/auth options).

Example for another SQL Express instance:

```json
"DefaultConnection": "Server=MY-PC\\SQLEXPRESS;Database=Final_DB;Trusted_Connection=True;TrustServerCertificate=True;"
```

Example using SQL authentication:

```json
"DefaultConnection": "Server=MY-PC\\SQLEXPRESS;Database=Final_DB;User Id=sa;Password=YourStrongPassword;TrustServerCertificate=True;"
```

## 7. Run the Backend

### Terminal method

```powershell
cd C:\Users\nepra\Desktop\project\backend\Database_Backend\Database_Backend

dotnet restore
dotnet run
```

Expected dev URL from `Properties/launchSettings.json`:
- `http://localhost:5226`
- `https://localhost:7004`

Swagger should open at:
- `http://localhost:5226/swagger`

### Visual Studio method

1. Open `backend/Database_Backend/Database_Backend.sln`.
2. Set startup project to `Database_Backend`.
3. Select profile `http` or `https`.
4. Press F5 (debug) or Ctrl+F5 (run).

## 8. Run the Frontend

### 8.1 Configure frontend environment

Create `frontend/.env` from the example file:

```powershell
cd C:\Users\nepra\Desktop\project\frontend
copy .env.example .env
```

`frontend/.env.example` contains:

```env
VITE_API_BASE_URL=/api
VITE_BACKEND_ORIGIN=http://localhost:5226
```

If your backend runs on a different port/host, update `VITE_BACKEND_ORIGIN` in `frontend/.env`.

```powershell
cd C:\Users\nepra\Desktop\project\frontend

npm install
npm run dev
```

Default Vite URL:
- `http://127.0.0.1:5173`

Optional checks:

```powershell
npm run build
npm run preview
```

## 9. Daily Startup Order

1. Ensure SQL Server instance is running.
2. Start backend (`dotnet run`).
3. Start frontend (`npm run dev`).
4. Open frontend URL and log in.

## 10. Common Issues

### Backend cannot connect to DB
- Verify `ConnectionStrings:DefaultConnection` in `appsettings.json`.
- Confirm SQL Server instance name is correct.
- Confirm target DB exists and scripts were executed.

### SSL/certificate warning with sqlcmd
If needed, add `-C`:

```powershell
sqlcmd -S <YOUR_SERVER> -d DATABASE_PROJECT -E -C -i DDL.sql
```

### Frontend cannot reach API
- Confirm backend is running on `http://localhost:5226`.
- Confirm `frontend/.env` exists.
- Confirm `VITE_BACKEND_ORIGIN` in `frontend/.env` matches backend URL.
- Restart `npm run dev` after changing `.env`.

### Login fails on fresh database
- Ensure `bootstrap_auth_seed.sql` was executed successfully.
- Verify rows exist in `[Role]`, `[User]`, and `UserRole` tables.
- Re-run:

```powershell
cd C:\Users\nepra\Desktop\project\SQL_SCRIPTS
sqlcmd -S <YOUR_SERVER> -d DATABASE_PROJECT -E -i bootstrap_auth_seed.sql
```

## 11. Quick Command Reference

```powershell
# Backend
cd C:\Users\nepra\Desktop\project\backend\Database_Backend\Database_Backend
dotnet restore
dotnet run

# Frontend
cd C:\Users\nepra\Desktop\project\frontend
npm install
npm run dev

# Frontend production check
npm run build
```
