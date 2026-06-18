USE Final_DB;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
    Index implementation for Smart Disaster Response MIS
    Source: Docs/INDEXING_SPECIFICATION.md
    Notes:
    - Script is idempotent: each index is created only if missing.
    - Inventory(WarehouseID, ResourceID) is usually already indexed by
      UQ_Inventory_Warehouse_Resource unique constraint.
*/

-- ============================================================================
-- 1) INCIDENT LOCATION INDEXES
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EmergencyReport_Province_City_Area_Street'
      AND object_id = OBJECT_ID('dbo.EmergencyReport')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmergencyReport_Province_City_Area_Street
    ON dbo.EmergencyReport (Province, City, Area, Street);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_DisasterEvent_Province_City_Area_Street'
      AND object_id = OBJECT_ID('dbo.DisasterEvent')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DisasterEvent_Province_City_Area_Street
    ON dbo.DisasterEvent (Province, City, Area, Street);
END
GO

-- ============================================================================
-- 2) DISASTER TYPE INDEXES
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EmergencyReport_DisasterType'
      AND object_id = OBJECT_ID('dbo.EmergencyReport')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmergencyReport_DisasterType
    ON dbo.EmergencyReport (DisasterType);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_DisasterEvent_DisasterType_Status_StartTime'
      AND object_id = OBJECT_ID('dbo.DisasterEvent')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DisasterEvent_DisasterType_Status_StartTime
    ON dbo.DisasterEvent (DisasterType, [Status], StartTime);
END
GO

-- ============================================================================
-- 3) RESOURCE TYPE INDEXES
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Resource_ResourceType'
      AND object_id = OBJECT_ID('dbo.Resource')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Resource_ResourceType
    ON dbo.Resource (ResourceType);
END
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_Inventory_Warehouse_Resource'
      AND object_id = OBJECT_ID('dbo.Inventory')
)
BEGIN
    PRINT 'Inventory(WarehouseID, ResourceID) already covered by UQ_Inventory_Warehouse_Resource.';
END
ELSE IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Inventory_WarehouseID_ResourceID'
      AND object_id = OBJECT_ID('dbo.Inventory')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Inventory_WarehouseID_ResourceID
    ON dbo.Inventory (WarehouseID, ResourceID);
END
GO

-- ============================================================================
-- 4) TRANSACTION TIMESTAMP INDEXES
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EmergencyReport_ReportTime'
      AND object_id = OBJECT_ID('dbo.EmergencyReport')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmergencyReport_ReportTime
    ON dbo.EmergencyReport (ReportTime);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TeamAssignment_AssignmentTime'
      AND object_id = OBJECT_ID('dbo.TeamAssignment')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TeamAssignment_AssignmentTime
    ON dbo.TeamAssignment (AssignmentTime);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ResourceAllocation_RequestTime'
      AND object_id = OBJECT_ID('dbo.ResourceAllocation')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ResourceAllocation_RequestTime
    ON dbo.ResourceAllocation (RequestTime);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Donation_DonationDate'
      AND object_id = OBJECT_ID('dbo.Donation')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Donation_DonationDate
    ON dbo.Donation (DonationDate);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Expense_ExpenseDate'
      AND object_id = OBJECT_ID('dbo.Expense')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Expense_ExpenseDate
    ON dbo.Expense (ExpenseDate);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApprovalRequest_RequestTime'
      AND object_id = OBJECT_ID('dbo.ApprovalRequest')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApprovalRequest_RequestTime
    ON dbo.ApprovalRequest (RequestTime);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApprovalHistory_ActionTime'
      AND object_id = OBJECT_ID('dbo.ApprovalHistory')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApprovalHistory_ActionTime
    ON dbo.ApprovalHistory (ActionTime);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Inventory_LastUpdated'
      AND object_id = OBJECT_ID('dbo.Inventory')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Inventory_LastUpdated
    ON dbo.Inventory (LastUpdated);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_InventoryAlert_AlertTime'
      AND object_id = OBJECT_ID('dbo.InventoryAlert')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventoryAlert_AlertTime
    ON dbo.InventoryAlert (AlertTime);
END
GO

-- ============================================================================
-- 5) ADDITIONAL HIGH-PRIORITY OPERATIONAL INDEXES
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RescueTeam_AvailabilityStatus'
      AND object_id = OBJECT_ID('dbo.RescueTeam')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RescueTeam_AvailabilityStatus
    ON dbo.RescueTeam (AvailabilityStatus);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RescueTeam_TeamType_AvailabilityStatus'
      AND object_id = OBJECT_ID('dbo.RescueTeam')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RescueTeam_TeamType_AvailabilityStatus
    ON dbo.RescueTeam (TeamType, AvailabilityStatus);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Hospital_AvailableBeds'
      AND object_id = OBJECT_ID('dbo.Hospital')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Hospital_AvailableBeds
    ON dbo.Hospital (AvailableBeds);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PatientAdmission_HospitalID_Status'
      AND object_id = OBJECT_ID('dbo.PatientAdmission')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PatientAdmission_HospitalID_Status
    ON dbo.PatientAdmission (HospitalID, [Status]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EmergencyReport_SeverityLevel_Status'
      AND object_id = OBJECT_ID('dbo.EmergencyReport')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmergencyReport_SeverityLevel_Status
    ON dbo.EmergencyReport (SeverityLevel, [Status]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TeamActivity_TeamID_StartTime'
      AND object_id = OBJECT_ID('dbo.TeamActivity')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TeamActivity_TeamID_StartTime
    ON dbo.TeamActivity (TeamID, StartTime);
END
GO

-- ============================================================================
-- 6) RECOMMENDED OPERATIONAL INDEX SET
-- ============================================================================

-- Emergency and Dispatch
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EmergencyReport_Status_ReportTime'
      AND object_id = OBJECT_ID('dbo.EmergencyReport')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmergencyReport_Status_ReportTime
    ON dbo.EmergencyReport ([Status], ReportTime)
    INCLUDE (EventID, CitizenID, DisasterType, SeverityLevel);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EmergencyReport_EventID_ReportTime'
      AND object_id = OBJECT_ID('dbo.EmergencyReport')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmergencyReport_EventID_ReportTime
    ON dbo.EmergencyReport (EventID, ReportTime)
    INCLUDE ([Status], DisasterType, SeverityLevel);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TeamAssignment_ReportID_Status_AssignmentTime'
      AND object_id = OBJECT_ID('dbo.TeamAssignment')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TeamAssignment_ReportID_Status_AssignmentTime
    ON dbo.TeamAssignment (ReportID, [Status], AssignmentTime)
    INCLUDE (TeamID, CompletionTime, AssignedBy);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TeamAssignment_TeamID_Status_AssignmentTime'
      AND object_id = OBJECT_ID('dbo.TeamAssignment')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TeamAssignment_TeamID_Status_AssignmentTime
    ON dbo.TeamAssignment (TeamID, [Status], AssignmentTime)
    INCLUDE (ReportID, CompletionTime);
END
GO

-- Resource and Inventory
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ResourceAllocation_EventID_Status_RequestTime'
      AND object_id = OBJECT_ID('dbo.ResourceAllocation')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ResourceAllocation_EventID_Status_RequestTime
    ON dbo.ResourceAllocation (EventID, [Status], RequestTime)
    INCLUDE (InventoryID, Quantity, RequestedBy, DispatchedAt, ConsumedAt);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ResourceAllocation_InventoryID_Status_RequestTime'
      AND object_id = OBJECT_ID('dbo.ResourceAllocation')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ResourceAllocation_InventoryID_Status_RequestTime
    ON dbo.ResourceAllocation (InventoryID, [Status], RequestTime)
    INCLUDE (EventID, Quantity);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_InventoryAlert_Status_AlertTime'
      AND object_id = OBJECT_ID('dbo.InventoryAlert')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventoryAlert_Status_AlertTime
    ON dbo.InventoryAlert ([Status], AlertTime)
    INCLUDE (InventoryID, AlertType, ResolvedAt);
END
GO

-- Financial and Audit
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Donation_EventID_Status_DonationDate'
      AND object_id = OBJECT_ID('dbo.Donation')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Donation_EventID_Status_DonationDate
    ON dbo.Donation (EventID, [Status], DonationDate)
    INCLUDE (Amount, DonorID);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Expense_EventID_ExpenseDate'
      AND object_id = OBJECT_ID('dbo.Expense')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Expense_EventID_ExpenseDate
    ON dbo.Expense (EventID, ExpenseDate)
    INCLUDE (Amount, Category, PaymentStatus, ApprovedBy);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AuditLog_TableName_Timestamp'
      AND object_id = OBJECT_ID('dbo.AuditLog')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLog_TableName_Timestamp
    ON dbo.AuditLog (TableName, [Timestamp])
    INCLUDE (UserID, [Action], RecordID);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AuditLog_Timestamp'
      AND object_id = OBJECT_ID('dbo.AuditLog')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLog_Timestamp
    ON dbo.AuditLog ([Timestamp])
    INCLUDE (UserID, [Action], TableName, RecordID);
END
GO

-- Approval and Security Workflows
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApprovalRequest_Status_RequestTime'
      AND object_id = OBJECT_ID('dbo.ApprovalRequest')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApprovalRequest_Status_RequestTime
    ON dbo.ApprovalRequest ([Status], RequestTime)
    INCLUDE (RequestType, RequestedBy, ReviewedBy, AllocationID, AssignmentID, ExpenseID);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApprovalHistory_RequestID_ActionTime'
      AND object_id = OBJECT_ID('dbo.ApprovalHistory')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApprovalHistory_RequestID_ActionTime
    ON dbo.ApprovalHistory (RequestID, ActionTime)
    INCLUDE (Decision, ActionBy);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_UserRole_RoleID_UserID'
      AND object_id = OBJECT_ID('dbo.UserRole')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_UserRole_RoleID_UserID
    ON dbo.UserRole (RoleID, UserID);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RolePermission_PermissionID_RoleID'
      AND object_id = OBJECT_ID('dbo.RolePermission')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RolePermission_PermissionID_RoleID
    ON dbo.RolePermission (PermissionID, RoleID);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PatientAdmission_Status_AdmissionTime'
      AND object_id = OBJECT_ID('dbo.PatientAdmission')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PatientAdmission_Status_AdmissionTime
    ON dbo.PatientAdmission ([Status], AdmissionTime)
    INCLUDE (HospitalID, PatientID, ReportID, [Condition]);
END
GO

PRINT 'Index creation script completed for Final_DB.';
GO
