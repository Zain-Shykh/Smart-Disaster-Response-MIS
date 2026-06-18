# Comprehensive Testing Plan

Project: Smart Disaster Response MIS  
Date: April 24, 2026  
Scope: End-to-end verification of frontend, backend, and database behavior against Docs/PROJECT_STATEMENT.md requirements.

## 1. Goals

1. Verify every functional requirement in PROJECT_STATEMENT is implemented correctly.
2. Verify cross-layer behavior: frontend -> backend API -> database.
3. Verify non-functional expectations: concurrency, security, auditability, performance, and role isolation.
4. Provide repeatable execution guidance for smoke, regression, and release readiness.

## 2. Test Scope

In scope:
- Backend APIs and business rules.
- Frontend workflows and role-based screens.
- Database-triggered automation and data integrity behaviors.
- Reporting and analytics outputs.
- Approval-gated critical operations.

Out of scope:
- Production infrastructure hardening (WAF, CDN, cloud autoscaling).
- Third-party penetration testing.

## 3. Test Levels and Strategy

1. Unit tests
- Validate pure business logic and helper methods.
- Focus on ranking/prioritization calculations, validation helpers, and mapping logic.

2. Integration tests (backend)
- Validate controller endpoints, DTO validation, authorization, database writes, and transaction boundaries.
- Existing baseline: 54/54 tests passing.

3. Frontend component/integration tests
- Validate form behavior, loading/empty/error states, route guards, and role-based visibility.

4. End-to-end tests
- Validate key workflows by role from login to final state transitions.
- Include negative and forbidden-access paths.

5. Non-functional tests
- Performance/latency checks, concurrent update conflict checks, security checks, and audit trace checks.

## 4. Environments

1. Local developer environment
- Backend: ASP.NET Core 8
- Frontend: Vite React
- Database: SQL Server

2. Pre-release validation environment
- Same schema and seed profile as local.
- Controlled data reset between runs.

## 5. Test Data and Accounts

Required seeded roles:
- Administrator
- Emergency Operator
- Field Officer
- Warehouse Manager
- Finance Officer

Required data fixtures:
- Active and inactive disaster events.
- Citizens and emergency reports with mixed severities.
- Rescue teams with varying location and availability.
- Warehouses, resources, inventories near threshold.
- Hospitals with varying bed capacity and specialization.
- Donors, donations, expenses, and approval requests in different statuses.

## 6. Execution Cadence

1. Per commit
- Backend integration test run.
- Frontend build and lint checks.

2. Daily
- API smoke suite.
- UI smoke suite for login, dashboard, analytics, and one module per domain.

3. Pre-release
- Full regression by requirement matrix.
- Concurrency and latency benchmark rerun.
- Audit and approval traceability verification.

## 7. Requirement Traceability Matrix

### R1 Emergency Reporting
Backend tests:
- Create report with valid payload -> 201.
- Missing required fields -> 400.
- Filter by status/location/severity/date returns correct subsets.
- Prioritization endpoint recalculates score and label correctly.

Frontend tests:
- Submit emergency report form with valid and invalid payloads.
- Queue filters produce expected row sets.
- Status transitions update row values and badges.

Acceptance criteria:
- High-frequency inserts do not corrupt data.
- Status and priority are visible and update correctly.

### R2 Rescue Team Management
Backend tests:
- Team create/list/update endpoints enforce role and validation.
- Recommendation endpoint ranks by severity/proximity logic.
- Assignment transitions enforce allowed state changes.
- Team activity history and summary endpoints return consistent aggregates.

Frontend tests:
- Team registry create/edit/availability updates.
- Recommendation fetch + assignment flow.
- Activity log entry and timeline rendering.

Acceptance criteria:
- Assignment and status lifecycle is reliable and auditable.

### R3 Resource Management
Backend tests:
- Resource, warehouse, inventory CRUD and query filtering.
- Inventory update enforces optimistic concurrency token/version checks.
- Low-stock alerts generated when thresholds crossed.
- Allocation create and state transitions maintain stock consistency.

Frontend tests:
- Resource/warehouse/inventory forms.
- Alert board filtering.
- Allocation lifecycle controls.
- Inventory history and CSV export action.

Acceptance criteria:
- Warehouse-wise visibility is correct.
- Dispatched/consumed/stock values remain consistent.

### R4 Hospital Coordination
Backend tests:
- Hospital and patient/admission endpoints validate required fields.
- Auto-routing selects best hospital when capacity exists.
- Escalation path returned when no suitable capacity exists.
- Load-balancing fallback chain behaves as designed.

Frontend tests:
- Hospital registry and bed update workflows.
- Patient admission status updates.
- Manual route and auto-route UX with escalation display.

Acceptance criteria:
- Routing outcomes are deterministic and explainable.

### R5 Financial Management
Backend tests:
- Donor, donation, and expense endpoints validate and persist correctly.
- Donation and expense status transitions enforce allowed states.
- Audit records created for financial mutations.

Frontend tests:
- Donor and donation create/update flows.
- Expense create/update flows.
- Validation guardrails for critical forms.

Acceptance criteria:
- Financial operations are traceable and status-accurate.

### R6 High-Volume Transaction Processing
Backend tests:
- Concurrent updates on inventory/allocation/expense endpoints trigger conflict handling where expected.
- No duplicate or partial writes under parallel requests.

Frontend tests:
- UI displays conflict/problem details clearly for retry behavior.

Acceptance criteria:
- Race conditions are handled without silent data loss.

### R7 Secure Transaction Requirements (ACID)
Backend tests:
- Allocation + stock updates rollback on forced downstream failure.
- Assignment + availability updates rollback on forced failure.
- Financial operation rollback tests for multi-step flows.

Frontend tests:
- User sees error feedback when transaction fails and data remains unchanged after reload.

Acceptance criteria:
- No partial committed state in multi-step critical operations.

### R8 RBAC
Backend tests:
- Endpoint authorization matrix by role for all controllers.
- Forbidden role attempts return 403.
- Allowed role attempts succeed.

Frontend tests:
- Route guards block unauthorized routes.
- Navigation menu only shows authorized modules.
- Unauthorized page shown on blocked access.

Acceptance criteria:
- Fine-grained role isolation at both API and UI layers.

### R9 Approval-Based Workflow
Backend tests:
- Approval request create/read/decision/history endpoints.
- Pending -> Approved/Rejected transitions only by authorized actors.
- Gated operations blocked until approval states allow execution.

Frontend tests:
- Approval inbox filters and detail view.
- Decision action UX and timeline visibility.
- Dependent actions behave according to approval status.

Acceptance criteria:
- Approval state governs execution and full history is preserved.

### R10 Data Security and Privacy
Backend tests:
- Auth login success/failure and token issuance.
- Password storage format verification (PBKDF2 and migration path).
- Critical endpoint calls without token -> 401.
- Token with wrong role -> 403.

Frontend tests:
- Login and session bootstrap from me endpoint.
- Session-expired and unauthorized UX flows.

Acceptance criteria:
- Sensitive operations require valid authentication and authorization.

### R11 MIS Reporting and Analytics
Backend tests:
- Reports endpoints return complete and filtered aggregates:
  - incidents by location/type/prioritized
  - resource utilization and overview
  - financial summary
  - approvals summary
  - audit logs
- Response values reconcile with underlying transactional data.

Frontend tests:
- Dashboard analytics widgets render all sections.
- Filters update tables and KPI values.
- Drill-down table selection shows detail panel.
- Loading/empty/error states displayed correctly.

Acceptance criteria:
- Reporting is accurate, filterable, and actionable.

### R12 Audit and Monitoring
Backend tests:
- Triggered and API-generated logs include actor, table/action, timestamp, and payload snapshot where applicable.
- Approval history records all decisions.

Frontend tests:
- Compliance and analytics audit views filter and drill-down correctly.
- Latest activity snapshot updates after mutations.

Acceptance criteria:
- Full traceability for critical operations.

### R13 Advanced DB Behavior and Automation
Database tests:
- Trigger behavior tests for inventory, team status, audit, and business-rule enforcement.
- Negative inventory prevention tests.

View tests:
- Validate view outputs against equivalent base-table queries.
- Role-specific visibility validation where view abstraction is used.

Acceptance criteria:
- Trigger automation and view abstraction behave as designed.

### R14 Performance Optimization and Indexing
Database tests:
- Run benchmark scripts with and without indexes.
- Compare latency and execution plans for target queries.
- Record insert/update overhead observations.

Acceptance criteria:
- Benchmark evidence documents indexed vs non-indexed behavior and trade-offs.

### R15 Frontend Requirements
Frontend tests:
- Role-specific dashboard rendering and route access.
- Forms: emergency reporting, resource requests/allocations, financial entries.
- Interactive dashboards with tables/filters and drill-down.
- Near real-time simulation via refresh patterns and immediate mutation feedback.

Acceptance criteria:
- Complete usable UI by role with all core forms and dashboards.

### R16-R17-R19 Design and Documentation Requirements
Document verification:
- ERD, schema, normalization, design rationale, security notes, stress report, benchmark reports, and frontend evidence pack exist and align with implementation.

Acceptance criteria:
- Documentation set is coherent and matches implemented behavior.

### R18 Full-Fledged System Expectations
End-to-end tests:
- Cross-domain workflow scenario from report -> rescue -> resource allocation -> hospital routing -> finance -> approval -> audit trace.
- Verify validation and error handling at each stage.

Acceptance criteria:
- Frontend, backend, and database integration behaves as a complete system.

## 8. Backend Test Suite Checklist

1. Authentication and session
- Valid login, invalid login, inactive user login, token-required checks.

2. Authorization
- 401/403 checks for all major controller routes.

3. Validation
- DTO boundary and malformed payload checks for each create/update endpoint.

4. Business rules
- Status-transition guards, approval gating, recommendation ranking, routing fallback.

5. Concurrency
- Version conflict and parallel update tests.

6. Transactions
- Forced-failure rollback tests for allocation/assignment/financial multi-step flows.

7. Auditability
- Audit log and approval history correctness checks.

## 9. Frontend Test Suite Checklist

1. Auth and guards
- Login success/failure, bootstrap from me, unauthorized route handling.

2. Module workflows
- CRUD and transitions for each implemented module page.

3. UX reliability
- Form guardrails, loading/empty/error states, notification rendering.

4. Role views
- Navigation and dashboard variation by role.

5. Analytics and audit views
- Filters, tables, drill-down detail, and retry on API failure.

6. Responsive behavior
- Shell, cards, forms, and table actions on narrow widths.

## 10. Suggested Automation Stack

Backend:
- dotnet test for integration suites.
- SQL script execution for trigger/index/view benchmarks.

Frontend:
- Build validation via npm run build.
- E2E via Playwright or Cypress for role workflows and guard paths.

## 11. Entry and Exit Criteria

Entry criteria:
1. Backend and frontend build successfully.
2. Test database is reachable and seeded.
3. Required role accounts are available.

Exit criteria:
1. All critical and high-priority tests pass.
2. No open critical defects.
3. Requirement traceability matrix has no uncovered required item.
4. Regression summary and evidence artifacts are attached to release notes.

## 12. Deliverable Artifacts from Testing

1. Test execution log by run date.
2. Execution tracker sheet: `Docs/test_execution_sheet.md`.
3. Failed test report with defect IDs.
4. Requirement coverage report (this matrix + pass/fail status).
5. Performance benchmark outputs and comparison charts.
6. Final sign-off checklist for frontend, backend, and database.
