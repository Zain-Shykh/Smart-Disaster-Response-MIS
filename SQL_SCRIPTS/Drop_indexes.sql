USE Final_DB;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmergencyReport_City_Province_ReportTime' AND object_id = OBJECT_ID('dbo.EmergencyReport'))
DROP INDEX IX_EmergencyReport_City_Province_ReportTime ON dbo.EmergencyReport;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmergencyReport_DisasterType_Status_ReportTime' AND object_id = OBJECT_ID('dbo.EmergencyReport'))
DROP INDEX IX_EmergencyReport_DisasterType_Status_ReportTime ON dbo.EmergencyReport;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DisasterEvent_DisasterType_Status_StartTime' AND object_id = OBJECT_ID('dbo.DisasterEvent'))
DROP INDEX IX_DisasterEvent_DisasterType_Status_StartTime ON dbo.DisasterEvent;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Resource_ResourceType_ResourceName' AND object_id = OBJECT_ID('dbo.Resource'))
DROP INDEX IX_Resource_ResourceType_ResourceName ON dbo.Resource;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Inventory_ResourceID_LastUpdated' AND object_id = OBJECT_ID('dbo.Inventory'))
DROP INDEX IX_Inventory_ResourceID_LastUpdated ON dbo.Inventory;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ResourceAllocation_Event_Status_RequestTime' AND object_id = OBJECT_ID('dbo.ResourceAllocation'))
DROP INDEX IX_ResourceAllocation_Event_Status_RequestTime ON dbo.ResourceAllocation;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ResourceAllocation_Inventory_Status_RequestTime' AND object_id = OBJECT_ID('dbo.ResourceAllocation'))
DROP INDEX IX_ResourceAllocation_Inventory_Status_RequestTime ON dbo.ResourceAllocation;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ResourceAllocation_Inventory_Status_RequestTime' AND object_id = OBJECT_ID('dbo.ResourceAllocation'))
DROP INDEX IX_ResourceAllocation_Inventory_Status_RequestTime ON dbo.ResourceAllocation;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Donation_Event_DonationDate' AND object_id = OBJECT_ID('dbo.Donation'))
DROP INDEX IX_Donation_Event_DonationDate ON dbo.Donation;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Expense_Event_ExpenseDate' AND object_id = OBJECT_ID('dbo.Expense'))
DROP INDEX IX_Expense_Event_ExpenseDate ON dbo.Expense;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalRequest_Status_RequestTime' AND object_id = OBJECT_ID('dbo.ApprovalRequest'))
DROP INDEX IX_ApprovalRequest_Status_RequestTime ON dbo.ApprovalRequest;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLog_TableName_Timestamp' AND object_id = OBJECT_ID('dbo.AuditLog'))
DROP INDEX IX_AuditLog_TableName_Timestamp ON dbo.AuditLog;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PatientAdmission_Hospital_Status_AdmissionTime' AND object_id = OBJECT_ID('dbo.PatientAdmission'))
DROP INDEX IX_PatientAdmission_Hospital_Status_AdmissionTime ON dbo.PatientAdmission;
GO