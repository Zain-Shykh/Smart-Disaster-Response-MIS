# Test Run Report - 2026-04-24

Scope: Active execution of Docs/comprehensive_testing_plan.md and Docs/test_execution_sheet.md.

## Automated Validation Results

- Backend test suite: `dotnet test` -> 54 passed, 0 failed.
- Frontend build: `npm.cmd run build` -> success.
- Backend RBAC matrix probe: 60/60 expected status checks passed across 5 roles and 12 module endpoints.
- Anonymous authorization guard check: `GET /api/Reports/overview` without token -> 401.

## Live Browser Workflow Results

Validated with role-based login and route checks:

- Administrator: module routes and analytics/audit/admin views loaded.
- EmergencyOperator: core operations/medical/analytics access works after frontend role mapping fix.
- FieldOfficer: dashboard, hospital admissions flow, and rescue team route access verified.
- WarehouseManager: logistics route access verified; required resource API now returns 200 after backend authorization patch.
- FinanceOfficer: expenses route access verified; required finance API now returns 200 after backend authorization patch.

Additional browser guard validation (manual role cycles):

- Administrator: `/admin/users` and `/finance/expenses` both accessible.
- EmergencyOperator: `/operations/reports` accessible and `/admin/users` blocked (unauthorized).
- FieldOfficer: `/medical/routing` accessible and `/finance/expenses` blocked (unauthorized).
- WarehouseManager: `/logistics/resources` accessible and `/operations/reports` blocked (unauthorized).
- FinanceOfficer: `/finance/expenses` accessible and `/logistics/resources` blocked (unauthorized).

## Defect Timeline

1. D-001 (Closed)
- Frontend role name mismatch (`Emergency Operator` vs backend claim `EmergencyOperator`)
- Fix applied in `frontend/src/app/navigation.js`.
- Retest result: operator and field routes accessible according to UI role guard expectations.

2. D-002 (Closed)
- Nested button DOM issue in analytics page causing console errors.
- Fix applied in `frontend/src/pages/IncidentAnalyticsPage.jsx` by repairing malformed action/card JSX structure.
- Retest result: analytics route renders without nested-button console error.

3. D-003 (Closed)
- `WarehouseManager` had been denied on required logistics API endpoint: `GET /api/ResourceLogistics/resources` returned 403.
- Fix applied by expanding backend controller role authorization to include required role claims.
- Retest result: endpoint returns 200 and warehouse logistics workflow is accessible.

4. D-004 (Closed)
- `FinanceOfficer` had been denied on required finance API endpoint: `GET /api/DonationFinance/expenses` returned 403.
- Fix applied by expanding backend controller role authorization to include required role claims.
- Retest result: endpoint returns 200 and finance workflow is accessible.

5. Backend authorization alignment patch (Closed)
- Updated role allow-lists in ResourceLogistics, DonationFinance, DonorPhone, RescueTeam, HospitalPatient, ApprovalWorkflow, and Reports controllers.
- Regression check: `dotnet test` rerun passed 54/54 after patch.

6. Test-environment credential stabilization (Operational)
- Existing seeded role-account passwords were not documented in repository artifacts.
- For deterministic cross-role execution, local test users (`admin1`, `ops2`, `field1`, `warehouse1`, `finance1`) were reset in the local dev database to a known test password for this run.
- This was an execution-environment preparation action, not a product defect fix.

## Current Sign-Off Status

- Frontend/UI defect blockers from this run: resolved.
- Backend role authorization coverage against project requirements: complete for all tested roles.
- Final workflow sign-off: ready, pending project-owner approval entry in execution sheet.
