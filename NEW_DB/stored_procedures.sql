USE Final_DB;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
    Stored procedures for Smart Disaster Response MIS.
    Assumes tables and triggers from DDL.sql and triggers.sql are already deployed.
*/

CREATE OR ALTER PROCEDURE dbo.sp_ApproveAllocation
    @AllocationID INT,
    @ReviewedBy INT,
    @Comments VARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @ReviewedBy;

        DECLARE @RequestID INT;

        SELECT TOP (1) @RequestID = ar.RequestID
        FROM dbo.ApprovalRequest ar
        WHERE ar.AllocationID = @AllocationID
          AND ar.RequestType = 'ResourceDistribution'
          AND ar.[Status] = 'Pending'
        ORDER BY ar.RequestID DESC;

        IF @RequestID IS NULL
        BEGIN
            RAISERROR('No pending resource distribution request found for the specified allocation.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.ApprovalRequest
        SET ReviewedBy = @ReviewedBy,
            [Status] = 'Approved'
        WHERE RequestID = @RequestID;

        IF @Comments IS NOT NULL
        BEGIN
            UPDATE ah
            SET ah.Comments = @Comments
            FROM dbo.ApprovalHistory ah
            WHERE ah.RequestID = @RequestID
              AND ah.HistoryID = (
                  SELECT MAX(HistoryID)
                  FROM dbo.ApprovalHistory
                  WHERE RequestID = @RequestID
              );
        END;

        COMMIT TRANSACTION;

        SELECT
            'Approved' AS ResultStatus,
            @AllocationID AS AllocationID,
            @RequestID AS RequestID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_RejectAllocation
    @AllocationID INT,
    @ReviewedBy INT,
    @Comments VARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @ReviewedBy;

        DECLARE @RequestID INT;

        SELECT TOP (1) @RequestID = ar.RequestID
        FROM dbo.ApprovalRequest ar
        WHERE ar.AllocationID = @AllocationID
          AND ar.RequestType = 'ResourceDistribution'
          AND ar.[Status] = 'Pending'
        ORDER BY ar.RequestID DESC;

        IF @RequestID IS NULL
        BEGIN
            RAISERROR('No pending resource distribution request found for the specified allocation.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.ApprovalRequest
        SET ReviewedBy = @ReviewedBy,
            [Status] = 'Rejected'
        WHERE RequestID = @RequestID;

        IF @Comments IS NOT NULL
        BEGIN
            UPDATE ah
            SET ah.Comments = @Comments
            FROM dbo.ApprovalHistory ah
            WHERE ah.RequestID = @RequestID
              AND ah.HistoryID = (
                  SELECT MAX(HistoryID)
                  FROM dbo.ApprovalHistory
                  WHERE RequestID = @RequestID
              );
        END;

        COMMIT TRANSACTION;

        SELECT
            'Rejected' AS ResultStatus,
            @AllocationID AS AllocationID,
            @RequestID AS RequestID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DispatchResources
    @AllocationID INT,
    @DispatchedByUserID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @DispatchedByUserID;

        DECLARE @CurrentStatus VARCHAR(20);

        SELECT @CurrentStatus = ra.[Status]
        FROM dbo.ResourceAllocation ra
        WHERE ra.AllocationID = @AllocationID;

        IF @CurrentStatus IS NULL
        BEGIN
            RAISERROR('Resource allocation not found.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @CurrentStatus <> 'Approved'
        BEGIN
            RAISERROR('Only approved allocations can be dispatched.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.ResourceAllocation
        SET [Status] = 'Dispatched',
            DispatchedAt = SYSUTCDATETIME()
        WHERE AllocationID = @AllocationID;

        COMMIT TRANSACTION;

        SELECT
            'Dispatched' AS ResultStatus,
            @AllocationID AS AllocationID,
            (SELECT DispatchedAt FROM dbo.ResourceAllocation WHERE AllocationID = @AllocationID) AS DispatchedAt;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AssignTeam
    @TeamID INT,
    @ReportID INT,
    @AssignedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @AssignedBy;

        IF NOT EXISTS (SELECT 1 FROM dbo.RescueTeam WHERE TeamID = @TeamID)
        BEGIN
            RAISERROR('Rescue team not found.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF NOT EXISTS (SELECT 1 FROM dbo.EmergencyReport WHERE ReportID = @ReportID)
        BEGIN
            RAISERROR('Emergency report not found.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF EXISTS (
            SELECT 1
            FROM dbo.RescueTeam
            WHERE TeamID = @TeamID
              AND AvailabilityStatus <> 'Available'
        )
        BEGIN
            RAISERROR('Selected team is not available.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        INSERT INTO dbo.TeamAssignment
        (
            TeamID,
            ReportID,
            AssignedBy,
            AssignmentTime,
            CompletionTime,
            [Status]
        )
        VALUES
        (
            @TeamID,
            @ReportID,
            @AssignedBy,
            SYSUTCDATETIME(),
            NULL,
            'Assigned'
        );

        DECLARE @AssignmentID INT = SCOPE_IDENTITY();

        COMMIT TRANSACTION;

        SELECT
            'Assigned' AS ResultStatus,
            @AssignmentID AS AssignmentID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ApproveDeployment
    @AssignmentID INT,
    @ReviewedBy INT,
    @Comments VARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @ReviewedBy;

        DECLARE @RequestID INT;

        SELECT TOP (1) @RequestID = ar.RequestID
        FROM dbo.ApprovalRequest ar
        WHERE ar.AssignmentID = @AssignmentID
          AND ar.RequestType = 'RescueDeployment'
          AND ar.[Status] = 'Pending'
        ORDER BY ar.RequestID DESC;

        IF @RequestID IS NULL
        BEGIN
            RAISERROR('No pending rescue deployment request found for the specified assignment.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.ApprovalRequest
        SET ReviewedBy = @ReviewedBy,
            [Status] = 'Approved'
        WHERE RequestID = @RequestID;

        IF @Comments IS NOT NULL
        BEGIN
            UPDATE ah
            SET ah.Comments = @Comments
            FROM dbo.ApprovalHistory ah
            WHERE ah.RequestID = @RequestID
              AND ah.HistoryID = (
                  SELECT MAX(HistoryID)
                  FROM dbo.ApprovalHistory
                  WHERE RequestID = @RequestID
              );
        END;

        COMMIT TRANSACTION;

        SELECT 'Approved' AS ResultStatus, @AssignmentID AS AssignmentID, @RequestID AS RequestID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_RejectDeployment
    @AssignmentID INT,
    @ReviewedBy INT,
    @Comments VARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @ReviewedBy;

        DECLARE @RequestID INT;

        SELECT TOP (1) @RequestID = ar.RequestID
        FROM dbo.ApprovalRequest ar
        WHERE ar.AssignmentID = @AssignmentID
          AND ar.RequestType = 'RescueDeployment'
          AND ar.[Status] = 'Pending'
        ORDER BY ar.RequestID DESC;

        IF @RequestID IS NULL
        BEGIN
            RAISERROR('No pending rescue deployment request found for the specified assignment.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.ApprovalRequest
        SET ReviewedBy = @ReviewedBy,
            [Status] = 'Rejected'
        WHERE RequestID = @RequestID;

        IF @Comments IS NOT NULL
        BEGIN
            UPDATE ah
            SET ah.Comments = @Comments
            FROM dbo.ApprovalHistory ah
            WHERE ah.RequestID = @RequestID
              AND ah.HistoryID = (
                  SELECT MAX(HistoryID)
                  FROM dbo.ApprovalHistory
                  WHERE RequestID = @RequestID
              );
        END;

        COMMIT TRANSACTION;

        SELECT 'Rejected' AS ResultStatus, @AssignmentID AS AssignmentID, @RequestID AS RequestID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CompleteAssignment
    @AssignmentID INT,
    @CompletedByUserID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @CompletedByUserID;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.TeamAssignment
            WHERE AssignmentID = @AssignmentID
              AND [Status] IN ('Assigned','EnRoute','OnSite')
        )
        BEGIN
            RAISERROR('Assignment not found or not in a completable state.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.TeamAssignment
        SET [Status] = 'Completed',
            CompletionTime = SYSUTCDATETIME()
        WHERE AssignmentID = @AssignmentID;

        COMMIT TRANSACTION;

        SELECT
            'Completed' AS ResultStatus,
            @AssignmentID AS AssignmentID,
            (SELECT CompletionTime FROM dbo.TeamAssignment WHERE AssignmentID = @AssignmentID) AS CompletionTime;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ApproveRequest
    @RequestID INT,
    @ReviewedBy INT,
    @Comments VARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @ReviewedBy;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.ApprovalRequest
            WHERE RequestID = @RequestID
              AND [Status] = 'Pending'
        )
        BEGIN
            RAISERROR('Approval request not found or not pending.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.ApprovalRequest
        SET ReviewedBy = @ReviewedBy,
            [Status] = 'Approved'
        WHERE RequestID = @RequestID;

        IF @Comments IS NOT NULL
        BEGIN
            UPDATE ah
            SET ah.Comments = @Comments
            FROM dbo.ApprovalHistory ah
            WHERE ah.RequestID = @RequestID
              AND ah.HistoryID = (
                  SELECT MAX(HistoryID)
                  FROM dbo.ApprovalHistory
                  WHERE RequestID = @RequestID
              );
        END;

        COMMIT TRANSACTION;

        SELECT 'Approved' AS ResultStatus, @RequestID AS RequestID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_RejectRequest
    @RequestID INT,
    @ReviewedBy INT,
    @Comments VARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @ReviewedBy;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.ApprovalRequest
            WHERE RequestID = @RequestID
              AND [Status] = 'Pending'
        )
        BEGIN
            RAISERROR('Approval request not found or not pending.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.ApprovalRequest
        SET ReviewedBy = @ReviewedBy,
            [Status] = 'Rejected'
        WHERE RequestID = @RequestID;

        IF @Comments IS NOT NULL
        BEGIN
            UPDATE ah
            SET ah.Comments = @Comments
            FROM dbo.ApprovalHistory ah
            WHERE ah.RequestID = @RequestID
              AND ah.HistoryID = (
                  SELECT MAX(HistoryID)
                  FROM dbo.ApprovalHistory
                  WHERE RequestID = @RequestID
              );
        END;

        COMMIT TRANSACTION;

        SELECT 'Rejected' AS ResultStatus, @RequestID AS RequestID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ApproveExpense
    @ExpenseID INT,
    @ApprovedBy INT,
    @Comments VARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @ApprovedBy;

        DECLARE @RequestID INT;

        SELECT TOP (1) @RequestID = ar.RequestID
        FROM dbo.ApprovalRequest ar
        WHERE ar.ExpenseID = @ExpenseID
          AND ar.RequestType = 'Financial'
          AND ar.[Status] = 'Pending'
        ORDER BY ar.RequestID DESC;

        IF @RequestID IS NULL
        BEGIN
            RAISERROR('No pending financial approval request found for the specified expense.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.ApprovalRequest
        SET ReviewedBy = @ApprovedBy,
            [Status] = 'Approved'
        WHERE RequestID = @RequestID;

        IF @Comments IS NOT NULL
        BEGIN
            UPDATE ah
            SET ah.Comments = @Comments
            FROM dbo.ApprovalHistory ah
            WHERE ah.RequestID = @RequestID
              AND ah.HistoryID = (
                  SELECT MAX(HistoryID)
                  FROM dbo.ApprovalHistory
                  WHERE RequestID = @RequestID
              );
        END;

        COMMIT TRANSACTION;

        SELECT 'Approved' AS ResultStatus, @ExpenseID AS ExpenseID, @RequestID AS RequestID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_RejectExpense
    @ExpenseID INT,
    @ApprovedBy INT,
    @Comments VARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @ApprovedBy;

        DECLARE @RequestID INT;

        SELECT TOP (1) @RequestID = ar.RequestID
        FROM dbo.ApprovalRequest ar
        WHERE ar.ExpenseID = @ExpenseID
          AND ar.RequestType = 'Financial'
          AND ar.[Status] = 'Pending'
        ORDER BY ar.RequestID DESC;

        IF @RequestID IS NULL
        BEGIN
            RAISERROR('No pending financial approval request found for the specified expense.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.ApprovalRequest
        SET ReviewedBy = @ApprovedBy,
            [Status] = 'Rejected'
        WHERE RequestID = @RequestID;

        IF @Comments IS NOT NULL
        BEGIN
            UPDATE ah
            SET ah.Comments = @Comments
            FROM dbo.ApprovalHistory ah
            WHERE ah.RequestID = @RequestID
              AND ah.HistoryID = (
                  SELECT MAX(HistoryID)
                  FROM dbo.ApprovalHistory
                  WHERE RequestID = @RequestID
              );
        END;

        COMMIT TRANSACTION;

        SELECT 'Rejected' AS ResultStatus, @ExpenseID AS ExpenseID, @RequestID AS RequestID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AdmitPatient
    @PatientID INT,
    @HospitalID INT,
    @Condition VARCHAR(20),
    @ReportID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Patient WHERE PatientID = @PatientID)
        BEGIN
            RAISERROR('Patient not found.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF NOT EXISTS (SELECT 1 FROM dbo.Hospital WHERE HospitalID = @HospitalID)
        BEGIN
            RAISERROR('Hospital not found.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @Condition NOT IN ('Critical','Serious','Stable')
        BEGIN
            RAISERROR('Invalid patient condition.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @ReportID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.EmergencyReport WHERE ReportID = @ReportID)
        BEGIN
            RAISERROR('Emergency report not found.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF (SELECT AvailableBeds FROM dbo.Hospital WHERE HospitalID = @HospitalID) <= 0
        BEGIN
            RAISERROR('Hospital has no available beds.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        INSERT INTO dbo.PatientAdmission
        (
            PatientID,
            HospitalID,
            ReportID,
            AdmissionTime,
            DischargeTime,
            [Condition],
            [Status]
        )
        VALUES
        (
            @PatientID,
            @HospitalID,
            @ReportID,
            SYSUTCDATETIME(),
            NULL,
            @Condition,
            'Admitted'
        );

        DECLARE @AdmissionID INT = SCOPE_IDENTITY();

        COMMIT TRANSACTION;

        SELECT
            'Admitted' AS ResultStatus,
            @AdmissionID AS AdmissionID,
            (SELECT AvailableBeds FROM dbo.Hospital WHERE HospitalID = @HospitalID) AS AvailableBeds;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DischargePatient
    @AdmissionID INT,
    @Status VARCHAR(20),
    @DischargedByUserID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @DischargedByUserID;

        IF @Status NOT IN ('Discharged','Transferred')
        BEGIN
            RAISERROR('Status must be Discharged or Transferred.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        DECLARE @HospitalID INT;

        SELECT @HospitalID = HospitalID
        FROM dbo.PatientAdmission
        WHERE AdmissionID = @AdmissionID
          AND [Status] = 'Admitted';

        IF @HospitalID IS NULL
        BEGIN
            RAISERROR('Admitted patient record not found.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.PatientAdmission
        SET [Status] = @Status,
            DischargeTime = SYSUTCDATETIME()
        WHERE AdmissionID = @AdmissionID;

        COMMIT TRANSACTION;

        SELECT
            'Completed' AS ResultStatus,
            @AdmissionID AS AdmissionID,
            (SELECT AvailableBeds FROM dbo.Hospital WHERE HospitalID = @HospitalID) AS AvailableBeds;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CheckInventoryLevel
    @InventoryID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            i.InventoryID,
            i.WarehouseID,
            i.ResourceID,
            i.Quantity,
            i.MinThreshold,
            i.MaxCapacity,
            CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM dbo.InventoryAlert ia
                    WHERE ia.InventoryID = i.InventoryID
                      AND ia.[Status] = 'Active'
                ) THEN 'Alert'
                WHEN i.Quantity < i.MinThreshold THEN 'LowStock'
                ELSE 'Normal'
            END AS AlertStatus
        FROM dbo.Inventory i
        WHERE i.InventoryID = @InventoryID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateInventoryStock
    @InventoryID INT,
    @NewQuantity DECIMAL(14,2),
    @UpdatedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC sys.sp_set_session_context @key = N'UserID', @value = @UpdatedBy;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.[User] u
            JOIN dbo.UserRole ur ON ur.UserID = u.UserID
            JOIN dbo.[Role] r ON r.RoleID = ur.RoleID
            WHERE u.UserID = @UpdatedBy
              AND r.RoleName IN ('Administrator', 'Warehouse Manager')
        )
        BEGIN
            RAISERROR('Only administrators or warehouse managers can update inventory stock manually.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @NewQuantity < 0
        BEGIN
            RAISERROR('Inventory quantity cannot be negative.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        DECLARE @MaxCapacity DECIMAL(14,2);

        SELECT @MaxCapacity = MaxCapacity
        FROM dbo.Inventory
        WHERE InventoryID = @InventoryID;

        IF @MaxCapacity IS NULL
        BEGIN
            RAISERROR('Inventory record not found.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @NewQuantity > @MaxCapacity
        BEGIN
            RAISERROR('New quantity cannot exceed max capacity.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.Inventory
        SET Quantity = @NewQuantity,
            LastUpdated = SYSUTCDATETIME()
        WHERE InventoryID = @InventoryID;

        SELECT
            'Updated' AS ResultStatus,
            @InventoryID AS InventoryID,
            @NewQuantity AS NewQuantity;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDashboardStats
    @EventID INT = NULL,
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            (
                SELECT COUNT(*)
                FROM dbo.EmergencyReport er
                WHERE (@EventID IS NULL OR er.EventID = @EventID)
                  AND (@StartDate IS NULL OR er.ReportTime >= @StartDate)
                  AND (@EndDate IS NULL OR er.ReportTime <= @EndDate)
            ) AS IncidentCount,
            (
                SELECT COUNT(*)
                FROM dbo.ResourceAllocation ra
                WHERE (@EventID IS NULL OR ra.EventID = @EventID)
                  AND (@StartDate IS NULL OR ra.RequestTime >= @StartDate)
                  AND (@EndDate IS NULL OR ra.RequestTime <= @EndDate)
            ) AS ResourceAllocationCount,
            (
                SELECT COUNT(*)
                FROM dbo.TeamAssignment ta
                INNER JOIN dbo.EmergencyReport er ON er.ReportID = ta.ReportID
                WHERE (@EventID IS NULL OR er.EventID = @EventID)
                  AND (@StartDate IS NULL OR ta.AssignmentTime >= @StartDate)
                  AND (@EndDate IS NULL OR ta.AssignmentTime <= @EndDate)
            ) AS TeamAssignmentCount,
            (
                SELECT CAST(AVG(CAST(h.OccupancyRate AS DECIMAL(10,2))) AS DECIMAL(10,2))
                FROM dbo.Hospital h
            ) AS AvgHospitalOccupancyRate,
            (
                SELECT CAST(ISNULL(SUM(d.Amount), 0) AS DECIMAL(14,2))
                FROM dbo.Donation d
                WHERE d.[Status] = 'Confirmed'
                  AND (@EventID IS NULL OR d.EventID = @EventID)
                  AND (@StartDate IS NULL OR d.DonationDate >= @StartDate)
                  AND (@EndDate IS NULL OR d.DonationDate <= @EndDate)
            ) AS TotalConfirmedDonations,
            (
                SELECT CAST(ISNULL(SUM(e.Amount), 0) AS DECIMAL(14,2))
                FROM dbo.Expense e
                WHERE (@EventID IS NULL OR e.EventID = @EventID)
                  AND (@StartDate IS NULL OR e.ExpenseDate >= @StartDate)
                  AND (@EndDate IS NULL OR e.ExpenseDate <= @EndDate)
            ) AS TotalExpenses,
            (
                SELECT CAST(CASE
                    WHEN SUM(i.MaxCapacity) IS NULL OR SUM(i.MaxCapacity) = 0 THEN 0
                    ELSE (SUM(i.Quantity) * 100.0 / SUM(i.MaxCapacity))
                END AS DECIMAL(10,2))
                FROM dbo.Inventory i
            ) AS InventoryUtilizationPercent;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

PRINT 'Stored procedure script created successfully for Final_DB.';
GO
