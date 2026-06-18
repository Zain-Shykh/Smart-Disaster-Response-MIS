# Quick Start Guide - Final_DB Migration Complete ✅

## 🚀 TL;DR - Just Run These Commands

### Terminal 1: Execute Database Scripts
```powershell
cd NEW_DB
sqlcmd -S localhost\SQLEXPRESS -d master -E -i DDL.sql
sqlcmd -S localhost\SQLEXPRESS -d Final_DB -E -i bootstrap_auth_seed.sql
```

### Terminal 2: Backend
```powershell
cd backend\Database_Backend\Database_Backend
dotnet restore
dotnet run
```

### Terminal 3: Frontend
```powershell
cd frontend
npm install
npm run dev
```

### Browser
Open: `http://localhost:5173`

---

## 🔐 Login (Use Any of These)

```
Username: admin1          | Password: Admin@1234
Username: ops2            | Password: Ops@1234
Username: field1          | Password: Field@1234
Username: warehouse1      | Password: Warehouse@1234
Username: finance1        | Password: Finance@1234
```

---

## ✅ What Changed

- **OLD Database**: `DATABASE_PROJECT`
- **NEW Database**: `Final_DB` ✨

Everything else works exactly the same!

---

## 📍 Key Locations

| Item | Location |
|------|----------|
| Backend Config | `backend/Database_Backend/Database_Backend/appsettings.json` |
| Database Scripts | `NEW_DB/` (recommended) or `SQL_SCRIPTS/` (legacy) |
| Frontend Config | `frontend/.env` |
| Documentation | `README.md`, `how to run.txt` |

---

## 🆘 Quick Troubleshoot

**Backend won't start?**
- Check SQL Server is running
- Verify `Final_DB` exists
- Check connection string in appsettings.json

**Frontend won't connect?**
- Ensure backend is running on http://localhost:5226
- Check `frontend/.env` VITE_BACKEND_ORIGIN

**Login fails?**
- Verify bootstrap_auth_seed.sql was executed
- Try a different credential from the list above

---

## 📚 Full Documentation

- **Migration Details**: `DATABASE_MIGRATION_REPORT.md`
- **Complete Checklist**: `MIGRATION_CHECKLIST.md`
- **Full Setup Guide**: `README.md`
- **How to Run**: `how to run.txt`

---

**Status**: ✅ Ready to Run | **Database**: Final_DB | **Updated**: 2026-05-04
