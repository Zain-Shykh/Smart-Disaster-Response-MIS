# Stored Procedures Specification
## Smart Disaster Response MIS

**Total Stored Procedures: 16** (organized by business domain)

---

## 1. Resource Allocation & Approval (3)

| # | Name | Purpose | Parameters | Returns |
|---|------|---------|------------|---------|
| 1 | `sp_ApproveAllocation` | Approve a pending resource allocation request | @AllocationID INT, @ReviewedBy INT, @Comments VARCHAR(2000) | Success/Failure status, updated AllocationID |
| 2 | `sp_RejectAllocation` | Reject a pending resource allocation request | @AllocationID INT, @ReviewedBy INT, @Comments VARCHAR(2000) | Success/Failure status |
| 3 | `sp_DispatchResources` | Mark resources as dispatched from warehouse to event | @AllocationID INT, @DispatchedByUserID INT | Success/Failure status, DispatchedAt timestamp |

---

## 2. Team Assignment & Deployment (4)

| # | Name | Purpose | Parameters | Returns |
|---|------|---------|------------|---------|
| 4 | `sp_AssignTeam` | Create a new team assignment for an emergency report | @TeamID INT, @ReportID INT, @AssignedBy INT | Success/Failure status, AssignmentID |
| 5 | `sp_ApproveDeployment` | Approve a pending team deployment request | @AssignmentID INT, @ReviewedBy INT, @Comments VARCHAR(2000) | Success/Failure status, status change confirmation |
| 6 | `sp_RejectDeployment` | Reject a pending team deployment request | @AssignmentID INT, @ReviewedBy INT, @Comments VARCHAR(2000) | Success/Failure status, team freed for reassignment |
| 7 | `sp_CompleteAssignment` | Mark a team assignment as completed | @AssignmentID INT, @CompletedByUserID INT | Success/Failure status, CompletionTime, freed team |

---

## 3. Approval Workflow Management (2)

| # | Name | Purpose | Parameters | Returns |
|---|------|---------|------------|---------|
| 8 | `sp_ApproveRequest` | Generic approval endpoint for polymorphic ApprovalRequest | @RequestID INT, @ReviewedBy INT, @Comments VARCHAR(2000) | Success/Failure status, cascading updates to linked record |
| 9 | `sp_RejectRequest` | Generic rejection endpoint for polymorphic ApprovalRequest | @RequestID INT, @ReviewedBy INT, @Comments VARCHAR(2000) | Success/Failure status, cascading rollback |

---

## 4. Expense & Financial Management (2)

| # | Name | Purpose | Parameters | Returns |
|---|------|---------|------------|---------|
| 10 | `sp_ApproveExpense` | Approve an expense for payment | @ExpenseID INT, @ApprovedBy INT, @Comments VARCHAR(2000) | Success/Failure status, updated PaymentStatus |
| 11 | `sp_RejectExpense` | Reject an expense | @ExpenseID INT, @ApprovedBy INT, @Comments VARCHAR(2000) | Success/Failure status |

---

## 5. Hospital & Patient Management (2)

| # | Name | Purpose | Parameters | Returns |
|---|------|---------|------------|---------|
| 12 | `sp_AdmitPatient` | Admit a patient to a hospital (decrements available beds) | @PatientID INT, @HospitalID INT, @Condition VARCHAR(20), @ReportID INT NULL | Success/Failure status, AdmissionID, updated AvailableBeds |
| 13 | `sp_DischargePatient` | Discharge/transfer a patient from hospital (increments available beds) | @AdmissionID INT, @Status VARCHAR(20), @DischargedByUserID INT | Success/Failure status, updated AvailableBeds |

---

## 6. Inventory Management (2)

| # | Name | Purpose | Parameters | Returns |
|---|------|---------|------------|---------|
| 14 | `sp_CheckInventoryLevel` | Check if inventory is below threshold and return alert status | @InventoryID INT | Quantity, MinThreshold, MaxCapacity, AlertStatus |
| 15 | `sp_UpdateInventoryStock` | Manually update inventory quantity (admin/warehouse manager only) | @InventoryID INT, @NewQuantity DECIMAL, @UpdatedBy INT | Success/Failure status, old quantity, new quantity |

---

## 7. Reporting & Analytics (1)

| # | Name | Purpose | Parameters | Returns |
|---|------|---------|------------|---------|
| 16 | `sp_GetDashboardStats` | Fetch aggregated statistics for admin dashboard | @EventID INT NULL, @StartDate DATETIME2 NULL, @EndDate DATETIME2 NULL | Result set: incident count, resource utilization %, team assignments, hospital occupancy, donations, expenses |

---

## Transaction Atomicity & Error Handling

All procedures must:
- ✓ Use `BEGIN TRANSACTION` / `COMMIT` / `ROLLBACK`
- ✓ Validate inputs before executing business logic
- ✓ Return error messages and status codes on failure
- ✓ Roll back all changes if any validation fails
- ✓ Log audit trail via triggers (automatically via AuditLog trigger)
- ✓ Handle deadlocks and retry logic where needed

---

## Procedure Dependencies & Execution Order

```
Independent (can be created in any order):
  - sp_CheckInventoryLevel
  - sp_UpdateInventoryStock
  - sp_GetDashboardStats

Dependent on ApprovalRequest logic:
  - sp_ApproveAllocation → cascades to ResourceAllocation.Status
  - sp_RejectAllocation → cascades to ResourceAllocation.Status
  - sp_ApproveDeployment → cascades to TeamAssignment.Status
  - sp_RejectDeployment → cascades to TeamAssignment.Status
  - sp_ApproveExpense → cascades to Expense.PaymentStatus
  - sp_RejectExpense → cascades to Expense.PaymentStatus
  - sp_ApproveRequest (generic wrapper)
  - sp_RejectRequest (generic wrapper)

Dependent on external state:
  - sp_DispatchResources (requires valid Allocation in Approved state)
  - sp_CompleteAssignment (requires valid Assignment in progress)
  - sp_AssignTeam (requires Team.AvailabilityStatus = Available)
  - sp_AdmitPatient (requires Hospital.AvailableBeds > 0)
  - sp_DischargePatient (requires valid Admission in Admitted state)

Recommendation: Create in this order:
  1. Independent procedures (CheckInventoryLevel, UpdateInventoryStock, GetDashboardStats)
  2. Basic CRUD procedures (AssignTeam, AdmitPatient)
  3. Approval procedures (ApproveAllocation, ApproveDeployment, ApproveExpense, etc.)
  4. Generic approval wrapper (sp_ApproveRequest, sp_RejectRequest)
  5. Complex operations (DispatchResources, CompleteAssignment, DischargePatient)
```

---

## Implementation Priority

**Critical (Phase 2a):**
- sp_ApproveAllocation
- sp_ApproveDeployment
- sp_ApproveExpense
- sp_ApproveRequest / sp_RejectRequest

**High (Phase 2b):**
- sp_AssignTeam
- sp_DispatchResources
- sp_CompleteAssignment
- sp_AdmitPatient
- sp_DischargePatient

**Medium (Phase 2c):**
- sp_CheckInventoryLevel
- sp_UpdateInventoryStock
- sp_GetDashboardStats
