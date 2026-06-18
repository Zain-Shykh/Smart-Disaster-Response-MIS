# Smart Disaster Response MIS — ER Design

> **Prepared for:** draw.io XML generation via GitHub Copilot  
> **Use this file as the single source of truth for the ERD.**  
> All location composites, entity types, attributes, and relationships are fully specified below.



## Entity Classification

### Strong Entities (15)

| Entity | Description |
|--------|-------------|
| User | System users across all roles |
| Role | RBAC roles (Admin, Operator, etc.) |
| Permission | Fine-grained CRUD permissions per module |
| Citizen | People who submit emergency reports |
| EmergencyReport | Incident reports submitted by citizens |
| DisasterEvent | A declared disaster (flood, fire, earthquake) |
| RescueTeam | Field teams (medical, fire, rescue, search) |
| Resource | Resource types (food, water, medicine, shelter) |
| Warehouse | Storage locations for resources |
| Hospital | Medical facilities tracking beds/patients |
| Patient | Individuals admitted to hospitals |
| Donor | Individuals or organizations making donations |
| Donation | Financial donations linked to a disaster event |
| Expense | Spending records for a disaster event |
| ApprovalRequest | Pending workflow requests needing authorization |

> **Note:** `AuditLog` has been **promoted to a Strong Entity** (see correction #3 above).

---

### Weak Entities (4)

| Weak Entity | Identifying Owner | Why Weak |
|-------------|-------------------|----------|
| TeamActivity | RescueTeam | Activity has no meaning without its team; partial key = ActivityID |
| ApprovalHistory | ApprovalRequest | History entries exist only in context of a specific request |
| InventoryAlert | Inventory (Associative) | Alert has no existence independent of a specific inventory record |
| AuditLog *(reclassified — see note)* | *(now Strong — UserID is nullable FK)* | Reclassified to preserve compliance logs on user deletion |

---

### Associative Entities (6)

| Associative Entity | Bridges | Own Attributes |
|--------------------|---------|----------------|
| UserRole | User ↔ Role | AssignedAt, AssignedBy |
| RolePermission | Role ↔ Permission | — |
| TeamAssignment | RescueTeam ↔ EmergencyReport | AssignmentTime, CompletionTime, Status |
| Inventory | Resource ↔ Warehouse | Quantity, MinThreshold, MaxCapacity, LastUpdated |
| ResourceAllocation | Inventory ↔ DisasterEvent | Quantity, Status, DispatchedAt, ConsumedAt |
| PatientAdmission | Patient ↔ Hospital | Condition, AdmissionTime, DischargeTime, Status |

---

## All Entity Attributes

### User *(Strong)*

| Attribute | Type | Notes |
|-----------|------|-------|
| UserID | PK | Simple |
| FullName | Composite | → FirstName, LastName |
| Username | Simple | |
| PasswordHash | Simple | Encrypted |
| Email | Simple | |
| Phone | Multivalued {Phone} | Separate table: UserPhone |
| CreatedAt | Simple | |
| LastLoginAt | Derived | Derived from AuditLog (most recent login action) |
| IsActive | Simple | Boolean |

---

### Role *(Strong)*

| Attribute | Type |
|-----------|------|
| RoleID | PK |
| RoleName | Simple |
| Description | Simple |

---

### Permission *(Strong)*

| Attribute | Type |
|-----------|------|
| PermissionID | PK |
| PermissionName | Simple |
| Module | Simple (e.g., Reports, Finance) |
| Action | Simple (CREATE / READ / UPDATE / DELETE) |

---

### Citizen *(Strong)*

| Attribute | Type | Notes |
|-----------|------|-------|
| CitizenID | PK | Simple |
| FullName | Composite | → FirstName, LastName |
| NationalID | Simple | |
| Email | Simple | |
| Phone | Multivalued {Phone} | Separate table: CitizenPhone |
| Address | Composite | → **Street, Area, City, Province** *(corrected from Street, City, Province)* |
| TotalReports | Derived | Count of submitted EmergencyReports |

---

### EmergencyReport *(Strong)*

| Attribute | Type | Notes |
|-----------|------|-------|
| ReportID | PK | Simple |
| Location | Composite | → **Street, Area, City, Province, Latitude, Longitude** *(corrected from Latitude, Longitude, Address)* |
| DisasterType | Simple | |
| SeverityLevel | Simple | Low / Medium / High / Critical |
| ReportTime | Simple | |
| Status | Simple | Pending / InProgress / Resolved / Closed |
| Source | Simple | Mobile / Helpline / MonitoringSystem |
| Description | Simple | |
| ResponseTime | Derived | Time from ReportTime to first TeamAssignment |
| ResolutionTime | Derived | ReportTime → Resolved timestamp |

---

### DisasterEvent *(Strong)*

| Attribute | Type | Notes |
|-----------|------|-------|
| EventID | PK | Simple |
| EventName | Simple | |
| DisasterType | Simple | |
| StartTime | Simple | |
| EndTime | Simple | |
| Duration | Derived | EndTime − StartTime |
| Location | Composite | → **Street, Area, City, Province** *(corrected from Area, City, Province)* |
| Status | Simple | Active / Contained / Resolved |
| AffectedPopulation | Simple | |
| TotalReports | Derived | Count of linked EmergencyReports |

---

### RescueTeam *(Strong)*

| Attribute | Type | Notes |
|-----------|------|-------|
| TeamID | PK | Simple |
| TeamName | Simple | |
| TeamType | Simple | Medical / Fire / Rescue / Search |
| CurrentLocation | Composite | → **Street, Area, City, Province, Latitude, Longitude** *(corrected from only Latitude, Longitude)* |
| AvailabilityStatus | Simple | Available / Assigned / Busy / Completed |
| Capacity | Simple | |
| Specialization | Multivalued {Specialization} | Separate table |
| TotalAssignments | Derived | Count of TeamAssignment records |

---

### TeamActivity *(Weak — Owner: RescueTeam)*

| Attribute | Type | Notes |
|-----------|------|-------|
| ActivityID | Partial Key | |
| TeamID | FK (Owner) | |
| ActivityType | Simple | |
| StartTime | Simple | |
| EndTime | Simple | Nullable if activity is ongoing |
| Duration | Derived | EndTime − StartTime (null if EndTime is null) |
| Notes | Simple | |
| Outcome | Simple | |

---

### TeamAssignment *(Associative)*

| Attribute | Type |
|-----------|------|
| AssignmentID | PK |
| TeamID | FK → RescueTeam |
| ReportID | FK → EmergencyReport |
| AssignedBy | FK → User |
| AssignmentTime | Simple |
| CompletionTime | Simple |
| Status | Simple (Assigned / EnRoute / OnSite / Completed) |

---

### Resource *(Strong)*

| Attribute | Type |
|-----------|------|
| ResourceID | PK |
| ResourceName | Simple |
| ResourceType | Simple (Food / Water / Medicine / Shelter) |
| Unit | Simple (kg / liters / units) |
| Description | Simple |

---

### Warehouse *(Strong)*

| Attribute | Type | Notes |
|-----------|------|-------|
| WarehouseID | PK | Simple |
| WarehouseName | Simple | |
| Location | Composite | → **Street, Area, City, Province, Latitude, Longitude** *(corrected from Address, City, Province, Lat, Lng)* |
| Capacity | Simple | |
| ManagerID | FK → User | |
| ContactInfo | Composite | → Phone, Email |

---

### Inventory *(Associative)*

| Attribute | Type |
|-----------|------|
| InventoryID | PK |
| WarehouseID | FK → Warehouse |
| ResourceID | FK → Resource |
| Quantity | Simple |
| MinThreshold | Simple |
| MaxCapacity | Simple |
| LastUpdated | Simple |

---

### InventoryAlert *(Weak — Owner: Inventory)*

| Attribute | Type |
|-----------|------|
| AlertID | Partial Key |
| InventoryID | FK (Owner) |
| AlertType | Simple (LowStock / OutOfStock) |
| AlertTime | Simple |
| Status | Simple (Active / Resolved) |
| ResolvedAt | Simple |

---

### ResourceAllocation *(Associative)*

| Attribute | Type |
|-----------|------|
| AllocationID | PK |
| InventoryID | FK → Inventory |
| EventID | FK → DisasterEvent |
| RequestedBy | FK → User |
| Quantity | Simple |
| RequestTime | Simple |
| Status | Simple (Pending / Approved / Dispatched / Consumed / Rejected) |
| DispatchedAt | Simple |
| ConsumedAt | Simple |

---

### Hospital *(Strong)*

| Attribute | Type | Notes |
|-----------|------|-------|
| HospitalID | PK | Simple |
| HospitalName | Simple | |
| Location | Composite | → **Street, Area, City, Province** *(corrected from Address, City, Province)* |
| TotalBeds | Simple | |
| AvailableBeds | Simple | |
| OccupancyRate | Derived | (TotalBeds − AvailableBeds) / TotalBeds × 100 |
| ContactInfo | Composite | → Phone, Email |
| Specialization | Multivalued {Specialization} | Separate table |

---

### Patient *(Strong)*

| Attribute | Type |
|-----------|------|
| PatientID | PK |
| FullName | Composite → FirstName, LastName |
| Age | Simple |
| Gender | Simple |
| NationalID | Simple |
| BloodType | Simple |
| ContactPhone | Simple |

---

### PatientAdmission *(Associative)*

| Attribute | Type | Notes |
|-----------|------|-------|
| AdmissionID | PK | |
| PatientID | FK → Patient | |
| HospitalID | FK → Hospital | |
| ReportID | FK → EmergencyReport | Nullable |
| AdmissionTime | Simple | |
| DischargeTime | Simple | |
| Condition | Simple | Critical / Serious / Stable |
| Status | Simple | Admitted / Discharged / Transferred |
| LengthOfStay | Derived | DischargeTime − AdmissionTime |

---

### Donor *(Strong)*

| Attribute | Type | Notes |
|-----------|------|-------|
| DonorID | PK | Simple |
| FullName | Composite | → FirstName, LastName |
| DonorType | Simple | Individual / Organization |
| OrganizationName | Simple | Nullable |
| Phone | Multivalued {Phone} | Separate table |
| Email | Simple | |
| Address | Composite | → **Street, Area, City, Province** *(corrected from Street, City)* |

---

### Donation *(Strong)*

| Attribute | Type |
|-----------|------|
| DonationID | PK |
| DonorID | FK → Donor |
| EventID | FK → DisasterEvent |
| Amount | Simple |
| DonationDate | Simple |
| PaymentMethod | Simple (Cash / BankTransfer / Online) |
| Status | Simple (Pending / Confirmed / Rejected) |
| ReceiptNumber | Simple |

---

### Expense *(Strong)*

| Attribute | Type | Notes |
|-----------|------|-------|
| ExpenseID | PK | Simple |
| EventID | FK → DisasterEvent | |
| ApprovedBy | FK → User | Nullable; denormalized reference — populated after ApprovalRequest is granted *(see correction #2)* |
| Category | Simple | Procurement / Operations / Medical / Logistics |
| Amount | Simple | |
| Description | Simple | |
| ExpenseDate | Simple | |
| PaymentStatus | Simple | |

---

### ApprovalRequest *(Strong)*

| Attribute | Type | Notes |
|-----------|------|-------|
| RequestID | PK | Simple |
| RequestedBy | FK → User | |
| ReviewedBy | FK → User | Nullable |
| RequestType | Simple | ResourceDistribution / RescueDeployment / Financial |
| RequestTime | Simple | |
| Status | Simple | Pending / Approved / Rejected |
| Description | Simple | |
| AllocationID | FK → ResourceAllocation | Nullable *(corrected from single polymorphic ReferenceID)* |
| AssignmentID | FK → TeamAssignment | Nullable |
| ExpenseID | FK → Expense | Nullable |

> **Design note:** Exactly one of `AllocationID`, `AssignmentID`, `ExpenseID` will be non-null per row, enforced at the application/trigger level. This replaces the original polymorphic `ReferenceID + ReferenceType` pattern.

---

### ApprovalHistory *(Weak — Owner: ApprovalRequest)*

| Attribute | Type |
|-----------|------|
| HistoryID | Partial Key |
| RequestID | FK (Owner) |
| ActionBy | FK → User |
| ActionTime | Simple |
| Decision | Simple (Approved / Rejected / Escalated) |
| Comments | Simple |

---

### AuditLog *(Strong — reclassified from Weak)*

| Attribute | Type | Notes |
|-----------|------|-------|
| LogID | PK | Simple (was partial key; now full PK) |
| UserID | FK → User | **Nullable, SET NULL on User delete** — preserves log on user removal |
| Action | Simple | |
| TableName | Simple | |
| RecordID | Simple | |
| OldValue | Simple | |
| NewValue | Simple | |
| Timestamp | Simple | |
| IPAddress | Simple | |

---

## Relationships, Cardinalities & Participation

| # | Relationship | Entity A | Cardinality A (min,max) | Entity B | Cardinality B (min,max) | Participation Notes |
|---|-------------|----------|------------------------|----------|------------------------|---------------------|
| 1 | User **has** Role | User | (1, N) | Role | (0, N) | Total on User (must have ≥1 role); Partial on Role |
| 2 | Role **grants** Permission | Role | (1, N) | Permission | (0, N) | Total on Role; Partial on Permission |
| 3 | Citizen **submits** EmergencyReport | Citizen | (0, N) | EmergencyReport | (1, 1) | Partial on Citizen; Total on Report |
| 4 | EmergencyReport **categorized in** DisasterEvent | EmergencyReport | (0, 1) | DisasterEvent | (0, N) | Partial on both |
| 5 | RescueTeam **assigned to** EmergencyReport | RescueTeam | (0, N) | EmergencyReport | (0, N) | Partial on both (via TeamAssignment) |
| 6 | User **performs** TeamAssignment | User | (0, N) | TeamAssignment | (1, 1) | Partial on User; Total on Assignment |
| 7 | Resource **stored in** Warehouse | Resource | (0, N) | Warehouse | (0, N) | Partial on both (via Inventory) |
| 8 | User **manages** Warehouse | User | (0, N) | Warehouse | (1, 1) | Partial on User; Total on Warehouse |
| 9 | Inventory **allocated for** DisasterEvent | Inventory | (0, N) | DisasterEvent | (0, N) | Partial on both (via ResourceAllocation) |
| 10 | Inventory **triggers** InventoryAlert | Inventory | (0, N) | InventoryAlert | (1, 1) | Partial on Inventory; Total on Alert |
| 11 | RescueTeam **records** TeamActivity | RescueTeam | (0, N) | TeamActivity | (1, 1) | Partial on Team; Total on Activity |
| 12 | Hospital **admits** Patient | Hospital | (0, N) | Patient | (0, N) | Partial on both (via PatientAdmission) |
| 13 | EmergencyReport **results in** PatientAdmission | EmergencyReport | (0, N) | PatientAdmission | (0, 1) | Partial on both |
| 14 | Donor **makes** Donation | Donor | (0, N) | Donation | (1, 1) | Partial on Donor; Total on Donation |
| 15 | Donation **funds** DisasterEvent | Donation | (1, 1) | DisasterEvent | (0, N) | Total on Donation; Partial on Event |
| 16 | Expense **charged to** DisasterEvent | Expense | (1, 1) | DisasterEvent | (0, N) | Total on Expense; Partial on Event |
| 17 | User **approves** Expense | User | (0, N) | Expense | (0, 1) | Partial on both |
| 18 | User **requests** ApprovalRequest | User | (0, N) | ApprovalRequest | (1, 1) | Partial on User; Total on Request |
| 19 | User **reviews** ApprovalRequest | User | (0, N) | ApprovalRequest | (0, 1) | Partial on both |
| 20 | ApprovalRequest **has** ApprovalHistory | ApprovalRequest | (1, N) | ApprovalHistory | (1, 1) | Total on both (identifying relationship) |
| 21 | User **decides** ApprovalHistory | User | (0, N) | ApprovalHistory | (1, 1) | Partial on User; Total on History |
| 22 | User **generates** AuditLog | User | (0, N) | AuditLog | (1, 1) | Partial on User; Total on Log *(UserID nullable — see correction #3)* |
| 23 | User **requests** ResourceAllocation | User | (0, N) | ResourceAllocation | (1, 1) | Partial on User; Total on Allocation |

---

## Multivalued Attribute Tables (Separate Tables)

These are created during normalization to handle multivalued attributes:

| Table | FK | Attribute |
|-------|----|-----------|
| UserPhone | UserID → User | Phone |
| CitizenPhone | CitizenID → Citizen | Phone |
| DonorPhone | DonorID → Donor | Phone |
| RescueTeamSpecialization | TeamID → RescueTeam | Specialization |
| HospitalSpecialization | HospitalID → Hospital | Specialization |

---

## Composite Attribute Reference (Standardized)

All location/address composites follow this standard pattern:

| Entity | Attribute Name | Sub-attributes |
|--------|---------------|----------------|
| Citizen | Address | Street, Area, City, Province |
| EmergencyReport | Location | Street, Area, City, Province, **Latitude, Longitude** |
| DisasterEvent | Location | Street, Area, City, Province |
| RescueTeam | CurrentLocation | Street, Area, City, Province, **Latitude, Longitude** |
| Warehouse | Location | Street, Area, City, Province, **Latitude, Longitude** |
| Hospital | Location | Street, Area, City, Province |
| Donor | Address | Street, Area, City, Province |
| User | FullName | FirstName, LastName |
| Citizen | FullName | FirstName, LastName |
| Patient | FullName | FirstName, LastName |
| Donor | FullName | FirstName, LastName |
| Warehouse | ContactInfo | Phone, Email |
| Hospital | ContactInfo | Phone, Email |

> Latitude and Longitude are included only where real-time GPS tracking is operationally needed (incident sites, moving teams, physical warehouses).

---

## Entity Summary Count

| Type | Count | Entities |
|------|-------|----------|
| Strong | 16 | User, Role, Permission, Citizen, EmergencyReport, DisasterEvent, RescueTeam, Resource, Warehouse, Hospital, Patient, Donor, Donation, Expense, ApprovalRequest, **AuditLog** |
| Weak | 3 | TeamActivity, ApprovalHistory, InventoryAlert |
| Associative | 6 | UserRole, RolePermission, TeamAssignment, Inventory, ResourceAllocation, PatientAdmission |
| **Total** | **25** | |

---

*End of ER Design Document*