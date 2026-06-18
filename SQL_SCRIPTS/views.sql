USE Final_DB;
GO

/* =====================================================
   VIEWS FOR ROLE-SPECIFIC ACCESS AND REPORTING
   ===================================================== */

CREATE OR ALTER VIEW dbo.vw_FieldOfficer_IncidentQueue
AS
SELECT
    er.ReportID,
    er.ReportTime,
    er.DisasterType,
    er.SeverityLevel,
    er.Status AS ReportStatus,
    er.Street,
    er.Area,
    er.City,
    er.Province,
    de.EventID,
    de.EventName,
    de.Status AS EventStatus,
    de.AffectedPopulation,
    COUNT(ta.AssignmentID) AS AssignmentCount,
    MAX(ta.AssignmentTime) AS LastAssignmentTime,
    CASE er.SeverityLevel
        WHEN 'Critical' THEN 1
        WHEN 'High' THEN 2
        WHEN 'Medium' THEN 3
        ELSE 4
    END AS PriorityBucket
FROM EmergencyReport er
LEFT JOIN DisasterEvent de ON de.EventID = er.EventID
LEFT JOIN TeamAssignment ta ON ta.ReportID = er.ReportID
GROUP BY
    er.ReportID,
    er.ReportTime,
    er.DisasterType,
    er.SeverityLevel,
    er.Status,
    er.Street,
    er.Area,
    er.City,
    er.Province,
    de.EventID,
    de.EventName,
    de.Status,
    de.AffectedPopulation;
GO

CREATE OR ALTER VIEW dbo.vw_WarehouseManager_InventoryStatus
AS
SELECT
    i.InventoryID,
    w.WarehouseID,
    w.WarehouseName,
    w.City,
    w.Province,
    r.ResourceID,
    r.ResourceName,
    r.ResourceType,
    i.Quantity,
    i.MinThreshold,
    i.MaxCapacity,
    i.LastUpdated,
    CASE
        WHEN i.Quantity <= 0 THEN 'OutOfStock'
        WHEN i.Quantity <= i.MinThreshold THEN 'LowStock'
        ELSE 'Healthy'
    END AS StockState,
    MAX(CASE WHEN ia.Status = 'Active' THEN ia.AlertType END) AS ActiveAlertType,
    MAX(CASE WHEN ia.Status = 'Active' THEN ia.AlertTime END) AS ActiveAlertTime
FROM Inventory i
JOIN Warehouse w ON w.WarehouseID = i.WarehouseID
JOIN Resource r ON r.ResourceID = i.ResourceID
LEFT JOIN InventoryAlert ia ON ia.InventoryID = i.InventoryID
GROUP BY
    i.InventoryID,
    w.WarehouseID,
    w.WarehouseName,
    w.City,
    w.Province,
    r.ResourceID,
    r.ResourceName,
    r.ResourceType,
    i.Quantity,
    i.MinThreshold,
    i.MaxCapacity,
    i.LastUpdated;
GO

CREATE OR ALTER VIEW dbo.vw_FinanceOfficer_EventFinancialSummary
AS
WITH DonationAgg AS
(
    SELECT
        d.EventID,
        SUM(d.Amount) AS DonationTotal,
        SUM(CASE WHEN d.Status = 'Confirmed' THEN d.Amount ELSE 0 END) AS ConfirmedDonationTotal,
        SUM(CASE WHEN d.Status = 'Pending' THEN d.Amount ELSE 0 END) AS PendingDonationTotal
    FROM Donation d
    GROUP BY d.EventID
),
ExpenseAgg AS
(
    SELECT
        e.EventID,
        SUM(e.Amount) AS ExpenseTotal,
        SUM(CASE WHEN e.PaymentStatus IN ('Paid', 'Completed') THEN e.Amount ELSE 0 END) AS SettledExpenseTotal,
        SUM(CASE WHEN e.PaymentStatus IN ('Pending', 'PendingApproval') THEN e.Amount ELSE 0 END) AS PendingExpenseTotal
    FROM Expense e
    GROUP BY e.EventID
)
SELECT
    de.EventID,
    de.EventName,
    de.DisasterType,
    de.Status AS EventStatus,
    ISNULL(da.DonationTotal, 0.00) AS DonationTotal,
    ISNULL(da.ConfirmedDonationTotal, 0.00) AS ConfirmedDonationTotal,
    ISNULL(da.PendingDonationTotal, 0.00) AS PendingDonationTotal,
    ISNULL(ea.ExpenseTotal, 0.00) AS ExpenseTotal,
    ISNULL(ea.SettledExpenseTotal, 0.00) AS SettledExpenseTotal,
    ISNULL(ea.PendingExpenseTotal, 0.00) AS PendingExpenseTotal,
    ISNULL(da.ConfirmedDonationTotal, 0.00) - ISNULL(ea.ExpenseTotal, 0.00) AS NetPosition
FROM DisasterEvent de
LEFT JOIN DonationAgg da ON da.EventID = de.EventID
LEFT JOIN ExpenseAgg ea ON ea.EventID = de.EventID;
GO

CREATE OR ALTER VIEW dbo.vw_ApprovalWorkflow_Operational
AS
SELECT
    ar.RequestID,
    ar.RequestType,
    ar.RequestTime,
    ar.Status,
    ar.Description,
    req.UserID AS RequestedByUserID,
    req.Username AS RequestedByUsername,
    rev.UserID AS ReviewedByUserID,
    rev.Username AS ReviewedByUsername,
    ar.AllocationID,
    ar.AssignmentID,
    ar.ExpenseID,
    CASE
        WHEN ar.AllocationID IS NOT NULL THEN 'ResourceAllocation'
        WHEN ar.AssignmentID IS NOT NULL THEN 'TeamAssignment'
        WHEN ar.ExpenseID IS NOT NULL THEN 'Expense'
        ELSE 'Unknown'
    END AS TargetEntity
FROM ApprovalRequest ar
JOIN [User] req ON req.UserID = ar.RequestedBy
LEFT JOIN [User] rev ON rev.UserID = ar.ReviewedBy;
GO

CREATE OR ALTER VIEW dbo.vw_Admin_AuditTrail
AS
SELECT
    al.LogID,
    al.[Timestamp],
    al.[Action],
    al.TableName,
    al.RecordID,
    al.IPAddress,
    al.UserID,
    u.Username,
    u.Email,
    al.OldValue,
    al.NewValue
FROM AuditLog al
LEFT JOIN [User] u ON u.UserID = al.UserID;
GO

/* =====================================================
   ADDITIONAL VIEWS FOR SYSTEM ANALYTICS & DASHBOARDS
   ===================================================== */

-- Inventory & Resource Views
CREATE OR ALTER VIEW dbo.vw_Inventory_Current AS SELECT * FROM Inventory;
GO
CREATE OR ALTER VIEW dbo.vw_Inventory_Alerts AS SELECT * FROM InventoryAlert;
GO
CREATE OR ALTER VIEW dbo.vw_ResourceAllocation_Status AS SELECT * FROM ResourceAllocation;
GO

-- Emergency Report Views
CREATE OR ALTER VIEW dbo.vw_EmergencyReports_Pending AS SELECT * FROM EmergencyReport WHERE Status = 'Pending';
GO
CREATE OR ALTER VIEW dbo.vw_EmergencyReports_ByEvent AS 
SELECT er.*, de.EventName 
FROM EmergencyReport er 
LEFT JOIN DisasterEvent de ON er.EventID = de.EventID;
GO

-- Team & Assignment Views
CREATE OR ALTER VIEW dbo.vw_Teams_Availability AS SELECT * FROM RescueTeam;
GO
CREATE OR ALTER VIEW dbo.vw_Assignments_Detail AS SELECT * FROM TeamAssignment;
GO
CREATE OR ALTER VIEW dbo.vw_TeamActivity_Log AS SELECT * FROM TeamActivity;
GO

-- Approval & Workflow Views
CREATE OR ALTER VIEW dbo.vw_Pending_Approvals AS SELECT * FROM ApprovalRequest WHERE Status = 'Pending';
GO
CREATE OR ALTER VIEW dbo.vw_Approval_History AS SELECT * FROM ApprovalHistory;
GO

-- Hospital & Patient Views
CREATE OR ALTER VIEW dbo.vw_Hospital_Capacity AS SELECT * FROM Hospital;
GO
CREATE OR ALTER VIEW dbo.vw_Patient_Admissions AS SELECT * FROM PatientAdmission;
GO

-- Financial Views
CREATE OR ALTER VIEW dbo.vw_Donations_Summary AS SELECT * FROM Donation;
GO
CREATE OR ALTER VIEW dbo.vw_Expenses_Summary AS SELECT * FROM Expense;
GO
CREATE OR ALTER VIEW dbo.vw_Budget_PerEvent AS 
SELECT 
    de.EventID, de.EventName,
    ISNULL(SUM(d.Amount), 0) AS TotalDonations,
    ISNULL(SUM(e.Amount), 0) AS TotalExpenses,
    ISNULL(SUM(d.Amount), 0) - ISNULL(SUM(e.Amount), 0) AS NetBudget
FROM DisasterEvent de
LEFT JOIN Donation d ON de.EventID = d.EventID AND d.Status = 'Confirmed'
LEFT JOIN Expense e ON de.EventID = e.EventID
GROUP BY de.EventID, de.EventName;
GO

-- Reporting & Dashboard Views
CREATE OR ALTER VIEW dbo.vw_Event_Overview AS 
SELECT 
    de.EventID, de.EventName, de.StartTime, de.EndTime, de.Status, de.AffectedPopulation,
    (SELECT COUNT(*) FROM EmergencyReport WHERE EventID = de.EventID) AS IncidentCount,
    (SELECT COUNT(*) FROM ResourceAllocation WHERE EventID = de.EventID) AS TotalAllocations,
    ISNULL((SELECT SUM(Amount) FROM Donation WHERE EventID = de.EventID AND Status = 'Confirmed'), 0) AS TotalDonations,
    ISNULL((SELECT SUM(Amount) FROM Expense WHERE EventID = de.EventID), 0) AS TotalExpenses
FROM DisasterEvent de;
GO

CREATE OR ALTER VIEW dbo.vw_Response_Performance AS 
SELECT 
    er.EventID,
    AVG(CAST(er.ResponseTimeMinutes AS DECIMAL(10,2))) AS AvgResponseTime,
    AVG(CAST(er.ResolutionTimeMinutes AS DECIMAL(10,2))) AS AvgTeamCompletionTime,
    CAST(0.0 AS DECIMAL(5,2)) AS ResourceUtilizationPercent
FROM EmergencyReport er
WHERE er.EventID IS NOT NULL
GROUP BY er.EventID;
GO

-- Security & RBAC Views
CREATE OR ALTER VIEW dbo.vw_User_Roles_Permissions AS 
SELECT 
    u.UserID, u.Username, r.RoleID, r.RoleName, p.PermissionID, p.PermissionName, p.Module, p.Action
FROM [User] u
JOIN UserRole ur ON u.UserID = ur.UserID
JOIN Role r ON ur.RoleID = r.RoleID
JOIN RolePermission rp ON r.RoleID = rp.RoleID
JOIN Permission p ON rp.PermissionID = p.PermissionID;
GO

-- Audit & Monitoring Views
CREATE OR ALTER VIEW dbo.vw_Audit_Recent AS SELECT TOP 1000 * FROM AuditLog ORDER BY Timestamp DESC;
GO
CREATE OR ALTER VIEW dbo.vw_FinancialAuditTrail AS 
SELECT * FROM AuditLog WHERE TableName IN ('Donation', 'Expense', 'ApprovalRequest');
GO
