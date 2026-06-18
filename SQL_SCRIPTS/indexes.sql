USE Final_DB;
GO

/* =====================================================
   CUSTOM INDEXING STRATEGY
   - Single-column and composite indexes for high-frequency filters
   - Safe rerun using IF NOT EXISTS checks
   ===================================================== */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmergencyReport_City_Province_ReportTime' AND object_id = OBJECT_ID('dbo.EmergencyReport'))
BEGIN
    CREATE INDEX IX_EmergencyReport_City_Province_ReportTime
    ON dbo.EmergencyReport (City, Province, ReportTime DESC)
    INCLUDE (SeverityLevel, Status, EventID, DisasterType);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmergencyReport_DisasterType_Status_ReportTime' AND object_id = OBJECT_ID('dbo.EmergencyReport'))
BEGIN
    CREATE INDEX IX_EmergencyReport_DisasterType_Status_ReportTime
    ON dbo.EmergencyReport (DisasterType, Status, ReportTime DESC)
    INCLUDE (City, Province, SeverityLevel, EventID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DisasterEvent_DisasterType_Status_StartTime' AND object_id = OBJECT_ID('dbo.DisasterEvent'))
BEGIN
    CREATE INDEX IX_DisasterEvent_DisasterType_Status_StartTime
    ON dbo.DisasterEvent (DisasterType, Status, StartTime DESC)
    INCLUDE (City, Province, AffectedPopulation);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Resource_ResourceType_ResourceName' AND object_id = OBJECT_ID('dbo.Resource'))
BEGIN
    CREATE INDEX IX_Resource_ResourceType_ResourceName
    ON dbo.Resource (ResourceType, ResourceName)
    INCLUDE (Unit);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Inventory_ResourceID_LastUpdated' AND object_id = OBJECT_ID('dbo.Inventory'))
BEGIN
    CREATE INDEX IX_Inventory_ResourceID_LastUpdated
    ON dbo.Inventory (ResourceID, LastUpdated DESC)
    INCLUDE (WarehouseID, Quantity, MinThreshold, MaxCapacity);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ResourceAllocation_Event_Status_RequestTime' AND object_id = OBJECT_ID('dbo.ResourceAllocation'))
BEGIN
    CREATE INDEX IX_ResourceAllocation_Event_Status_RequestTime
    ON dbo.ResourceAllocation (EventID, Status, RequestTime DESC)
    INCLUDE (InventoryID, Quantity, RequestedBy, DispatchedAt, ConsumedAt);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ResourceAllocation_Inventory_Status_RequestTime' AND object_id = OBJECT_ID('dbo.ResourceAllocation'))
BEGIN
    CREATE INDEX IX_ResourceAllocation_Inventory_Status_RequestTime
    ON dbo.ResourceAllocation (InventoryID, Status, RequestTime DESC)
    INCLUDE (EventID, Quantity, RequestedBy, DispatchedAt, ConsumedAt);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Donation_Event_DonationDate' AND object_id = OBJECT_ID('dbo.Donation'))
BEGIN
    CREATE INDEX IX_Donation_Event_DonationDate
    ON dbo.Donation (EventID, DonationDate DESC)
    INCLUDE (Amount, Status, DonorID, PaymentMethod);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Expense_Event_ExpenseDate' AND object_id = OBJECT_ID('dbo.Expense'))
BEGIN
    CREATE INDEX IX_Expense_Event_ExpenseDate
    ON dbo.Expense (EventID, ExpenseDate DESC)
    INCLUDE (Amount, Category, PaymentStatus, ApprovedBy);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalRequest_Status_RequestTime' AND object_id = OBJECT_ID('dbo.ApprovalRequest'))
BEGIN
    CREATE INDEX IX_ApprovalRequest_Status_RequestTime
    ON dbo.ApprovalRequest (Status, RequestTime DESC)
    INCLUDE (RequestType, RequestedBy, ReviewedBy, AllocationID, AssignmentID, ExpenseID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLog_TableName_Timestamp' AND object_id = OBJECT_ID('dbo.AuditLog'))
BEGIN
    CREATE INDEX IX_AuditLog_TableName_Timestamp
    ON dbo.AuditLog (TableName, [Timestamp] DESC)
    INCLUDE ([Action], RecordID, UserID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PatientAdmission_Hospital_Status_AdmissionTime' AND object_id = OBJECT_ID('dbo.PatientAdmission'))
BEGIN
    CREATE INDEX IX_PatientAdmission_Hospital_Status_AdmissionTime
    ON dbo.PatientAdmission (HospitalID, Status, AdmissionTime DESC)
    INCLUDE (PatientID, ReportID, [Condition], DischargeTime);
END
GO
