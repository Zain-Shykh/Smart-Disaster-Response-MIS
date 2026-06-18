#!/usr/bin/env pwsh
<#
Runs the backend project from the solution folder.
Usage: in PowerShell run this file from backend\Database_Backend: `./run-backend.ps1`
#>
$proj = Join-Path $PSScriptRoot 'Database_Backend\Database_Backend.csproj'
Write-Host "Using project: $proj"
dotnet restore "$proj"
dotnet run --project "$proj"
