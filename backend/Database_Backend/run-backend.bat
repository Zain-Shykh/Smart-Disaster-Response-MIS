@echo off
:: Run backend project from the solution folder
:: Usage: run this from backend\Database_Backend: run-backend.bat
@echo Using project: %~dp0Database_Backend\Database_Backend.csproj
dotnet restore "%~dp0Database_Backend\Database_Backend.csproj"
dotnet run --project "%~dp0Database_Backend\Database_Backend.csproj"