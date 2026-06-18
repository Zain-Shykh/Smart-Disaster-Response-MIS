# Smart Disaster Response MIS Implementation Plan

**Last Updated:** April 23, 2026  
**Status:** Database and backend requirements closed; frontend planning completed and implementation in progress.

## Current State

- ✅ **Database Schema:** Created, tested, constraints enforced
- ✅ **Database Triggers:** Fully implemented and validated
- ✅ **Backend Solution:** ASP.NET Core 8.0 with Entity Framework Core 8.0
- ✅ **Authentication & RBAC:** JWT login + role-based controller access, globally hardened with ProblemDetails error responses
- ✅ **9 Operational Controllers:** DisasterEvent, EmergencyReport, RescueTeam, ResourceLogistics, HospitalPatient, DonationFinance, ApprovalWorkflow, Auth, Reports
- ✅ **Input Validation:** Data annotations on all request DTOs; global validation error factory
- ✅ **Integration Tests:** 54/54 passing (auth, RBAC, validation, user admin, role/permission admin, team activity, phone management, inventory history, hospital routing, incident prioritization, approval-gating, optimistic concurrency, transaction-scope atomicity checks, rescue recommendation ranking, hospital load-balancing fallback/escalation, password hash migration/format checks)
- ✅ **Remaining Statement-Level DB/Backend Tasks:** Closed
- 🔄 **Frontend:** In progress (see `Docs/frontend_implementation_plan.md`)

## Frontend Execution Tracker

- Canonical frontend plan document: `Docs/frontend_implementation_plan.md`
- Current active step: Frontend implementation complete
- Update policy: After each frontend step, update statuses and evidence in the frontend plan doc before moving to the next step.

## Implementation Principles

- Each feature is tied to a specific gap in the use-case coverage.
- All changes are validated with new integration tests before marking complete.
- Transactional consistency and approval-gating are enforced at the API layer.
- Concurrency control and optimistic locking prevent lost updates in high-contention scenarios.

---

## Project Statement Gap Board (Database + Backend Pre-Frontend Gate)

This section maps current implementation against `Docs/PROJECT_STATEMENT.md` and lists what is complete vs incomplete for database and backend work.

### Database Workstream

- ✅ **DB-01 Complete:** Core schema, keys, constraints, and trigger automation are implemented (`SQL_SCRIPTS/DDL.sql`, `SQL_SCRIPTS/triggers.sql`, `SQL_SCRIPTS/testing_Triggers.sql`).
- ✅ **DB-02 Complete:** ER design and relational schema documentation exist (`Docs/ER_Design.md`, `Docs/RELATIONAL_SCHEMA.txt`).
- ✅ **DB-03 Complete:** Views created and executed via `SQL_SCRIPTS/views.sql`.
- ✅ **DB-04 Complete:** Latency benchmark execution documented in `Docs/database_latency_benchmark_report.md`.
- ✅ **DB-05 Complete:** Custom indexes created and executed via `SQL_SCRIPTS/indexes.sql`.
- ✅ **DB-06 Complete:** Explicit WITHOUT_INDEX vs WITH_INDEX benchmark comparison documented in `Docs/database_latency_benchmark_report.md`.
- ✅ **DB-07 Complete:** Dedicated benchmark/query pack now added via `SQL_SCRIPTS/performance_benchmarks.sql`.
- ✅ **DB-08 Complete:** Transaction demonstrations executed successfully and recorded in `Docs/database_latency_benchmark_report.md`.
- ✅ **DB-09 Complete:** Normalization steps document added at `Docs/NORMALIZATION_STEPS.md`.

### Backend Workstream

- ✅ **BE-01 Complete:** Authentication, RBAC, controller coverage, approval gating, and optimistic concurrency are implemented and tested.
- ✅ **BE-02 Complete:** Multi-step approval workflows now run with explicit transaction scopes for relational providers.
- ✅ **BE-03 Complete:** Transactional unit-of-work handling and atomicity validation tests added for allocation/assignment/expense failure paths.
- ✅ **BE-04 Complete:** Rescue assignment recommender endpoint implemented in `RescueTeamController` with proximity + severity weighted ranking and integration tests.
- ✅ **BE-05 Complete:** Hospital auto-routing now supports best-hospital selection with load-balancing, city/province/global fallback, and escalation responses when no suitable capacity exists.
- ✅ **BE-06 Complete:** Password hashing upgraded to PBKDF2 with legacy hash migration on login; security notes documented in `Docs/backend_security_notes.md`.
- ✅ **BE-07 Complete:** Backend stress testing approach, scenarios, metrics, and integrity checks documented in `Docs/backend_stress_testing_report.md`.

### One-by-One Execution Queue (Do This Before Frontend)

No remaining DB/backend closure items.

Backend workstream completion status: all planned backend tasks are complete.

Frontend start gate: satisfied.

---

## Completed Phases (1–8)

### Phase 1: Database Foundation ✅
- SQL Server schema created with all tables, foreign keys, constraints, and indexes.
- Five system roles (Administrator, Emergency Operator, Field Officer, Warehouse Manager, Finance Officer) seeded.
- Permissions mapped to roles through `RolePermission`.

### Phase 2: Database Automation ✅
- Triggers installed for report counts, team availability, inventory alerts, bed management, approval history, and audit logging.
- All trigger behavior validated.

### Phase 3: Backend Solution Setup ✅
- ASP.NET Core 8.0 with Entity Framework Core 8.0 configured.
- Connection to SQL Server established; migrations in place.
- EF models aligned with database schema.

### Phase 4: Authentication and RBAC ✅
- JWT-based login endpoint in `AuthController`.
- Class-level `[Authorize(Roles = "...")]` attributes on all operational controllers.
- Global error middleware with standardized ProblemDetails responses.
- Input validation via data annotations on all request DTOs.

### Phase 5: Core Operational Modules ✅
- **DisasterEventController:** Create, list, update disaster events.
- **EmergencyReportController:** Report submission, listing, filtering by status and location.
- **RescueTeamController:** Team registration, location updates, status and specialization tracking.
- **ResourceLogisticsController:** Resource, warehouse, inventory, and allocation management.
- **HospitalPatientController:** Hospital, patient, bed availability, and admission tracking.
- **DonationFinanceController:** Donor, donation, and expense recording.
- **ApprovalWorkflowController:** Create and retrieve approval requests.
- **RbacController:** Query roles and permissions.
- **ReportsController:** MIS dashboards and summaries.

### Phase 6: Approval Workflows (Partial) ✅
- Approval request creation and state tracking implemented.
- Approval history triggered on state changes.
- **Gap identified:** Approval-gating enforcement (blocking mutations until approval) not wired to dependent actions—to be addressed in Phase 9.

### Phase 7: Reporting and Audit ✅
- **ReportsController** provides MIS dashboards: incident summary, resource allocation status, financial summaries, hospital occupancy.
- **AuditLog** triggers capture all donation and expense mutations.

### Phase 8: Validation and Testing ✅
- Integration test suite (43/43 passing) validates authentication, RBAC enforcement, input validation, standardized errors, user admin, role/permission management, team activity APIs, phone management APIs, inventory history APIs, hospital routing APIs, incident prioritization APIs, and approval-gating enforcement.
- All 9 controllers hardened with role checks and validation.

---

## Phase 9 Delivery Summary (9 Targeted Features)

**9 Features Total: 9 Complete ✅ | 0 Remaining**

### Feature 1: User Management CRUD (Admin) ✅ COMPLETE

**Status:** ✅ Complete (16/16 tests passing)

**What was implemented:**
- `UserController` with 5 endpoints: Create, List, GetById, Update, Delete (soft delete)
- Data validation on all DTOs: `CreateUserRequest`, `UpdateUserRequest`
- Role-based access control: `[Authorize(Roles = "Administrator")]` on all endpoints
- Pagination support on the List endpoint (pageNumber, pageSize, roleId filter)
- Password hashing using SHA256
- Comprehensive integration test suite:
  - ✅ CreateUser_WithValidPayload_ReturnsCreated
  - ✅ CreateUser_WithDuplicateUsername_ReturnsBadRequest
  - ✅ CreateUser_WithDuplicateEmail_ReturnsBadRequest
  - ✅ CreateUser_WithInvalidEmail_ReturnsBadRequest
  - ✅ CreateUser_WithoutAdminRole_ReturnsForbidden
  - ✅ GetUsers_WithAdminRole_ReturnsUsersList
  - ✅ GetUsers_WithoutAdminRole_ReturnsForbidden
  - ✅ GetUserById_WithValidId_ReturnsUserDetails
  - ✅ GetUserById_WithInvalidId_ReturnsNotFound
  - ✅ UpdateUser_WithValidPayload_ReturnsUpdatedUser
  - ✅ DeleteUser_WithValidId_ReturnNoContent

**Test coverage:** Validation (400), Authorization (403), Success (200/201), Not Found (404)

---

### Feature 2: Role and Permission Admin APIs

**Status:** ✅ Complete (28/28 tests passing)

**Goal:** Allow administrators to manage roles and permissions dynamically at runtime.

**Use Cases:**
- Create custom roles
- Update role description
- Define permissions for modules/actions
- Map permissions to roles and vice versa

**What was implemented:**
1. Created `RoleController` with:
   - `POST /api/Role` – Create role
   - `GET /api/Role` – List roles
   - `GET /api/Role/{id}` – Get role details
   - `PUT /api/Role/{id}` – Update name/description
2. Created `PermissionController` with:
   - `GET /api/Permission` – List permissions (paginated)
   - `POST /api/Permission` – Create permission
   - `GET /api/Permission/{id}` – Get permission details
3. Extended `RbacController` with:
   - `POST /api/Rbac/role-permission` – Map permission to role
   - `DELETE /api/Rbac/role-permission/{roleId}/{permissionId}` – Unmap
4. Added uniqueness validation for role names and permission names
5. Added integration tests for:
   - Role creation/list/update
   - Permission creation/list
   - Role-permission map/unmap
   - Non-admin access denial

**Test coverage:** Validation (400), Authorization (403), Success (200/201), Not Found (404), Conflict-safe mapping flows

---

### Feature 3: Field Activity Logging (TeamActivity API)

**Status:** ✅ Complete (28/28 tests passing)

**Goal:** Allow field officers to log team activities (completed tasks, status updates, location check-ins).

**Use Cases:**
- Log a team activity with time, location, and status
- Retrieve activity history for a team
- Filter activity by date range
- Track team movements and task completion

**What was implemented:**
1. Created `TeamActivityController` with:
   - `POST /api/TeamActivity` – Log activity (time, location, activity type, notes)
   - `GET /api/TeamActivity?teamId={id}&startDate={date}&endDate={date}` – Retrieve history
   - `GET /api/TeamActivity/summary/{teamId}` – Completed/pending task counts
2. Added role-based access:
   - POST restricted to `EmergencyOperator,FieldOfficer`
   - GET/Summary allowed for `Administrator,EmergencyOperator,FieldOfficer`
3. Added DTO validation:
   - `[Required]` TeamId, ActivityType, Timestamp
   - `[MaxLength(500)]` Notes
4. Added business checks:
   - Team must exist before activity can be logged
   - Start/end time cannot be in the future
   - End time cannot be earlier than start time
5. Added 3 integration tests:
   - CreateTeamActivity_WithEmergencyOperator_ReturnsCreated
   - CreateTeamActivity_WithAdministrator_ReturnsForbidden
   - CreateTeamActivity_WithInvalidTeam_ReturnsNotFound

**Validation:**
- Field officer can log activity
- Non-field-officer gets 403 on POST
- Invalid TeamId returns 404
- Future dates rejected

**Test coverage:** Success (201), Authorization (403), Not Found (404), Date validation (400)

### Feature 4: Phone Number Management (Donor & User)

**Status:** ✅ Complete (32/32 tests passing)

**Goal:** Allow users to have multiple phone numbers with type (mobile, landline, emergency).

**Use Cases:**
- Add phone to user or donor
- Update phone details
- Deactivate/remove phone
- List phones for a person

**What was implemented:**
1. Created `UserPhoneController`:
   - `POST /api/User/{userId}/Phone` – Add phone
   - `GET /api/User/{userId}/Phone` – List phones
   - `PUT /api/User/{userId}/Phone/{phone}` – Update phone
   - `DELETE /api/User/{userId}/Phone/{phone}` – Remove phone
2. Created `DonorPhoneController`:
   - `POST /api/Donor/{donorId}/Phone` – Add phone
   - `GET /api/Donor/{donorId}/Phone` – List phones
   - `PUT /api/Donor/{donorId}/Phone/{phone}` – Update phone
   - `DELETE /api/Donor/{donorId}/Phone/{phone}` – Remove phone
3. Added validation:
   - `[Required][Phone][MaxLength(30)]` on phone fields
4. Added access-control rules:
   - `UserPhone`: admin or owner (same userId)
   - `DonorPhone`: administrator only
5. Added 4 integration tests combined
6. Implemented on current schema constraint:
   - Existing database has `UserPhone(UserId, Phone)` and `DonorPhone(DonorId, Phone)` only
   - Phone type / primary flags are not in current schema and can be added in a future migration if needed

**Validation:**
- User can only add/edit own phones; admin can edit any user phone
- Non-admin donor phone operations return 403
- Invalid phone format returns 400

**Test coverage:** Success (200/201/204), Authorization (403), Validation (400)

### Feature 5: Inventory Movement History

**Status:** ✅ Complete (34/34 tests passing)

**Goal:** Provide audit trail for all inventory changes (receipts, allocations, consumption, adjustments).

**Use Cases:**
- Query inventory movement ledger
- Track resource flow for a warehouse
- Generate stock reconciliation reports
- Export movement history for audit

**What was implemented:**
1. Created `InventoryHistoryController`:
   - `GET /api/InventoryHistory/inventory/{inventoryId}/history?startDate={date}&endDate={date}` – Movement ledger
   - `GET /api/InventoryHistory/warehouse/{warehouseId}/history` – Warehouse-level summary
   - `GET /api/InventoryHistory/inventory/{inventoryId}/history/export?format=csv` – CSV export
2. Implemented movement event stream from existing `ResourceAllocation` lifecycle timestamps:
   - Requested
   - Dispatched
   - Consumed
3. Added date-range validation and explicit 404 handling for missing inventory/warehouse
4. Added role checks for inventory history endpoints (Administrator / Warehouse Manager)
5. Added 2 integration tests:
   - `InventoryHistory_AdminCanReadAndExportCsv`
   - `InventoryHistory_NonAdminForbidden_AndDateRangeValidation`
6. Kept implementation schema-compatible:
   - No new table required for this slice
   - Uses existing `ResourceAllocation`, `Inventory`, `Warehouse`, and `Resource` relationships

**Validation:**
- Only warehouse manager and admin can query
- Date range validation (endDate >= startDate)
- Returns 404 if no inventory found

**Test coverage:** Success (200), Authorization (403), Validation (400), Not Found (404), CSV export content type

### Feature 6: Approval-Gating Enforcement

**Status:** ✅ Complete (43/43 tests passing)

**Goal:** Block mutations (resource allocation, expense record, team assignment) until approval is granted.

**Use Cases:**
- Create resource allocation request (pending approval)
- Lock allocation from dispatch until approved
- Create expense request (pending approval)
- Lock expense from payment until approved

**What was implemented:**
1. Resource allocation gating:
   - `ResourceAllocationCreateDto` now supports `RequiresApproval` and `ApprovalRequestedBy`
   - Creating gated allocations auto-creates `ApprovalRequest` of type `ResourceDistribution`
   - Dispatch/consume status transitions are blocked until approved request exists
2. Expense gating:
   - `ExpenseCreateDto` now supports `RequiresApproval` and `ApprovalRequestedBy`
   - Gated expenses start with payment status `PendingApproval`
   - Marking expense `Paid/Completed` is blocked until approved financial request exists
3. Team assignment gating:
   - `TeamAssignmentCreateDto` now supports `RequiresApproval` and `ApprovalRequestedBy`
   - Creating gated assignments auto-creates `ApprovalRequest` of type `RescueDeployment`
   - Assignment transitions beyond `Assigned` are blocked until approval
4. Approval decision propagation:
   - `ApprovalWorkflowController` now applies approval/rejection to linked targets
   - Approved resource requests unlock allocations (`Pending -> Approved`)
   - Approved financial requests unlock expenses (`PendingApproval -> Pending`)
   - Rejections map dependent records to rejected terminal statuses
5. Added 3 end-to-end integration tests:
   - `Allocation_DispatchBlockedUntilApproved`
   - `Expense_PaidBlockedUntilApproved`
   - `Assignment_StatusChangeBlockedUntilApproved`

**Validation:**
- Dispatch blocked if allocation status = Pending (returns 400)
- Only approver role can approve (returns 403 if not)
- Approval rejection surfaces error to original requester

**Test coverage:** Success after approval, pre-approval blocking (400), cross-controller workflow enforcement

### Feature 7: Optimistic Concurrency Control

**Status:** ✅ Complete (45/45 tests passing)

**Goal:** Prevent lost updates when multiple users edit the same record simultaneously.

**Use Cases:**
- Edit disaster event details while another admin edits it → conflict detected
- Update team assignment while dispatcher updates same team → conflict detected
- Modify inventory allocation while warehouse manager consumes it → conflict detected

**What was implemented:**
1. Added shared token helper:
   - `Services/ConcurrencyTokenService.cs`
   - Generates deterministic version tokens from high-contention entity snapshots
2. Added optimistic concurrency checks to update/patch flows:
   - `DisasterEventController` (full update + status update)
   - `RescueTeamController` (team update + availability update)
   - `ResourceLogisticsController` (inventory update + allocation status update)
   - `ApprovalWorkflowController` (approval decision update)
3. Extended response DTOs to return `VersionToken` and update DTOs to accept optional `VersionToken`
4. Added standardized 409 payloads that return the latest `CurrentVersionToken` for client retry behavior
5. Added 2 integration tests covering stale-token conflicts:
   - `DisasterEvent_UpdateWithStaleVersionToken_ReturnsConflict`
   - `ApprovalDecision_WithStaleVersionToken_ReturnsConflict`

**Validation:**
- Second update with stale token returns 409 with `CurrentVersionToken`
- Client can retry with refreshed token
- Existing clients remain compatible because token fields are optional

### Feature 8: Hospital Specialization Routing

**Status:** ✅ Complete (38/38 tests passing)

**Goal:** Intelligently route patients to hospitals based on required specialization.

**Use Cases:**
- Query hospitals with specific specialization (trauma, cardiac, pediatric)
- Find nearest hospital with available beds for specialization
- Route patient to hospital based on injury type and nearest match

**What was implemented:**
1. Extended `HospitalPatientController` with specialization/routing endpoints:
   - `POST /api/HospitalPatient/hospitals/{hospitalId}/specializations` – add specialization to hospital
   - `GET /api/HospitalPatient/hospitals/search?specialization={spec}&city={city?}&bedRequirement={n}` – specialization + beds search
   - `POST /api/HospitalPatient/hospitals/{hospitalId}/route-patient` – route and create admission
2. Added routing validation:
   - Required specialization must exist globally
   - Selected hospital must support requested specialization
   - Selected hospital must have available beds
3. Added role-protected behavior under existing hospital controller auth policy
4. Added 4 integration tests:
   - `HospitalSearch_BySpecialization_ReturnsMatchingHospitals`
   - `HospitalSearch_WithUnknownSpecialization_ReturnsBadRequest`
   - `RoutePatientToHospital_WithMatchingSpecialization_ReturnsCreated`
   - `RoutePatientToHospital_WithoutRequiredSpecialization_ReturnsNotFound`
5. Implemented with current schema constraints:
   - Current `Hospital` schema has no latitude/longitude fields
   - Search ranking is specialization + bed availability (and optional city), not geo-distance yet

**Validation:**
- Returns 400 if specialization not found
- Returns 404 if no hospitals match criteria
- Confirms beds available before admission

**Test coverage:** Success (200/201), Validation (400), Not Found (404)

### Feature 9: Incident Prioritization Logic

**Status:** ✅ Complete (40/40 tests passing)

**Goal:** Automatically calculate incident priority and reorder emergency response queue.

**Use Cases:**
- Assign priority based on severity, injured count, affected area
- Re-queue incidents as new data arrives
- Surface high-priority incidents to operator dashboard
- Recommend team dispatch based on incident priority

**What was implemented:**
1. Added shared priority service:
   - `Services/IncidentPriorityService.cs`
   - Computes `PriorityLevel`, `PriorityLabel`, `PriorityScore`, and `EstimatedResponseMinutes`
2. Extended `EmergencyReportController`:
   - `PUT /api/EmergencyReport/{id}/priority` to recalculate and return priority payload
   - Added priority fields to emergency report responses
   - Prioritizes report list ordering using calculated priority
3. Extended `ReportsController`:
   - `GET /api/Reports/incidents/prioritized?limit={n}` for prioritized queue output
4. Added test fixture seed data for `Citizen` and `DisasterEvent` to support report creation in integration tests
5. Added 2 integration tests:
   - `EmergencyReport_RecalculatePriority_ReturnsPriorityPayload`
   - `Reports_PrioritizedIncidents_ReturnsSortedByPriority`
6. Kept implementation schema-compatible for current stage:
   - Priority is computed dynamically in API/service layer
   - No emergency report schema migration required in this slice

**Validation:**
- New report auto-calculates priority
- Priority recalculated on severity/count changes
- Priority always in range [1, 4]

**Test coverage:** Success (200), Sorting behavior, Priority bounds [1..4]

---

## Integration and Testing Strategy

### For Each Feature:
1. **Schema updates** (if any): Migrate to SQL Server via EF Core migration or direct script
2. **Model/DTO updates:** Add fields, validation, concurrency tokens
3. **Controller implementation:** Endpoints with role checks and transactional handling
4. **Integration tests:** Success path, role denial (403), validation (400), resource not found (404), concurrency (409)
5. **Live endpoint verification:** Call endpoint with sample data; inspect response

### Testing Workflow:
- Run integration test suite after each feature
- Verify `dotnet build` passes
- Spot-check endpoints in live backend using test HTTP client
- All new tests must pass before marking feature complete

---

## Suggested Implementation Order

1. **User Management & RBAC Admin** (Foundation for governance)
2. **Field Activity Logging** (Easy win, no complex state)
3. **Phone Number Management** (Straightforward CRUD extension)
4. **Inventory Movement History** (Reporting/audit layer)
5. **Hospital Specialization Routing** (Business logic enhancement)
6. **Incident Prioritization** (Algorithm + reporting update)
7. **Approval-Gating Enforcement** (Complex cross-module changes)
8. **Optimistic Concurrency Control** (Advanced EF Core feature)

Order completed as planned.

---

## Final Acceptance Criteria

Completion checklist:

- ✅ All 9 remaining features have working endpoints
- ✅ Input validation blocks bad payloads with 400 + details
- ✅ RBAC enforces role checks with 403 for unauthorized users
- ✅ Approval-gating blocks mutations until approved
- ✅ Concurrency control returns 409 on conflict
- ✅ All endpoints tested with integration tests (minimum 30 tests covering success, auth, validation, conflict)
- ✅ `dotnet build` succeeds
- ✅ Spot-check: Live backend responds correctly to valid/invalid requests

Upon completion, backend is ready for frontend connection and system integration testing.
