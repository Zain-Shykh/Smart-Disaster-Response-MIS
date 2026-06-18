# Views Specification
## Smart Disaster Response MIS — Brief

Purpose: provide role-specific, reporting, and simplified-query views to support the frontend, stored procedures, and reporting needs defined by the DDL, triggers, and stored procedures.

---

## 1. Inventory & Resource Views
- `vw_Inventory_Current` : Inventory details (InventoryID, WarehouseID, ResourceID, Quantity, MinThreshold, MaxCapacity, LastUpdated). Used by warehouse UI and `sp_CheckInventoryLevel`.
- `vw_Inventory_Alerts` : Active and recent alerts (InventoryID, AlertID, AlertType, AlertTime, Status, ResolvedAt, Quantity, MinThreshold). Supports alert dashboards and notification feed.
- `vw_ResourceAllocation_Status` : Allocation summary (AllocationID, InventoryID, EventID, Quantity, Status, RequestedBy, RequestTime, DispatchedAt, ConsumedAt). Used by ops dashboard and `sp_DispatchResources` UI.

## 2. Emergency Report Views
- `vw_EmergencyReports_Pending` : Unassigned/pending reports (ReportID, CitizenID, EventID, DisasterType, SeverityLevel, ReportTime, Status, Street, Area, City). Operator dashboard for triage and assignment workflow.
- `vw_EmergencyReports_ByEvent` : Reports grouped by event (ReportID, EventID, EventName, DisasterType, SeverityLevel, Status, ReportTime). Used for event drill-down and admin analysis.

## 3. Team & Assignment Views
- `vw_Teams_Availability` : RescueTeam status (TeamID, TeamName, TeamType, AvailabilityStatus, Capacity, TotalAssignments, Latitude, Longitude). Used for assignment selection and map UI.
- `vw_Assignments_Detail` : Assignment details with report info (AssignmentID, TeamID, ReportID, ReportLocation, AssignedBy, AssignmentTime, CompletionTime, Status). Supports field officer dashboard and history.
- `vw_TeamActivity_Log` : Team activity history (TeamID, ActivityID, ActivityType, StartTime, EndTime, DurationMinutes, Notes, Outcome). Field officer dashboard and team audit trail.

## 4. Approval & Workflow Views
- `vw_Pending_Approvals` : Pending approvals (RequestID, RequestType, RequestedBy, RequestTime, AllocationID, AssignmentID, ExpenseID, Description). For approver queues and `sp_ApproveRequest` UI. **Note:** Use three nullable columns to avoid ambiguity; compute a single `TargetID = COALESCE(AllocationID, AssignmentID, ExpenseID)` if needed on frontend.
- `vw_Approval_History` : Approval history (RequestID, HistoryID, ActionBy, ActionTime, Decision, Comments). Used for audit and UI timeline.

## 5. Hospital & Patient Views
- `vw_Hospital_Capacity` : Hospital summary (HospitalID, HospitalName, TotalBeds, AvailableBeds, OccupancyRate). For hospital selection and load display.
- `vw_Patient_Admissions` : Current admissions only (AdmissionID, PatientID, HospitalID, AdmissionTime, Condition, Status, ReportID) filtered to `Status = 'Admitted'` only. For triage and bed management. **Note:** Discharged/transferred patients require a separate view or history query; this view shows only active admissions.

## 6. Financial Views
- `vw_Donations_Summary` : Donations per event/status (DonationID, DonorID, EventID, Amount, DonationDate, Status). For finance dashboards.
- `vw_Expenses_Summary` : Expenses per event/category/status (ExpenseID, EventID, Category, Amount, ExpenseDate, PaymentStatus, ApprovedBy). For budget tracking.
- `vw_Budget_PerEvent` : Side-by-side budget view (EventID, EventName, TotalDonations, TotalExpenses, NetBudget). Finance officer oversight and reconciliation. **Note:** EventName is required for frontend display without additional join.

## 7. Reporting & Dashboard Views
- `vw_Event_Overview` : Event-level aggregates (EventID, EventName, StartTime, EndTime, Status, AffectedPopulation, IncidentCount, TotalAllocations, TotalDonations, TotalExpenses). For high-level dashboards.
- `vw_Response_Performance` : Response metrics (EventID, AvgResponseTime, AvgTeamCompletionTime, ResourceUtilizationPercent). For analytics and SLA tracking.

## 8. Security & RBAC Views
- `vw_User_Roles_Permissions` : Flattened permissions (UserID, Username, RoleID, RoleName, PermissionID, PermissionName, Module, Action). For backend authorization checks and admin UI.

## 9. Audit & Monitoring Views
- `vw_Audit_Recent` : Recent audit log entries (LogID, UserID, Action, TableName, RecordID, Timestamp, OldValue, NewValue). For compliance dashboards — use TOP 1000 or date range to avoid slowness.
- `vw_FinancialAuditTrail` : Financial-only audit trail (LogID, UserID, Action, TableName, RecordID, Timestamp, OldValue, NewValue). Filters AuditLog to Donation/Expense/ApprovalRequest records only — required by PROJECT_STATEMENT §12.

---

## Implementation Notes

**Sensitive Data Handling:**
- `vw_User_Roles_Permissions` must explicitly exclude `PasswordHash` and `Email` columns to prevent exposure in authorization checks.
- Enforce role-based row filters in views (e.g., field officers see only assigned reports).

**Performance & Derived Attributes:**
- `vw_Response_Performance.AvgResponseTime` is computed as `AVG(DATEDIFF(MINUTE, ReportTime, CompletionTime))` — not a stored column; aligns with triggers and DDL.
- `vw_Audit_Recent` must include `TOP 1000` or date-range filter (e.g., last 7 days) to prevent table scans on high-volume systems.

**Best Practices:**
- Keep views read-only; apply DML restrictions in RBAC roles, not view definitions.
- Materialize only heavy-reporting queries (e.g., `vw_Event_Overview`, `vw_Budget_PerEvent`) after measuring workload.
- Next: implement `VIEWS_SQL.sql` with `CREATE VIEW` statements matching exact column names from `DDL.sql`.
