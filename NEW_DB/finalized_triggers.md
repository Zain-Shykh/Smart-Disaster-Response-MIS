# Smart Disaster Response MIS — Finalized Triggers

Total: 24 triggers across 7 categories.

---

## 1. Resource Management (4)

| # | Name | Event | Purpose | Action |
|---|---|---|---|---|
| 1 | `trg_deduct_inventory` | AFTER UPDATE on ResourceAllocation (Status → Approved) | Deduct stock when allocation approved | Decrease Inventory.Quantity by allocated amount |
| 2 | `trg_prevent_negative_inventory` | BEFORE INSERT/UPDATE on ResourceAllocation | Prevent over-allocation | ROLLBACK if requested Quantity > available Inventory.Quantity |
| 3 | `trg_check_inventory_threshold` | AFTER UPDATE on Inventory (Quantity changes) | Auto-generate low stock alert | INSERT into InventoryAlert if Quantity < MinThreshold |
| 4 | `trg_resolve_inventory_alert` | AFTER UPDATE on Inventory (Quantity rises above threshold) | Auto-resolve active alert | UPDATE InventoryAlert SET Status = Resolved |

---

## 2. Rescue Team Management (5)

| # | Name | Event | Purpose | Action |
|---|---|---|---|---|
| 5 | `trg_team_assigned` | AFTER INSERT on TeamAssignment | Mark team as tentatively assigned | UPDATE RescueTeam SET AvailabilityStatus = Assigned |
| 6 | `trg_team_busy_on_approval` | AFTER UPDATE on ApprovalRequest (Status → Approved, RequestType = RescueDeployment) | Mark team fully busy after deployment approved | UPDATE RescueTeam SET AvailabilityStatus = Busy via AssignmentID |
| 7 | `trg_team_completed` | AFTER UPDATE on TeamAssignment (Status → Completed) | Free up team after mission | UPDATE RescueTeam SET AvailabilityStatus = Available |
| 8 | `trg_free_team_on_rejection` | AFTER UPDATE on TeamAssignment (Status → Cancelled) | Free team if deployment rejected | UPDATE RescueTeam SET AvailabilityStatus = Available |
| 9 | `trg_prevent_double_assignment` | BEFORE INSERT on TeamAssignment | Prevent assigning a busy team | ROLLBACK if RescueTeam.AvailabilityStatus != Available |

---

## 3. Hospital Management (3)

| # | Name | Event | Purpose | Action |
|---|---|---|---|---|
| 10 | `trg_decrement_beds` | AFTER INSERT on PatientAdmission | Reduce available beds on admission | UPDATE Hospital SET AvailableBeds = AvailableBeds - 1 |
| 11 | `trg_increment_beds` | AFTER UPDATE on PatientAdmission (Status → Discharged / Transferred) | Free up bed on discharge | UPDATE Hospital SET AvailableBeds = AvailableBeds + 1 |
| 12 | `trg_prevent_bed_overflow` | BEFORE INSERT on PatientAdmission | Prevent admitting to a full hospital | ROLLBACK if Hospital.AvailableBeds = 0 |

---

## 4. Approval Request — Auto Creation (3)

| # | Name | Event | Purpose | Action |
|---|---|---|---|---|
| 13 | `trg_create_approval_on_allocation` | AFTER INSERT on ResourceAllocation | Auto-create approval request for resource distribution | INSERT into ApprovalRequest (RequestType = ResourceDistribution, AllocationID = new ID, Status = Pending) |
| 14 | `trg_create_approval_on_assignment` | AFTER INSERT on TeamAssignment | Auto-create approval request for rescue deployment | INSERT into ApprovalRequest (RequestType = RescueDeployment, AssignmentID = new ID, Status = Pending) |
| 15 | `trg_create_approval_on_expense` | AFTER INSERT on Expense | Auto-create approval request for financial expense | INSERT into ApprovalRequest (RequestType = Financial, ExpenseID = new ID, Status = Pending) |

---

## 5. Approval Workflow — Cascade on Decision (4)

| # | Name | Event | Purpose | Action |
|---|---|---|---|---|
| 16 | `trg_approval_granted` | AFTER UPDATE on ApprovalRequest (Status → Approved) | Cascade approval to linked record | UPDATE ResourceAllocation / TeamAssignment / Expense status to Approved based on which FK is non-null |
| 17 | `trg_approval_rejected` | AFTER UPDATE on ApprovalRequest (Status → Rejected) | Cascade rejection to linked record | UPDATE ResourceAllocation → Rejected / TeamAssignment → Cancelled / Expense → Rejected |
| 18 | `trg_set_expense_approvedby` | AFTER UPDATE on Expense (PaymentStatus → Confirmed) | Populate ApprovedBy on Expense record | UPDATE Expense SET ApprovedBy = ReviewedBy pulled from linked ApprovalRequest |

---

## 6. Activity & History Logging (3)

| # | Name | Event | Purpose | Action |
|---|---|---|---|---|
| 19 | `trg_log_approval_history` | AFTER UPDATE on ApprovalRequest (Status → Approved / Rejected / Escalated) | Auto-insert every decision into ApprovalHistory | INSERT into ApprovalHistory (RequestID, ActionBy = ReviewedBy, Decision = new Status, ActionTime = now) |
| 20 | `trg_log_team_activity` | AFTER UPDATE on TeamAssignment (Status changes) | Auto-record every status transition as a team activity | INSERT into TeamActivity (TeamID, ActivityType = new Status, StartTime = now) |
| 21 | `trg_single_approval_fk` | BEFORE INSERT on ApprovalRequest | Enforce exactly one FK is non-null | ROLLBACK if more than one or none of AllocationID, AssignmentID, ExpenseID is filled |

---

## 7. Audit Logging (3)

| # | Name | Event | Purpose | Action |
|---|---|---|---|---|
| 22 | `trg_audit_emergency_report` | AFTER INSERT/UPDATE on EmergencyReport | Track all report changes | INSERT into AuditLog (TableName, RecordID, OldValue, NewValue, UserID, Timestamp) |
| 23 | `trg_audit_financial` | AFTER INSERT/UPDATE on Donation / Expense | Financial audit trail | INSERT into AuditLog for every financial record change |

---

