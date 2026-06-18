param(
    [string]$Server = "localhost\SQLEXPRESS"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$bootstrapSql = Join-Path $root "NEW_DB\bootstrap_auth_seed.sql"
$seedSql = Join-Path $root "NEW_DB\seed_donations_expenses_approvals.sql"

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "sqlcmd not found. Install SQL Server Command Line Utilities and rerun this script."
}

if (-not (Test-Path $bootstrapSql)) {
    throw "Missing file: $bootstrapSql"
}

if (-not (Test-Path $seedSql)) {
    throw "Missing file: $seedSql"
}

Write-Host "[1/2] Applying bootstrap auth seed on server: $Server"
sqlcmd -S $Server -E -d master -b -i $bootstrapSql

Write-Host "[2/2] Applying Pakistani finance and approval seed on server: $Server"
sqlcmd -S $Server -E -d master -b -i $seedSql

Write-Host "[OK] Seed completed successfully."
Write-Host "You can now run backend and frontend, then open Dashboard and Approval Workflow."
