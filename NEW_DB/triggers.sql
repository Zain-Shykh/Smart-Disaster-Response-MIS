USE Final_DB;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- SQL Server implementation for the triggers defined in finalized_triggers.md.
-- Note: SQL Server does not support BEFORE triggers, so validation triggers
-- are implemented as AFTER triggers with rollback checks.

/* -------------------------------------------------------------------------- */
/* 1) Resource Management                                                     */
/* -------------------------------------------------------------------------- */

DROP TRIGGER IF EXISTS dbo.trg_prevent_negative_inventory;
GO
CREATE TRIGGER dbo.trg_prevent_negative_inventory
ON dbo.ResourceAllocation
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Inventory inv ON inv.InventoryID = i.InventoryID
        GROUP BY i.InventoryID, inv.Quantity
        HAVING SUM(i.Quantity) > MAX(inv.Quantity)
    )
    BEGIN
        RAISERROR('Requested resource allocation exceeds available inventory.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_deduct_inventory;
GO
CREATE TRIGGER dbo.trg_deduct_inventory
ON dbo.ResourceAllocation
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH ApprovedAllocations AS (
        SELECT
            i.InventoryID,
            SUM(i.Quantity) AS QuantityToDeduct
        FROM inserted i
        JOIN deleted d ON d.AllocationID = i.AllocationID
        WHERE i.[Status] = 'Approved'
          AND ISNULL(d.[Status], '') <> 'Approved'
        GROUP BY i.InventoryID
    )
    UPDATE inv
    SET inv.Quantity = inv.Quantity - aa.QuantityToDeduct,
        inv.LastUpdated = SYSUTCDATETIME()
    FROM dbo.Inventory inv
    JOIN ApprovedAllocations aa ON aa.InventoryID = inv.InventoryID;
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_check_inventory_threshold;
GO
CREATE TRIGGER dbo.trg_check_inventory_threshold
ON dbo.Inventory
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH LowStockRows AS (
        SELECT
            i.InventoryID,
            i.Quantity,
            i.MinThreshold,
            ROW_NUMBER() OVER (PARTITION BY i.InventoryID ORDER BY i.InventoryID) AS rn
        FROM inserted i
        JOIN deleted d ON d.InventoryID = i.InventoryID
        WHERE i.Quantity < i.MinThreshold
          AND i.Quantity <> d.Quantity
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.InventoryAlert ia
              WHERE ia.InventoryID = i.InventoryID
                AND ia.[Status] = 'Active'
          )
    ), MaxAlertIds AS (
        SELECT InventoryID, ISNULL(MAX(AlertID), 0) AS MaxAlertID
        FROM dbo.InventoryAlert
        GROUP BY InventoryID
    )
    INSERT INTO dbo.InventoryAlert (InventoryID, AlertID, AlertType, AlertTime, [Status], ResolvedAt)
    SELECT
        l.InventoryID,
        ISNULL(m.MaxAlertID, 0) + l.rn,
        CASE WHEN l.Quantity = 0 THEN 'OutOfStock' ELSE 'LowStock' END,
        SYSUTCDATETIME(),
        'Active',
        NULL
    FROM LowStockRows l
    LEFT JOIN MaxAlertIds m ON m.InventoryID = l.InventoryID;
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_resolve_inventory_alert;
GO
CREATE TRIGGER dbo.trg_resolve_inventory_alert
ON dbo.Inventory
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE ia
    SET ia.[Status] = 'Resolved',
        ia.ResolvedAt = SYSUTCDATETIME()
    FROM dbo.InventoryAlert ia
    JOIN inserted i ON i.InventoryID = ia.InventoryID
    JOIN deleted d ON d.InventoryID = i.InventoryID
    WHERE i.Quantity >= i.MinThreshold
      AND i.Quantity <> d.Quantity
      AND ia.[Status] = 'Active';
END;
GO

/* -------------------------------------------------------------------------- */
/* 2) Rescue Team Management                                                  */
/* -------------------------------------------------------------------------- */

DROP TRIGGER IF EXISTS dbo.trg_prevent_double_assignment;
GO
CREATE TRIGGER dbo.trg_prevent_double_assignment
ON dbo.TeamAssignment
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.RescueTeam rt ON rt.TeamID = i.TeamID
        WHERE rt.AvailabilityStatus <> 'Available'
    )
    BEGIN
        RAISERROR('This rescue team is not available for a new assignment.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_team_assigned;
GO
CREATE TRIGGER dbo.trg_team_assigned
ON dbo.TeamAssignment
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE rt
    SET rt.AvailabilityStatus = 'Assigned',
        rt.TotalAssignments = rt.TotalAssignments + x.AssignmentCount
    FROM dbo.RescueTeam rt
    JOIN (
        SELECT TeamID, COUNT(*) AS AssignmentCount
        FROM inserted
        GROUP BY TeamID
    ) x ON x.TeamID = rt.TeamID;
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_team_busy_on_approval;
GO
CREATE TRIGGER dbo.trg_team_busy_on_approval
ON dbo.ApprovalRequest
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE rt
    SET rt.AvailabilityStatus = 'Busy'
    FROM dbo.RescueTeam rt
    JOIN dbo.TeamAssignment ta ON ta.TeamID = rt.TeamID
    JOIN inserted i ON i.AssignmentID = ta.AssignmentID
    JOIN deleted d ON d.RequestID = i.RequestID
    WHERE i.RequestType = 'RescueDeployment'
      AND i.[Status] = 'Approved'
      AND ISNULL(d.[Status], '') <> 'Approved';
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_team_completed;
GO
CREATE TRIGGER dbo.trg_team_completed
ON dbo.TeamAssignment
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE rt
    SET rt.AvailabilityStatus = 'Available'
    FROM dbo.RescueTeam rt
    JOIN inserted i ON i.TeamID = rt.TeamID
    JOIN deleted d ON d.AssignmentID = i.AssignmentID
    WHERE i.[Status] = 'Completed'
      AND ISNULL(d.[Status], '') <> 'Completed';
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_free_team_on_rejection;
GO
CREATE TRIGGER dbo.trg_free_team_on_rejection
ON dbo.ApprovalRequest
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE rt
    SET rt.AvailabilityStatus = 'Available'
    FROM dbo.RescueTeam rt
    JOIN dbo.TeamAssignment ta ON ta.TeamID = rt.TeamID
    JOIN inserted i ON i.AssignmentID = ta.AssignmentID
    JOIN deleted d ON d.RequestID = i.RequestID
    WHERE i.RequestType = 'RescueDeployment'
      AND i.[Status] = 'Rejected'
      AND ISNULL(d.[Status], '') <> 'Rejected';
END;
GO

/* -------------------------------------------------------------------------- */
/* 3) Hospital Management                                                     */
/* -------------------------------------------------------------------------- */

DROP TRIGGER IF EXISTS dbo.trg_prevent_bed_overflow;
GO
CREATE TRIGGER dbo.trg_prevent_bed_overflow
ON dbo.PatientAdmission
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Hospital h ON h.HospitalID = i.HospitalID
        GROUP BY i.HospitalID, h.AvailableBeds
        HAVING COUNT(*) > MAX(h.AvailableBeds)
    )
    BEGIN
        RAISERROR('Hospital does not have enough available beds for the admission.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_decrement_beds;
GO
CREATE TRIGGER dbo.trg_decrement_beds
ON dbo.PatientAdmission
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE h
    SET h.AvailableBeds = h.AvailableBeds - x.AdmissionCount
    FROM dbo.Hospital h
    JOIN (
        SELECT HospitalID, COUNT(*) AS AdmissionCount
        FROM inserted
        GROUP BY HospitalID
    ) x ON x.HospitalID = h.HospitalID;
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_increment_beds;
GO
CREATE TRIGGER dbo.trg_increment_beds
ON dbo.PatientAdmission
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH ReleasedBeds AS (
        SELECT
            i.HospitalID,
            COUNT(*) AS ReleaseCount
        FROM inserted i
        JOIN deleted d ON d.AdmissionID = i.AdmissionID
        WHERE d.[Status] = 'Admitted'
          AND i.[Status] IN ('Discharged', 'Transferred')
        GROUP BY i.HospitalID
    )
    UPDATE h
    SET h.AvailableBeds = h.AvailableBeds + rb.ReleaseCount
    FROM dbo.Hospital h
    JOIN ReleasedBeds rb ON rb.HospitalID = h.HospitalID;
END;
GO

/* -------------------------------------------------------------------------- */
/* 4) Approval Request - Auto Creation                                         */
/* -------------------------------------------------------------------------- */

DROP TRIGGER IF EXISTS dbo.trg_create_approval_on_allocation;
GO
CREATE TRIGGER dbo.trg_create_approval_on_allocation
ON dbo.ResourceAllocation
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ApprovalRequest
    (
        RequestedBy,
        ReviewedBy,
        RequestType,
        RequestTime,
        [Status],
        [Description],
        AllocationID,
        AssignmentID,
        ExpenseID
    )
    SELECT
        i.RequestedBy,
        NULL,
        'ResourceDistribution',
        SYSUTCDATETIME(),
        'Pending',
        CONCAT('Auto-created approval request for resource allocation ', i.AllocationID),
        i.AllocationID,
        NULL,
        NULL
    FROM inserted i;
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_create_approval_on_assignment;
GO
CREATE TRIGGER dbo.trg_create_approval_on_assignment
ON dbo.TeamAssignment
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ApprovalRequest
    (
        RequestedBy,
        ReviewedBy,
        RequestType,
        RequestTime,
        [Status],
        [Description],
        AllocationID,
        AssignmentID,
        ExpenseID
    )
    SELECT
        i.AssignedBy,
        NULL,
        'RescueDeployment',
        SYSUTCDATETIME(),
        'Pending',
        CONCAT('Auto-created approval request for team assignment ', i.AssignmentID),
        NULL,
        i.AssignmentID,
        NULL
    FROM inserted i;
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_create_approval_on_expense;
GO
CREATE TRIGGER dbo.trg_create_approval_on_expense
ON dbo.Expense
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentUserID INT = TRY_CAST(SESSION_CONTEXT(N'UserID') AS INT);

    IF @CurrentUserID IS NULL
    BEGIN
        SELECT TOP (1) @CurrentUserID = UserID
        FROM dbo.[User]
        ORDER BY UserID;
    END;

    IF @CurrentUserID IS NULL
    BEGIN
        RAISERROR('Cannot auto-create an approval request for Expense because no user context is available.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    INSERT INTO dbo.ApprovalRequest
    (
        RequestedBy,
        ReviewedBy,
        RequestType,
        RequestTime,
        [Status],
        [Description],
        AllocationID,
        AssignmentID,
        ExpenseID
    )
    SELECT
        @CurrentUserID,
        NULL,
        'Financial',
        SYSUTCDATETIME(),
        'Pending',
        CONCAT('Auto-created approval request for expense ', i.ExpenseID),
        NULL,
        NULL,
        i.ExpenseID
    FROM inserted i;
END;
GO

/* -------------------------------------------------------------------------- */
/* 5) Approval Workflow - Cascade on Decision                                 */
/* -------------------------------------------------------------------------- */

DROP TRIGGER IF EXISTS dbo.trg_single_approval_fk;
GO
CREATE TRIGGER dbo.trg_single_approval_fk
ON dbo.ApprovalRequest
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted
        WHERE (
            CASE WHEN AllocationID IS NOT NULL THEN 1 ELSE 0 END +
            CASE WHEN AssignmentID IS NOT NULL THEN 1 ELSE 0 END +
            CASE WHEN ExpenseID IS NOT NULL THEN 1 ELSE 0 END
        ) <> 1
    )
    BEGIN
        RAISERROR('ApprovalRequest must reference exactly one target record.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_approval_granted;
GO
CREATE TRIGGER dbo.trg_approval_granted
ON dbo.ApprovalRequest
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE ra
    SET ra.[Status] = 'Approved'
    FROM dbo.ResourceAllocation ra
    JOIN inserted i ON i.AllocationID = ra.AllocationID
    JOIN deleted d ON d.RequestID = i.RequestID
    WHERE i.RequestType = 'ResourceDistribution'
      AND i.[Status] = 'Approved'
      AND ISNULL(d.[Status], '') <> 'Approved';

    UPDATE ta
    SET ta.[Status] = 'Assigned'
    FROM dbo.TeamAssignment ta
    JOIN inserted i ON i.AssignmentID = ta.AssignmentID
    JOIN deleted d ON d.RequestID = i.RequestID
    WHERE i.RequestType = 'RescueDeployment'
      AND i.[Status] = 'Approved'
      AND ISNULL(d.[Status], '') <> 'Approved';

    UPDATE e
    SET e.PaymentStatus = 'Approved'
    FROM dbo.Expense e
    JOIN inserted i ON i.ExpenseID = e.ExpenseID
    JOIN deleted d ON d.RequestID = i.RequestID
    WHERE i.RequestType = 'Financial'
      AND i.[Status] = 'Approved'
      AND ISNULL(d.[Status], '') <> 'Approved';
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_approval_rejected;
GO
CREATE TRIGGER dbo.trg_approval_rejected
ON dbo.ApprovalRequest
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE ra
    SET ra.[Status] = 'Rejected'
    FROM dbo.ResourceAllocation ra
    JOIN inserted i ON i.AllocationID = ra.AllocationID
    JOIN deleted d ON d.RequestID = i.RequestID
    WHERE i.RequestType = 'ResourceDistribution'
      AND i.[Status] = 'Rejected'
      AND ISNULL(d.[Status], '') <> 'Rejected';

    UPDATE e
    SET e.PaymentStatus = 'Rejected'
    FROM dbo.Expense e
    JOIN inserted i ON i.ExpenseID = e.ExpenseID
    JOIN deleted d ON d.RequestID = i.RequestID
    WHERE i.RequestType = 'Financial'
      AND i.[Status] = 'Rejected'
      AND ISNULL(d.[Status], '') <> 'Rejected';

    UPDATE rt
    SET rt.AvailabilityStatus = 'Available'
    FROM dbo.RescueTeam rt
    JOIN dbo.TeamAssignment ta ON ta.TeamID = rt.TeamID
    JOIN inserted i ON i.AssignmentID = ta.AssignmentID
    JOIN deleted d ON d.RequestID = i.RequestID
    WHERE i.RequestType = 'RescueDeployment'
      AND i.[Status] = 'Rejected'
      AND ISNULL(d.[Status], '') <> 'Rejected';
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_set_expense_approvedby;
GO
CREATE TRIGGER dbo.trg_set_expense_approvedby
ON dbo.ApprovalRequest
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE e
    SET e.ApprovedBy = COALESCE(i.ReviewedBy, e.ApprovedBy),
        e.PaymentStatus = 'Confirmed'
    FROM dbo.Expense e
    JOIN inserted i ON i.ExpenseID = e.ExpenseID
    JOIN deleted d ON d.RequestID = i.RequestID
    WHERE i.RequestType = 'Financial'
      AND i.[Status] = 'Approved'
      AND ISNULL(d.[Status], '') <> 'Approved';
END;
GO

/* -------------------------------------------------------------------------- */
/* 6) Activity & History Logging                                              */
/* -------------------------------------------------------------------------- */

DROP TRIGGER IF EXISTS dbo.trg_log_approval_history;
GO
CREATE TRIGGER dbo.trg_log_approval_history
ON dbo.ApprovalRequest
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Changes AS (
        SELECT
            i.RequestID,
            COALESCE(i.ReviewedBy, TRY_CAST(SESSION_CONTEXT(N'UserID') AS INT)) AS ActionBy,
            i.[Status] AS Decision,
            SYSUTCDATETIME() AS ActionTime,
            ROW_NUMBER() OVER (PARTITION BY i.RequestID ORDER BY i.RequestID) AS rn
        FROM inserted i
        JOIN deleted d ON d.RequestID = i.RequestID
        WHERE i.[Status] IN ('Approved', 'Rejected', 'Escalated')
          AND ISNULL(i.[Status], '') <> ISNULL(d.[Status], '')
    ), MaxHistory AS (
        SELECT RequestID, ISNULL(MAX(HistoryID), 0) AS MaxHistoryID
        FROM dbo.ApprovalHistory
        GROUP BY RequestID
    )
    INSERT INTO dbo.ApprovalHistory
    (
        RequestID,
        HistoryID,
        ActionBy,
        ActionTime,
        Decision,
        Comments
    )
    SELECT
        c.RequestID,
        ISNULL(m.MaxHistoryID, 0) + c.rn,
        c.ActionBy,
        c.ActionTime,
        c.Decision,
        NULL
    FROM Changes c
    LEFT JOIN MaxHistory m ON m.RequestID = c.RequestID;
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_log_team_activity;
GO
CREATE TRIGGER dbo.trg_log_team_activity
ON dbo.TeamAssignment
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Changes AS (
        SELECT
            i.TeamID,
            i.AssignmentID,
            i.[Status] AS NewStatus,
            d.[Status] AS OldStatus,
            SYSUTCDATETIME() AS StartTime,
            ROW_NUMBER() OVER (PARTITION BY i.TeamID ORDER BY i.AssignmentID) AS rn
        FROM inserted i
        JOIN deleted d ON d.AssignmentID = i.AssignmentID
        WHERE ISNULL(i.[Status], '') <> ISNULL(d.[Status], '')
    ), MaxActivity AS (
        SELECT TeamID, ISNULL(MAX(ActivityID), 0) AS MaxActivityID
        FROM dbo.TeamActivity
        GROUP BY TeamID
    )
    INSERT INTO dbo.TeamActivity
    (
        TeamID,
        ActivityID,
        ActivityType,
        StartTime,
        EndTime,
        Notes,
        Outcome
    )
    SELECT
        c.TeamID,
        ISNULL(m.MaxActivityID, 0) + c.rn,
        c.NewStatus,
        c.StartTime,
        NULL,
        CONCAT('Status changed from ', COALESCE(c.OldStatus, 'NULL'), ' to ', c.NewStatus),
        NULL
    FROM Changes c
    LEFT JOIN MaxActivity m ON m.TeamID = c.TeamID;
END;
GO

/* -------------------------------------------------------------------------- */
/* 7) Audit Logging                                                           */
/* -------------------------------------------------------------------------- */

DROP TRIGGER IF EXISTS dbo.trg_audit_emergency_report;
GO
CREATE TRIGGER dbo.trg_audit_emergency_report
ON dbo.EmergencyReport
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLog
    (
        UserID,
        [Action],
        TableName,
        RecordID,
        OldValue,
        NewValue,
        [Timestamp],
        IPAddress
    )
    SELECT
        TRY_CAST(SESSION_CONTEXT(N'UserID') AS INT),
        CASE WHEN d.ReportID IS NULL THEN 'INSERT' ELSE 'UPDATE' END,
        'EmergencyReport',
        CAST(i.ReportID AS VARCHAR(120)),
        CASE
            WHEN d.ReportID IS NULL THEN NULL
            ELSE CONCAT(
                'CitizenID=', d.CitizenID,
                '; EventID=', COALESCE(CAST(d.EventID AS VARCHAR(30)), 'NULL'),
                '; DisasterType=', d.DisasterType,
                '; SeverityLevel=', d.SeverityLevel,
                '; Status=', d.[Status],
                '; Source=', d.[Source],
                '; ReportTime=', CONVERT(VARCHAR(30), d.ReportTime, 126)
            )
        END,
        CONCAT(
            'CitizenID=', i.CitizenID,
            '; EventID=', COALESCE(CAST(i.EventID AS VARCHAR(30)), 'NULL'),
            '; DisasterType=', i.DisasterType,
            '; SeverityLevel=', i.SeverityLevel,
            '; Status=', i.[Status],
            '; Source=', i.[Source],
            '; ReportTime=', CONVERT(VARCHAR(30), i.ReportTime, 126)
        ),
        SYSUTCDATETIME(),
        NULL
    FROM inserted i
    LEFT JOIN deleted d ON d.ReportID = i.ReportID;
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_audit_donation;
GO
CREATE TRIGGER dbo.trg_audit_donation
ON dbo.Donation
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLog
    (
        UserID,
        [Action],
        TableName,
        RecordID,
        OldValue,
        NewValue,
        [Timestamp],
        IPAddress
    )
    SELECT
        TRY_CAST(SESSION_CONTEXT(N'UserID') AS INT),
        CASE WHEN d.DonationID IS NULL THEN 'INSERT' ELSE 'UPDATE' END,
        'Donation',
        CAST(i.DonationID AS VARCHAR(120)),
        CASE
            WHEN d.DonationID IS NULL THEN NULL
            ELSE CONCAT(
                'DonorID=', d.DonorID,
                '; EventID=', d.EventID,
                '; Amount=', d.Amount,
                '; DonationDate=', CONVERT(VARCHAR(30), d.DonationDate, 126),
                '; PaymentMethod=', d.PaymentMethod,
                '; Status=', d.[Status],
                '; ReceiptNumber=', COALESCE(d.ReceiptNumber, 'NULL')
            )
        END,
        CONCAT(
            'DonorID=', i.DonorID,
            '; EventID=', i.EventID,
            '; Amount=', i.Amount,
            '; DonationDate=', CONVERT(VARCHAR(30), i.DonationDate, 126),
            '; PaymentMethod=', i.PaymentMethod,
            '; Status=', i.[Status],
            '; ReceiptNumber=', COALESCE(i.ReceiptNumber, 'NULL')
        ),
        SYSUTCDATETIME(),
        NULL
    FROM inserted i
    LEFT JOIN deleted d ON d.DonationID = i.DonationID;
END;
GO

DROP TRIGGER IF EXISTS dbo.trg_audit_expense;
GO
CREATE TRIGGER dbo.trg_audit_expense
ON dbo.Expense
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLog
    (
        UserID,
        [Action],
        TableName,
        RecordID,
        OldValue,
        NewValue,
        [Timestamp],
        IPAddress
    )
    SELECT
        TRY_CAST(SESSION_CONTEXT(N'UserID') AS INT),
        CASE WHEN d.ExpenseID IS NULL THEN 'INSERT' ELSE 'UPDATE' END,
        'Expense',
        CAST(i.ExpenseID AS VARCHAR(120)),
        CASE
            WHEN d.ExpenseID IS NULL THEN NULL
            ELSE CONCAT(
                'EventID=', d.EventID,
                '; ApprovedBy=', COALESCE(CAST(d.ApprovedBy AS VARCHAR(30)), 'NULL'),
                '; Category=', d.Category,
                '; Amount=', d.Amount,
                '; ExpenseDate=', CONVERT(VARCHAR(30), d.ExpenseDate, 126),
                '; PaymentStatus=', d.PaymentStatus
            )
        END,
        CONCAT(
            'EventID=', i.EventID,
            '; ApprovedBy=', COALESCE(CAST(i.ApprovedBy AS VARCHAR(30)), 'NULL'),
            '; Category=', i.Category,
            '; Amount=', i.Amount,
            '; ExpenseDate=', CONVERT(VARCHAR(30), i.ExpenseDate, 126),
            '; PaymentStatus=', i.PaymentStatus
        ),
        SYSUTCDATETIME(),
        NULL
    FROM inserted i
    LEFT JOIN deleted d ON d.ExpenseID = i.ExpenseID;
END;
GO

/* -------------------------------------------------------------------------- */
/* Trigger order hints for same-table AFTER INSERT validation/action pairs     */
/* -------------------------------------------------------------------------- */

EXEC sys.sp_settriggerorder
    @triggername = N'dbo.trg_prevent_negative_inventory',
    @order = 'First',
    @stmttype = 'INSERT';
GO
EXEC sys.sp_settriggerorder
    @triggername = N'dbo.trg_prevent_negative_inventory',
    @order = 'First',
    @stmttype = 'UPDATE';
GO
EXEC sys.sp_settriggerorder
    @triggername = N'dbo.trg_create_approval_on_allocation',
    @order = 'Last',
    @stmttype = 'INSERT';
GO
EXEC sys.sp_settriggerorder
    @triggername = N'dbo.trg_prevent_double_assignment',
    @order = 'First',
    @stmttype = 'INSERT';
GO
EXEC sys.sp_settriggerorder
    @triggername = N'dbo.trg_create_approval_on_assignment',
    @order = 'Last',
    @stmttype = 'INSERT';
GO
EXEC sys.sp_settriggerorder
    @triggername = N'dbo.trg_prevent_bed_overflow',
    @order = 'First',
    @stmttype = 'INSERT';
GO
EXEC sys.sp_settriggerorder
    @triggername = N'dbo.trg_decrement_beds',
    @order = 'Last',
    @stmttype = 'INSERT';
GO

PRINT 'Trigger script created successfully for Final_DB.';
GO
