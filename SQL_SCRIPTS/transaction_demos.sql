USE Final_DB;
GO

/* =====================================================
   TRANSACTION HANDLING DEMONSTRATION (ACID)
   - Demo A: Commit workflow (allocation + approval request)
   - Demo B: Forced rollback workflow (expense + approval request)
   - Demo C: Commit workflow (team assignment + approval request)
   ===================================================== */

SET NOCOUNT ON;

DECLARE @RequestedBy INT = (SELECT TOP 1 UserID FROM [User] WHERE IsActive = 1 ORDER BY UserID);
DECLARE @ReviewedBy INT = (SELECT TOP 1 UserID FROM [User] WHERE IsActive = 1 ORDER BY UserID DESC);
DECLARE @InventoryID INT = (SELECT TOP 1 InventoryID FROM Inventory WHERE Quantity >= 1 ORDER BY InventoryID);
DECLARE @EventID INT = (SELECT TOP 1 EventID FROM DisasterEvent ORDER BY EventID DESC);
DECLARE @TeamID INT = (SELECT TOP 1 TeamID FROM RescueTeam ORDER BY TeamID);
DECLARE @ReportID INT = (SELECT TOP 1 ReportID FROM EmergencyReport ORDER BY ReportID DESC);

IF @RequestedBy IS NULL OR @InventoryID IS NULL OR @EventID IS NULL OR @TeamID IS NULL OR @ReportID IS NULL
BEGIN
    THROW 51000, 'Missing prerequisite data. Ensure users, inventory, events, teams, and reports exist before running demos.', 1;
END

PRINT '========== DEMO A: COMMIT (ResourceAllocation + ApprovalRequest) ==========';
DECLARE @AllocationID_A INT = NULL;
DECLARE @RequestID_A INT = NULL;

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO ResourceAllocation (InventoryID, EventID, RequestedBy, Quantity, RequestTime, [Status], DispatchedAt, ConsumedAt)
    VALUES (@InventoryID, @EventID, @RequestedBy, 1.00, SYSUTCDATETIME(), 'Pending', NULL, NULL);

    SET @AllocationID_A = SCOPE_IDENTITY();

    INSERT INTO ApprovalRequest (RequestedBy, ReviewedBy, RequestType, RequestTime, [Status], [Description], AllocationID, AssignmentID, ExpenseID)
    VALUES (@RequestedBy, NULL, 'ResourceDistribution', SYSUTCDATETIME(), 'Pending', 'Txn demo A', @AllocationID_A, NULL, NULL);

    SET @RequestID_A = SCOPE_IDENTITY();

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    ra.AllocationID,
    ra.EventID,
    ra.InventoryID,
    ra.Status,
    ar.RequestID,
    ar.RequestType,
    ar.Status AS ApprovalStatus
FROM ResourceAllocation ra
LEFT JOIN ApprovalRequest ar ON ar.AllocationID = ra.AllocationID
WHERE ra.AllocationID = @AllocationID_A;

PRINT '========== DEMO B: FORCED ROLLBACK (Expense + ApprovalRequest) ==========';
DECLARE @ExpenseID_B INT = NULL;
DECLARE @RequestID_B INT = NULL;

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO Expense (EventID, ApprovedBy, Category, Amount, [Description], ExpenseDate, PaymentStatus)
    VALUES (@EventID, NULL, 'Operations', 123.45, 'Txn demo B', SYSUTCDATETIME(), 'PendingApproval');

    SET @ExpenseID_B = SCOPE_IDENTITY();

    INSERT INTO ApprovalRequest (RequestedBy, ReviewedBy, RequestType, RequestTime, [Status], [Description], AllocationID, AssignmentID, ExpenseID)
    VALUES (@RequestedBy, NULL, 'Financial', SYSUTCDATETIME(), 'Pending', 'Txn demo B', NULL, NULL, @ExpenseID_B);

    SET @RequestID_B = SCOPE_IDENTITY();

    THROW 51001, 'Simulated failure after dependent inserts to demonstrate rollback.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT CONCAT('Rollback executed. Error: ', ERROR_MESSAGE());
END CATCH;

SELECT TOP 1 *
FROM Expense
WHERE ExpenseID = @ExpenseID_B;

SELECT TOP 1 *
FROM ApprovalRequest
WHERE RequestID = @RequestID_B;

PRINT '========== DEMO C: COMMIT (TeamAssignment + ApprovalRequest) ==========';
DECLARE @AssignmentID_C INT = NULL;
DECLARE @RequestID_C INT = NULL;

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO TeamAssignment (TeamID, ReportID, AssignedBy, AssignmentTime, CompletionTime, [Status])
    VALUES (@TeamID, @ReportID, @RequestedBy, SYSUTCDATETIME(), NULL, 'Assigned');

    SET @AssignmentID_C = SCOPE_IDENTITY();

    INSERT INTO ApprovalRequest (RequestedBy, ReviewedBy, RequestType, RequestTime, [Status], [Description], AllocationID, AssignmentID, ExpenseID)
    VALUES (@RequestedBy, @ReviewedBy, 'RescueDeployment', SYSUTCDATETIME(), 'Pending', 'Txn demo C', NULL, @AssignmentID_C, NULL);

    SET @RequestID_C = SCOPE_IDENTITY();

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    ta.AssignmentID,
    ta.TeamID,
    ta.ReportID,
    ta.Status,
    ar.RequestID,
    ar.RequestType,
    ar.Status AS ApprovalStatus
FROM TeamAssignment ta
LEFT JOIN ApprovalRequest ar ON ar.AssignmentID = ta.AssignmentID
WHERE ta.AssignmentID = @AssignmentID_C;
GO
