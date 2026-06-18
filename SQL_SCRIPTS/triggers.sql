/* =====================================================
   TRIGGERS
   ===================================================== */

USE Final_DB;
GO

DROP TRIGGER IF EXISTS trg_EmergencyReport_MaintainCounts;
GO
CREATE TRIGGER trg_EmergencyReport_MaintainCounts
ON EmergencyReport
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        ;WITH CitizenDelta AS (
            SELECT CitizenID, SUM(Delta) AS Delta
            FROM (
                SELECT CitizenID, CAST(1 AS INT) AS Delta FROM inserted
                UNION ALL
                SELECT CitizenID, CAST(-1 AS INT) AS Delta FROM deleted
            ) d
            GROUP BY CitizenID
        )
        UPDATE c
        SET c.TotalReports = c.TotalReports + cd.Delta
        FROM Citizen c
        JOIN CitizenDelta cd ON cd.CitizenID = c.CitizenID;

        IF EXISTS (SELECT 1 FROM Citizen WHERE TotalReports < 0)
        BEGIN
            RAISERROR('Citizen total report count cannot be negative.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        ;WITH EventDelta AS (
            SELECT EventID, SUM(Delta) AS Delta
            FROM (
                SELECT EventID, CAST(1 AS INT) AS Delta FROM inserted WHERE EventID IS NOT NULL
                UNION ALL
                SELECT EventID, CAST(-1 AS INT) AS Delta FROM deleted WHERE EventID IS NOT NULL
            ) d
            GROUP BY EventID
        )
        UPDATE e
        SET e.TotalReports = e.TotalReports + ed.Delta
        FROM DisasterEvent e
        JOIN EventDelta ed ON ed.EventID = e.EventID;

        IF EXISTS (SELECT 1 FROM DisasterEvent WHERE TotalReports < 0)
        BEGIN
            RAISERROR('Disaster event total report count cannot be negative.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

DROP TRIGGER IF EXISTS trg_TeamAssignment_SyncTeamState;
GO
CREATE TRIGGER trg_TeamAssignment_SyncTeamState
ON TeamAssignment
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        ;WITH TeamDelta AS (
            SELECT TeamID, SUM(Delta) AS Delta
            FROM (
                SELECT TeamID, CAST(1 AS INT) AS Delta FROM inserted
                UNION ALL
                SELECT TeamID, CAST(-1 AS INT) AS Delta FROM deleted
            ) x
            GROUP BY TeamID
        )
        UPDATE t
        SET t.TotalAssignments = t.TotalAssignments + td.Delta
        FROM RescueTeam t
        JOIN TeamDelta td ON td.TeamID = t.TeamID;

        IF EXISTS (SELECT 1 FROM RescueTeam WHERE TotalAssignments < 0)
        BEGIN
            RAISERROR('Rescue team total assignments cannot be negative.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        ;WITH AffectedTeams AS (
            SELECT TeamID FROM inserted
            UNION
            SELECT TeamID FROM deleted
        ),
        TeamState AS (
            SELECT
                at.TeamID,
                SUM(CASE WHEN ta.Status = 'OnSite' THEN 1 ELSE 0 END) AS OnSiteCount,
                SUM(CASE WHEN ta.Status IN ('Assigned', 'EnRoute') THEN 1 ELSE 0 END) AS AssignedCount,
                SUM(CASE WHEN ta.Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedCount,
                COUNT(ta.AssignmentID) AS TotalCount
            FROM AffectedTeams at
            LEFT JOIN TeamAssignment ta ON ta.TeamID = at.TeamID
            GROUP BY at.TeamID
        )
        UPDATE rt
        SET AvailabilityStatus = CASE
            WHEN ts.OnSiteCount > 0 THEN 'Busy'
            WHEN ts.AssignedCount > 0 THEN 'Assigned'
            WHEN ts.CompletedCount > 0 AND ts.TotalCount > 0 THEN 'Completed'
            ELSE 'Available'
        END
        FROM RescueTeam rt
        JOIN TeamState ts ON ts.TeamID = rt.TeamID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

DROP TRIGGER IF EXISTS trg_TeamActivity_SyncTeamState;
GO
CREATE TRIGGER trg_TeamActivity_SyncTeamState
ON TeamActivity
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        ;WITH AffectedTeams AS (
            SELECT TeamID FROM inserted
            UNION
            SELECT TeamID FROM deleted
        ),
        ActivityState AS (
            SELECT
                at.TeamID,
                SUM(CASE WHEN ta.EndTime IS NULL THEN 1 ELSE 0 END) AS OngoingCount,
                COUNT(ta.ActivityID) AS TotalCount
            FROM AffectedTeams at
            LEFT JOIN TeamActivity ta ON ta.TeamID = at.TeamID
            GROUP BY at.TeamID
        )
        UPDATE rt
        SET AvailabilityStatus = CASE
            WHEN ast.OngoingCount > 0 THEN 'Busy'
            WHEN ast.TotalCount > 0 THEN 'Completed'
            ELSE 'Available'
        END
        FROM RescueTeam rt
        JOIN ActivityState ast ON ast.TeamID = rt.TeamID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

DROP TRIGGER IF EXISTS trg_Inventory_ManageAlerts;
GO
CREATE TRIGGER trg_Inventory_ManageAlerts
ON Inventory
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM inserted WHERE Quantity < 0 OR MinThreshold < 0 OR MaxCapacity < 0 OR Quantity > MaxCapacity)
        BEGIN
            RAISERROR('Inventory values are invalid. Quantity must be between 0 and MaxCapacity.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        ;WITH LowStock AS (
            SELECT i.InventoryID
            FROM inserted i
            WHERE i.Quantity <= i.MinThreshold
        ),
        NeedNewAlert AS (
            SELECT ls.InventoryID
            FROM LowStock ls
            WHERE NOT EXISTS (
                SELECT 1
                FROM InventoryAlert ia
                WHERE ia.InventoryID = ls.InventoryID
                  AND ia.Status = 'Active'
            )
        )
        INSERT INTO InventoryAlert (InventoryID, AlertID, AlertType, AlertTime, Status, ResolvedAt)
        SELECT
            nna.InventoryID,
            ISNULL(mx.MaxAlertID, 0) + 1,
            'LowStock',
            SYSUTCDATETIME(),
            'Active',
            NULL
        FROM NeedNewAlert nna
        OUTER APPLY (
            SELECT MAX(AlertID) AS MaxAlertID
            FROM InventoryAlert ia
            WHERE ia.InventoryID = nna.InventoryID
        ) mx;

        UPDATE ia
        SET
            ia.Status = 'Resolved',
            ia.ResolvedAt = SYSUTCDATETIME()
        FROM InventoryAlert ia
        JOIN inserted i ON i.InventoryID = ia.InventoryID
        WHERE i.Quantity > i.MinThreshold
          AND ia.Status = 'Active';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

DROP TRIGGER IF EXISTS trg_ResourceAllocation_UpdateInventory;
GO
CREATE TRIGGER trg_ResourceAllocation_UpdateInventory
ON ResourceAllocation
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        ;WITH ChangeSet AS (
            SELECT
                COALESCE(i.AllocationID, d.AllocationID) AS AllocationID,
                COALESCE(i.InventoryID, d.InventoryID) AS InventoryID,
                ISNULL(i.Quantity, 0) AS NewQty,
                ISNULL(d.Quantity, 0) AS OldQty,
                CASE WHEN i.Status IN ('Dispatched', 'Consumed') THEN 1 ELSE 0 END AS NewEffective,
                CASE WHEN d.Status IN ('Dispatched', 'Consumed') THEN 1 ELSE 0 END AS OldEffective
            FROM inserted i
            FULL OUTER JOIN deleted d
                ON i.AllocationID = d.AllocationID
        ),
        DeltaByInventory AS (
            SELECT
                InventoryID,
                SUM(
                    CASE
                        WHEN OldEffective = 0 AND NewEffective = 1 THEN -NewQty
                        WHEN OldEffective = 1 AND NewEffective = 0 THEN OldQty
                        WHEN OldEffective = 1 AND NewEffective = 1 THEN OldQty - NewQty
                        ELSE 0
                    END
                ) AS QtyDelta
            FROM ChangeSet
            GROUP BY InventoryID
        )
        SELECT InventoryID, QtyDelta
        INTO #AllocationDelta
        FROM DeltaByInventory;

        IF EXISTS (
            SELECT 1
            FROM Inventory inv
            JOIN #AllocationDelta d ON d.InventoryID = inv.InventoryID
            WHERE inv.Quantity + d.QtyDelta < 0
        )
        BEGIN
            DROP TABLE #AllocationDelta;
            RAISERROR('Resource allocation would result in negative inventory.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE inv
        SET inv.Quantity = inv.Quantity + d.QtyDelta
        FROM Inventory inv
        JOIN #AllocationDelta d ON d.InventoryID = inv.InventoryID;

        DROP TABLE #AllocationDelta;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID('tempdb..#AllocationDelta') IS NOT NULL DROP TABLE #AllocationDelta;
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

DROP TRIGGER IF EXISTS trg_PatientAdmission_UpdateBeds;
GO
CREATE TRIGGER trg_PatientAdmission_UpdateBeds
ON PatientAdmission
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        ;WITH ChangeSet AS (
            SELECT
                COALESCE(i.AdmissionID, d.AdmissionID) AS AdmissionID,
                ISNULL(i.HospitalID, d.HospitalID) AS HospitalID,
                CASE WHEN i.Status = 'Admitted' THEN 1 ELSE 0 END AS NewOccupied,
                CASE WHEN d.Status = 'Admitted' THEN 1 ELSE 0 END AS OldOccupied
            FROM inserted i
            FULL OUTER JOIN deleted d
                ON i.AdmissionID = d.AdmissionID
        ),
        DeltaByHospital AS (
            SELECT
                HospitalID,
                SUM(
                    CASE
                        WHEN OldOccupied = 0 AND NewOccupied = 1 THEN -1
                        WHEN OldOccupied = 1 AND NewOccupied = 0 THEN 1
                        ELSE 0
                    END
                ) AS BedDelta
            FROM ChangeSet
            GROUP BY HospitalID
        )
        SELECT HospitalID, BedDelta
        INTO #BedDelta
        FROM DeltaByHospital;

        IF EXISTS (
            SELECT 1
            FROM Hospital h
            JOIN #BedDelta d ON d.HospitalID = h.HospitalID
            WHERE (h.AvailableBeds + d.BedDelta) < 0
               OR (h.AvailableBeds + d.BedDelta) > h.TotalBeds
        )
        BEGIN
            DROP TABLE #BedDelta;
            RAISERROR('Hospital available bed count would become invalid.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE h
        SET h.AvailableBeds = h.AvailableBeds + d.BedDelta
        FROM Hospital h
        JOIN #BedDelta d ON d.HospitalID = h.HospitalID;

        DROP TABLE #BedDelta;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID('tempdb..#BedDelta') IS NOT NULL DROP TABLE #BedDelta;
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

DROP TRIGGER IF EXISTS trg_ApprovalRequest_WriteHistory;
GO
CREATE TRIGGER trg_ApprovalRequest_WriteHistory
ON ApprovalRequest
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        ;WITH ChangedStatus AS (
            SELECT
                i.RequestID,
                i.Status,
                COALESCE(i.ReviewedBy, i.RequestedBy) AS ActionBy,
                ROW_NUMBER() OVER (PARTITION BY i.RequestID ORDER BY i.RequestTime, i.RequestID) AS RN
            FROM inserted i
            JOIN deleted d ON d.RequestID = i.RequestID
            WHERE i.Status IN ('Approved', 'Rejected')
              AND i.Status <> d.Status
        )
        INSERT INTO ApprovalHistory (RequestID, HistoryID, ActionBy, ActionTime, Decision, Comments)
        SELECT
            cs.RequestID,
            ISNULL(mx.MaxHistoryID, 0) + cs.RN,
            cs.ActionBy,
            SYSUTCDATETIME(),
            CASE WHEN cs.Status = 'Approved' THEN 'Approved' ELSE 'Rejected' END,
            'Auto-generated by ApprovalRequest status trigger.'
        FROM ChangedStatus cs
        OUTER APPLY (
            SELECT MAX(ah.HistoryID) AS MaxHistoryID
            FROM ApprovalHistory ah
            WHERE ah.RequestID = cs.RequestID
        ) mx;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

DROP TRIGGER IF EXISTS trg_Donation_Audit;
GO
CREATE TRIGGER trg_Donation_Audit
ON Donation
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO AuditLog (UserID, Action, TableName, RecordID, OldValue, NewValue, [Timestamp], IPAddress)
        SELECT
            NULL,
            'DONATION_INSERT',
            'Donation',
            CAST(i.DonationID AS VARCHAR(120)),
            NULL,
            CONCAT('Amount=', i.Amount, ';Status=', i.Status, ';EventID=', i.EventID),
            SYSUTCDATETIME(),
            NULL
        FROM inserted i
        LEFT JOIN deleted d ON d.DonationID = i.DonationID
        WHERE d.DonationID IS NULL;

        INSERT INTO AuditLog (UserID, Action, TableName, RecordID, OldValue, NewValue, [Timestamp], IPAddress)
        SELECT
            NULL,
            'DONATION_UPDATE',
            'Donation',
            CAST(i.DonationID AS VARCHAR(120)),
            CONCAT('Amount=', d.Amount, ';Status=', d.Status, ';EventID=', d.EventID),
            CONCAT('Amount=', i.Amount, ';Status=', i.Status, ';EventID=', i.EventID),
            SYSUTCDATETIME(),
            NULL
        FROM inserted i
        JOIN deleted d ON d.DonationID = i.DonationID;

        INSERT INTO AuditLog (UserID, Action, TableName, RecordID, OldValue, NewValue, [Timestamp], IPAddress)
        SELECT
            NULL,
            'DONATION_DELETE',
            'Donation',
            CAST(d.DonationID AS VARCHAR(120)),
            CONCAT('Amount=', d.Amount, ';Status=', d.Status, ';EventID=', d.EventID),
            NULL,
            SYSUTCDATETIME(),
            NULL
        FROM deleted d
        LEFT JOIN inserted i ON i.DonationID = d.DonationID
        WHERE i.DonationID IS NULL;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

DROP TRIGGER IF EXISTS trg_Expense_Audit;
GO
CREATE TRIGGER trg_Expense_Audit
ON Expense
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO AuditLog (UserID, Action, TableName, RecordID, OldValue, NewValue, [Timestamp], IPAddress)
        SELECT
            NULL,
            'EXPENSE_INSERT',
            'Expense',
            CAST(i.ExpenseID AS VARCHAR(120)),
            NULL,
            CONCAT('Amount=', i.Amount, ';Category=', i.Category, ';EventID=', i.EventID),
            SYSUTCDATETIME(),
            NULL
        FROM inserted i
        LEFT JOIN deleted d ON d.ExpenseID = i.ExpenseID
        WHERE d.ExpenseID IS NULL;

        INSERT INTO AuditLog (UserID, Action, TableName, RecordID, OldValue, NewValue, [Timestamp], IPAddress)
        SELECT
            NULL,
            'EXPENSE_UPDATE',
            'Expense',
            CAST(i.ExpenseID AS VARCHAR(120)),
            CONCAT('Amount=', d.Amount, ';Category=', d.Category, ';EventID=', d.EventID),
            CONCAT('Amount=', i.Amount, ';Category=', i.Category, ';EventID=', i.EventID),
            SYSUTCDATETIME(),
            NULL
        FROM inserted i
        JOIN deleted d ON d.ExpenseID = i.ExpenseID;

        INSERT INTO AuditLog (UserID, Action, TableName, RecordID, OldValue, NewValue, [Timestamp], IPAddress)
        SELECT
            NULL,
            'EXPENSE_DELETE',
            'Expense',
            CAST(d.ExpenseID AS VARCHAR(120)),
            CONCAT('Amount=', d.Amount, ';Category=', d.Category, ';EventID=', d.EventID),
            NULL,
            SYSUTCDATETIME(),
            NULL
        FROM deleted d
        LEFT JOIN inserted i ON i.ExpenseID = d.ExpenseID
        WHERE i.ExpenseID IS NULL;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

DROP TRIGGER IF EXISTS trg_AuditLog_LoginUpdate;
GO
CREATE TRIGGER trg_AuditLog_LoginUpdate
ON AuditLog
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        ;WITH LoginRows AS (
            SELECT UserID, MAX([Timestamp]) AS LastLoginAt
            FROM inserted
            WHERE [Action] = 'LOGIN'
              AND UserID IS NOT NULL
            GROUP BY UserID
        )
        UPDATE u
        SET u.LastLoginAt = lr.LastLoginAt
        FROM [User] u
        JOIN LoginRows lr ON lr.UserID = u.UserID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO
