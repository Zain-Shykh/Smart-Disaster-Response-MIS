# INDEXING_SPECIFICATION

## Purpose
This document defines the indexing required for the Smart Disaster Response MIS to support high-volume reads, operational dashboards, approval workflows, and financial/audit reporting.

It includes:
- Single-column indexes
- Composite indexes
- Priority and rationale
- Notes on write overhead (insert/update/delete impact)

## Indexing Principles Used
- Index columns used in `WHERE`, `JOIN`, `GROUP BY`, and `ORDER BY` patterns.
- Prefer composite indexes for common multi-column filters.
- Keep highly volatile columns out of excessive indexing to reduce write overhead.
- Avoid indexing large free-text columns (`VARCHAR(MAX)`, long description fields).

## Required Indexes (Project Requirements)

### 1) Incident Location Indexes
1. `EmergencyReport(Province, City, Area, Street)`
   - Type: Composite
   - Purpose: Fast incident-location filtering and map/dispatch lookups.

2. `DisasterEvent(Province, City, Area, Street)`
   - Type: Composite
   - Purpose: Fast event-level location analysis and regional dashboards.

### 2) Disaster Type Indexes
1. `EmergencyReport(DisasterType)`
   - Type: Single-column
   - Purpose: Type-based incident filtering.

2. `DisasterEvent(DisasterType, Status, StartTime)`
   - Type: Composite
   - Purpose: Type + lifecycle filtering in event overviews.

### 3) Resource Type Indexes
1. `Resource(ResourceType)`
   - Type: Single-column
   - Purpose: Fast resource-category filtering.

2. `Inventory(WarehouseID, ResourceID)`
   - Type: Composite
   - Purpose: Fast lookup of stock by warehouse and resource, which matches the most common inventory query pattern.

### 4) Transaction Timestamp Indexes
1. `EmergencyReport(ReportTime)`
2. `TeamAssignment(AssignmentTime)`
3. `ResourceAllocation(RequestTime)`
4. `Donation(DonationDate)`
5. `Expense(ExpenseDate)`
6. `ApprovalRequest(RequestTime)`
7. `ApprovalHistory(ActionTime)`
8. `Inventory(LastUpdated)`
9. `InventoryAlert(AlertTime)`

- Type: Single-column (each)
- Purpose: Time-window filtering, chronological dashboards, and recent-activity queries.

### 5) Additional High-Priority Operational Indexes
1. `RescueTeam(AvailabilityStatus)`
   - Type: Single-column
   - Purpose: Supports assignment checks and availability-based filtering in dispatch workflows.

2. `RescueTeam(TeamType, AvailabilityStatus)`
   - Type: Composite
   - Purpose: Supports operator filtering of available teams by type during dispatch.

3. `Hospital(AvailableBeds)`
   - Type: Single-column
   - Purpose: Supports bed availability checks and patient admission workflows.

4. `PatientAdmission(HospitalID, Status)`
   - Type: Composite
   - Purpose: Supports bed count queries and hospital load balancing.

5. `EmergencyReport(SeverityLevel, Status)`
   - Type: Composite
   - Purpose: Supports triage prioritization and status-based incident queues.

6. `TeamActivity(TeamID, StartTime)`
   - Type: Composite
   - Purpose: Supports activity log queries filtered by team and time.

## Recommended Operational Index Set (Production)

### Emergency and Dispatch
1. `EmergencyReport(Status, ReportTime)` INCLUDE `(EventID, CitizenID, DisasterType, SeverityLevel)`
   - Supports pending/in-progress queues and time-ordered triage.

2. `EmergencyReport(EventID, ReportTime)` INCLUDE `(Status, DisasterType, SeverityLevel)`
   - Supports event-level reporting and timeline views.

3. `TeamAssignment(ReportID, Status, AssignmentTime)` INCLUDE `(TeamID, CompletionTime, AssignedBy)`
   - Supports `NOT EXISTS` assignment checks and assignment detail views.

4. `TeamAssignment(TeamID, Status, AssignmentTime)` INCLUDE `(ReportID, CompletionTime)`
   - Supports team workload and availability analysis.

### Resource and Inventory
1. `ResourceAllocation(EventID, Status, RequestTime)` INCLUDE `(InventoryID, Quantity, RequestedBy, DispatchedAt, ConsumedAt)`
   - Supports event-level resource workflow tracking.

2. `ResourceAllocation(InventoryID, Status, RequestTime)` INCLUDE `(EventID, Quantity)`
   - Supports stock movement tracking per inventory item.

3. `Inventory(WarehouseID, ResourceID)`
   - Already enforced as UNIQUE in schema; do not create a separate duplicate index because SQL Server auto-creates one for the unique constraint.

4. `Inventory(ResourceID, WarehouseID)` is intentionally omitted.
   - Reason: it overlaps with `Inventory(WarehouseID, ResourceID)` and adds unnecessary write overhead.

5. `InventoryAlert(Status, AlertTime)` INCLUDE `(InventoryID, AlertType, ResolvedAt)`
   - Supports active-alert dashboards.

### Financial and Audit
1. `Donation(EventID, Status, DonationDate)` INCLUDE `(Amount, DonorID)`
   - Supports confirmed-donation aggregation by event.

2. `Expense(EventID, ExpenseDate)` INCLUDE `(Amount, Category, PaymentStatus, ApprovedBy)`
   - Supports event expense analysis and budget rollups.

3. `AuditLog(TableName, [Timestamp])` INCLUDE `(UserID, [Action], RecordID)`
   - Supports table-specific audit trails and recent financial audit queries.

4. `AuditLog([Timestamp])` INCLUDE `(UserID, [Action], TableName, RecordID)`
   - Supports recent activity monitoring.

### Approval and Security Workflows
1. `ApprovalRequest(Status, RequestTime)` INCLUDE `(RequestType, RequestedBy, ReviewedBy, AllocationID, AssignmentID, ExpenseID)`
   - Supports pending approvals and SLA tracking.

2. `ApprovalHistory(RequestID, ActionTime)` INCLUDE `(Decision, ActionBy)`
   - Supports approval history timelines.

3. `UserRole(RoleID, UserID)`
   - Supports reverse-role lookups and RBAC joins.

4. `RolePermission(PermissionID, RoleID)`
   - Supports permission-to-role lookups.

5. `PatientAdmission(Status, AdmissionTime)` INCLUDE `(HospitalID, PatientID, ReportID, [Condition])`
   - Supports active admission monitoring.

## Scenarios Where Indexing Improves Performance
- Large filtered reads on status/type/time dimensions.
- Join-heavy reporting queries (event, assignment, allocation, RBAC).
- Time-sliced dashboards (recent incidents, recent approvals, recent logs).
- Aggregate queries over donation/expense/resource allocation data.

## Cases Where Indexing Adds Overhead
- High-frequency inserts/updates on heavily indexed tables (`EmergencyReport`, `TeamAssignment`, `ResourceAllocation`, `AuditLog`).
- Extra storage and maintenance costs for many nonclustered indexes.
- Slower bulk load/seed operations because each index must be maintained.

## Implementation Priority
1. Mandatory requirement indexes:
   - Location, disaster type, resource type, transaction timestamps.
2. High-impact operational indexes:
   - Pending reports, team assignments, allocation workflow, budget/audit views, dispatch availability, admissions, and activity logs.
3. RBAC and workflow support indexes:
   - Approval, role, permission, admission paths.

## Validation Plan
- Compare representative queries before and after index creation using:
  - Execution time
  - Response latency
  - Logical reads (`SET STATISTICS IO ON`)
  - CPU time (`SET STATISTICS TIME ON`)
- Also record insert/update latency on write-heavy tables to quantify indexing overhead.
