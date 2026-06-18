USE Final_DB;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
    Database Views for Smart Disaster Response MIS
    Supports role-specific reporting, frontend queries, and stored procedure integration.
    Assumes DDL.sql and triggers.sql are already deployed.
*/

-- ============================================================================
-- 1. INVENTORY & RESOURCE VIEWS
-- ============================================================================

DROP VIEW IF EXISTS dbo.vw_Inventory_Current;
GO
CREATE VIEW dbo.vw_Inventory_Current
AS
SELECT
    i.InventoryID,
    i.WarehouseID,
    i.ResourceID,
    i.Quantity,
    i.MinThreshold,
    i.MaxCapacity,
    i.LastUpdated
FROM dbo.Inventory i;
GO

DROP VIEW IF EXISTS dbo.vw_Inventory_Alerts;
GO
CREATE VIEW dbo.vw_Inventory_Alerts
AS
SELECT
    ia.InventoryID,
    ia.AlertID,
    ia.AlertType,
    ia.AlertTime,
    ia.[Status],
    ia.ResolvedAt,
    i.Quantity,
    i.MinThreshold
FROM dbo.InventoryAlert ia
INNER JOIN dbo.Inventory i ON i.InventoryID = ia.InventoryID;
GO

DROP VIEW IF EXISTS dbo.vw_ResourceAllocation_Status;
GO
CREATE VIEW dbo.vw_ResourceAllocation_Status
AS
SELECT
    ra.AllocationID,
    ra.InventoryID,
    ra.EventID,
    ra.Quantity,
    ra.[Status],
    ra.RequestedBy,
    ra.RequestTime,
    ra.DispatchedAt,
    ra.ConsumedAt
FROM dbo.ResourceAllocation ra;
GO

-- ============================================================================
-- 2. EMERGENCY REPORT VIEWS
-- ============================================================================

DROP VIEW IF EXISTS dbo.vw_EmergencyReports_Pending;
GO
CREATE VIEW dbo.vw_EmergencyReports_Pending
AS
SELECT
    er.ReportID,
    er.CitizenID,
    er.EventID,
    er.DisasterType,
    er.SeverityLevel,
    er.ReportTime,
    er.[Status],
    er.Street,
    er.Area,
    er.City
FROM dbo.EmergencyReport er
WHERE er.[Status] IN ('Pending', 'InProgress')
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.TeamAssignment ta
      WHERE ta.ReportID = er.ReportID
  );
GO

DROP VIEW IF EXISTS dbo.vw_EmergencyReports_ByEvent;
GO
CREATE VIEW dbo.vw_EmergencyReports_ByEvent
AS
SELECT
    er.ReportID,
    er.EventID,
    de.EventName,
    er.DisasterType,
    er.SeverityLevel,
    er.[Status],
    er.ReportTime
FROM dbo.EmergencyReport er
LEFT JOIN dbo.DisasterEvent de ON de.EventID = er.EventID;
GO

-- ============================================================================
-- 3. TEAM & ASSIGNMENT VIEWS
-- ============================================================================

DROP VIEW IF EXISTS dbo.vw_Teams_Availability;
GO
CREATE VIEW dbo.vw_Teams_Availability
AS
SELECT
    rt.TeamID,
    rt.TeamName,
    rt.TeamType,
    rt.AvailabilityStatus,
    rt.Capacity,
    rt.TotalAssignments,
    rt.Latitude,
    rt.Longitude
FROM dbo.RescueTeam rt;
GO

DROP VIEW IF EXISTS dbo.vw_Assignments_Detail;
GO
CREATE VIEW dbo.vw_Assignments_Detail
AS
SELECT
    ta.AssignmentID,
    ta.TeamID,
    ta.ReportID,
    CONCAT(er.Street, ', ', er.Area, ', ', er.City) AS ReportLocation,
    ta.AssignedBy,
    ta.AssignmentTime,
    ta.CompletionTime,
    ta.[Status]
FROM dbo.TeamAssignment ta
INNER JOIN dbo.EmergencyReport er ON er.ReportID = ta.ReportID;
GO

DROP VIEW IF EXISTS dbo.vw_TeamActivity_Log;
GO
CREATE VIEW dbo.vw_TeamActivity_Log
AS
SELECT
    tact.TeamID,
    tact.ActivityID,
    tact.ActivityType,
    tact.StartTime,
    tact.EndTime,
    tact.DurationMinutes,
    tact.Notes,
    tact.Outcome
FROM dbo.TeamActivity tact;
GO

-- ============================================================================
-- 4. APPROVAL & WORKFLOW VIEWS
-- ============================================================================

DROP VIEW IF EXISTS dbo.vw_Pending_Approvals;
GO
CREATE VIEW dbo.vw_Pending_Approvals
AS
SELECT
    ar.RequestID,
    ar.RequestType,
    ar.RequestedBy,
    ar.RequestTime,
    ar.AllocationID,
    ar.AssignmentID,
    ar.ExpenseID,
    ar.[Description]
FROM dbo.ApprovalRequest ar
WHERE ar.[Status] = 'Pending';
GO

DROP VIEW IF EXISTS dbo.vw_Approval_History;
GO
CREATE VIEW dbo.vw_Approval_History
AS
SELECT
    ah.RequestID,
    ah.HistoryID,
    ah.ActionBy,
    ah.ActionTime,
    ah.Decision,
    ah.Comments
FROM dbo.ApprovalHistory ah;
GO

-- ============================================================================
-- 5. HOSPITAL & PATIENT VIEWS
-- ============================================================================

DROP VIEW IF EXISTS dbo.vw_Hospital_Capacity;
GO
CREATE VIEW dbo.vw_Hospital_Capacity
AS
SELECT
    h.HospitalID,
    h.HospitalName,
    h.TotalBeds,
    h.AvailableBeds,
    h.OccupancyRate
FROM dbo.Hospital h;
GO

DROP VIEW IF EXISTS dbo.vw_Patient_Admissions;
GO
CREATE VIEW dbo.vw_Patient_Admissions
AS
SELECT
    pa.AdmissionID,
    pa.PatientID,
    pa.HospitalID,
    pa.AdmissionTime,
    pa.[Condition],
    pa.[Status],
    pa.ReportID
FROM dbo.PatientAdmission pa
WHERE pa.[Status] = 'Admitted';
GO

-- ============================================================================
-- 6. FINANCIAL VIEWS
-- ============================================================================

DROP VIEW IF EXISTS dbo.vw_Donations_Summary;
GO
CREATE VIEW dbo.vw_Donations_Summary
AS
SELECT
    d.DonationID,
    d.DonorID,
    d.EventID,
    d.Amount,
    d.DonationDate,
    d.[Status]
FROM dbo.Donation d;
GO

DROP VIEW IF EXISTS dbo.vw_Expenses_Summary;
GO
CREATE VIEW dbo.vw_Expenses_Summary
AS
SELECT
    e.ExpenseID,
    e.EventID,
    e.Category,
    e.Amount,
    e.ExpenseDate,
    e.PaymentStatus,
    e.ApprovedBy
FROM dbo.Expense e;
GO

DROP VIEW IF EXISTS dbo.vw_Budget_PerEvent;
GO
CREATE VIEW dbo.vw_Budget_PerEvent
AS
SELECT
    de.EventID,
    de.EventName,
    CAST(ISNULL(SUM(CASE WHEN d.[Status] = 'Confirmed' THEN d.Amount ELSE 0 END), 0) AS DECIMAL(14,2)) AS TotalDonations,
    CAST(ISNULL(SUM(e.Amount), 0) AS DECIMAL(14,2)) AS TotalExpenses,
    CAST(ISNULL(SUM(CASE WHEN d.[Status] = 'Confirmed' THEN d.Amount ELSE 0 END), 0) - ISNULL(SUM(e.Amount), 0) AS DECIMAL(14,2)) AS NetBudget
FROM dbo.DisasterEvent de
LEFT JOIN dbo.Donation d ON d.EventID = de.EventID
LEFT JOIN dbo.Expense e ON e.EventID = de.EventID
GROUP BY de.EventID, de.EventName;
GO

-- ============================================================================
-- 7. REPORTING & DASHBOARD VIEWS
-- ============================================================================

DROP VIEW IF EXISTS dbo.vw_Event_Overview;
GO
CREATE VIEW dbo.vw_Event_Overview
AS
SELECT
    de.EventID,
    de.EventName,
    de.StartTime,
    de.EndTime,
    de.[Status],
    de.AffectedPopulation,
    ISNULL((SELECT COUNT(*) FROM dbo.EmergencyReport WHERE EventID = de.EventID), 0) AS IncidentCount,
    ISNULL((SELECT COUNT(*) FROM dbo.ResourceAllocation WHERE EventID = de.EventID), 0) AS TotalAllocations,
    CAST(ISNULL((SELECT SUM(Amount) FROM dbo.Donation WHERE EventID = de.EventID AND [Status] = 'Confirmed'), 0) AS DECIMAL(14,2)) AS TotalDonations,
    CAST(ISNULL((SELECT SUM(Amount) FROM dbo.Expense WHERE EventID = de.EventID), 0) AS DECIMAL(14,2)) AS TotalExpenses
FROM dbo.DisasterEvent de;
GO

DROP VIEW IF EXISTS dbo.vw_Response_Performance;
GO
CREATE VIEW dbo.vw_Response_Performance
AS
SELECT
    de.EventID,
    CAST(AVG(CAST(DATEDIFF(MINUTE, er.ReportTime, ta.AssignmentTime) AS FLOAT)) AS DECIMAL(10,2)) AS AvgResponseTime,
    CAST(AVG(CAST(DATEDIFF(MINUTE, ta.AssignmentTime, ISNULL(ta.CompletionTime, GETUTCDATE())) AS FLOAT)) AS DECIMAL(10,2)) AS AvgTeamCompletionTime,
    CAST(
        CASE
            WHEN SUM(i.MaxCapacity) IS NULL OR SUM(i.MaxCapacity) = 0 THEN 0
            ELSE (SUM(i.Quantity) * 100.0 / SUM(i.MaxCapacity))
        END
        AS DECIMAL(10,2)
    ) AS ResourceUtilizationPercent
FROM dbo.DisasterEvent de
LEFT JOIN dbo.EmergencyReport er ON er.EventID = de.EventID
LEFT JOIN dbo.TeamAssignment ta ON ta.ReportID = er.ReportID
CROSS JOIN dbo.Inventory i
GROUP BY de.EventID;
GO

-- ============================================================================
-- 8. SECURITY & RBAC VIEWS
-- ============================================================================

DROP VIEW IF EXISTS dbo.vw_User_Roles_Permissions;
GO
CREATE VIEW dbo.vw_User_Roles_Permissions
AS
SELECT
    u.UserID,
    u.Username,
    r.RoleID,
    r.RoleName,
    p.PermissionID,
    p.PermissionName,
    p.[Module],
    p.[Action]
FROM dbo.[User] u
INNER JOIN dbo.UserRole ur ON ur.UserID = u.UserID
INNER JOIN dbo.[Role] r ON r.RoleID = ur.RoleID
INNER JOIN dbo.RolePermission rp ON rp.RoleID = r.RoleID
INNER JOIN dbo.Permission p ON p.PermissionID = rp.PermissionID;
GO

-- ============================================================================
-- 9. AUDIT & MONITORING VIEWS
-- ============================================================================

DROP VIEW IF EXISTS dbo.vw_Audit_Recent;
GO
CREATE VIEW dbo.vw_Audit_Recent
AS
SELECT TOP (1000)
    al.LogID,
    al.UserID,
    al.[Action],
    al.TableName,
    al.RecordID,
    al.[Timestamp],
    al.OldValue,
    al.NewValue
FROM dbo.AuditLog al
ORDER BY al.LogID DESC;
GO

DROP VIEW IF EXISTS dbo.vw_FinancialAuditTrail;
GO
CREATE VIEW dbo.vw_FinancialAuditTrail
AS
SELECT
    al.LogID,
    al.UserID,
    al.[Action],
    al.TableName,
    al.RecordID,
    al.[Timestamp],
    al.OldValue,
    al.NewValue
FROM dbo.AuditLog al
WHERE al.TableName IN ('Donation', 'Expense', 'ApprovalRequest')
ORDER BY al.LogID DESC
OFFSET 0 ROWS;
GO

PRINT 'All views created successfully for Final_DB.';
GO
