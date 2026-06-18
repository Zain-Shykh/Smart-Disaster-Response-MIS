# Normalization Steps (1NF to 3NF/BCNF)

## Scope
This document summarizes normalization decisions applied to the Smart Disaster Response MIS schema.

## Unnormalized Design Risks (Pre-normalization)
Typical unnormalized issues for this domain include:
- Repeating phone numbers in user/citizen/donor rows
- Repeating specializations in team/hospital rows
- Mixed role-permission attributes in a single security table
- Embedded many-to-many relationships without bridge tables

These patterns were decomposed during schema design.

## First Normal Form (1NF)
Rule: each attribute must hold atomic values and no repeating groups.

Applied 1NF changes:
- Extracted multivalued phones into separate tables:
  - UserPhone(UserID, Phone)
  - CitizenPhone(CitizenID, Phone)
  - DonorPhone(DonorID, Phone)
- Extracted multivalued specializations into separate tables:
  - RescueTeamSpecialization(TeamID, Specialization)
  - HospitalSpecialization(HospitalID, Specialization)
- Ensured location fields are atomic columns (Street, Area, City, Province, Latitude, Longitude where relevant).

Result:
- No repeating groups in base entities.
- Atomic attributes across all operational tables.

## Second Normal Form (2NF)
Rule: for tables with composite keys, all non-key attributes must depend on the full key.

Applied 2NF checks:
- Bridge tables with composite keys contain only key-driven attributes:
  - RolePermission(RoleID, PermissionID)
  - UserPhone(UserID, Phone)
  - CitizenPhone(CitizenID, Phone)
  - DonorPhone(DonorID, Phone)
  - RescueTeamSpecialization(TeamID, Specialization)
  - HospitalSpecialization(HospitalID, Specialization)
- UserRole(UserID, RoleID) includes AssignedAt and AssignedBy, both dependent on the full relationship (user-role assignment event).
- Weak entities with composite keys retain attributes dependent on full key:
  - TeamActivity(TeamID, ActivityID, ...)
  - ApprovalHistory(RequestID, HistoryID, ...)
  - InventoryAlert(InventoryID, AlertID, ...)

Result:
- No partial dependency of non-key attributes on only part of a composite key.

## Third Normal Form (3NF)
Rule: non-key attributes must not depend on other non-key attributes (remove transitive dependency).

Applied 3NF checks:
- Role and permission metadata separated from user identity:
  - User, Role, Permission, UserRole, RolePermission
- Operational facts separated by domain responsibility:
  - EmergencyReport, DisasterEvent, TeamAssignment, ResourceAllocation, Donation, Expense
- Derived metrics are computed or maintained through controlled logic instead of duplicated denormalized columns:
  - Computed columns (for example, DurationMinutes, OccupancyRate, LengthOfStayHours)
  - Trigger-maintained counters and states (for example, TotalReports, AvailabilityStatus, alert lifecycle)
- Approval workflow data isolated:
  - ApprovalRequest and ApprovalHistory separated from target entities, linked by foreign keys.

Result:
- Transitive dependency minimized and domain concerns separated cleanly.

## BCNF Review
Most relation designs satisfy BCNF under business keys:
- Single-entity tables use surrogate PK with enforced alternate keys where needed (for example, Username, Email, NationalID).
- Associative tables use composite PKs where determinants are keys.
- Junction tables have determinant sets equal to candidate keys.

Pragmatic exceptions:
- Some derived/operational columns are intentionally retained for performance or workflow visibility and are protected via constraints/triggers.

## Data Integrity Controls Supporting Normalization
- Primary keys and foreign keys on all relationships.
- Unique constraints on natural identifiers where applicable.
- Check constraints for domain rules (status sets, value ranges, temporal consistency).
- Trigger safeguards for cross-table consistency and automation.

## Final Normalization Summary
- 1NF: achieved through decomposition of multivalued attributes and atomic columns.
- 2NF: achieved across composite-key structures by ensuring full-key dependency.
- 3NF: achieved by separating independent subject areas and removing transitive dependencies.
- BCNF: generally satisfied with controlled practical exceptions for derived operational behavior.
