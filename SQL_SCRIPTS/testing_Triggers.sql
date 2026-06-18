/* =====================================================
   TEST SCRIPT (INSERT/UPDATE scenarios)
   Run after DDL + triggers are created.
   ===================================================== */

USE Final_DB;
GO

DECLARE @AdminUserID INT,
        @OpsUserID INT,
        @CitizenID INT,
        @EventID INT,
        @TeamID INT,
        @ResourceID INT,
        @WarehouseID INT,
        @InventoryID INT,
        @HospitalID INT,
        @PatientID INT,
        @DonorID INT,
        @ReportID INT,
        @AssignmentID INT,
        @ActivityID INT,
        @AllocationID INT,
        @AdmissionID INT,
        @ExpenseID INT,
        @RequestID INT,
        @DonationID INT,
        @ReceiptNo VARCHAR(100),
        @UniqueStamp VARCHAR(32);

SET @UniqueStamp = REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '');

-- Base seed data (idempotent)
IF NOT EXISTS (SELECT 1 FROM [User] WHERE Username = 'admin1')
    INSERT INTO [User] (FirstName, LastName, Username, PasswordHash, Email, IsActive)
    VALUES ('Admin', 'One', 'admin1', 'hash_admin1', 'admin1@test.com', 1);

IF NOT EXISTS (SELECT 1 FROM [User] WHERE Username = 'ops2')
    INSERT INTO [User] (FirstName, LastName, Username, PasswordHash, Email, IsActive)
    VALUES ('Ops', 'Two', 'ops2', 'hash_ops2', 'ops2@test.com', 1);

SELECT @AdminUserID = UserID FROM [User] WHERE Username = 'admin1';
SELECT @OpsUserID = UserID FROM [User] WHERE Username = 'ops2';

IF NOT EXISTS (SELECT 1 FROM Citizen WHERE NationalID = 'CIT001')
    INSERT INTO Citizen (FirstName, LastName, NationalID, Email, Street, Area, City, Province)
    VALUES ('Ali', 'Khan', 'CIT001', 'citizen1@test.com', 'St 1', 'Area A', 'City A', 'Province A');

SELECT @CitizenID = CitizenID FROM Citizen WHERE NationalID = 'CIT001';

IF NOT EXISTS (SELECT 1 FROM DisasterEvent WHERE EventName = 'Flood Event')
    INSERT INTO DisasterEvent (EventName, DisasterType, StartTime, Street, Area, City, Province, [Status])
    VALUES ('Flood Event', 'Flood', SYSUTCDATETIME(), 'St 2', 'Area B', 'City B', 'Province B', 'Active');

SELECT TOP 1 @EventID = EventID FROM DisasterEvent WHERE EventName = 'Flood Event' ORDER BY EventID DESC;

IF NOT EXISTS (SELECT 1 FROM RescueTeam WHERE TeamName = 'Team Alpha')
    INSERT INTO RescueTeam (TeamName, TeamType, Street, Area, City, Province, AvailabilityStatus, Capacity)
    VALUES ('Team Alpha', 'Rescue', 'St 3', 'Area C', 'City C', 'Province C', 'Available', 10);

SELECT @TeamID = TeamID FROM RescueTeam WHERE TeamName = 'Team Alpha';

IF NOT EXISTS (SELECT 1 FROM Resource WHERE ResourceName = 'Water Pack' AND Unit = 'units')
    INSERT INTO Resource (ResourceName, ResourceType, Unit) VALUES ('Water Pack', 'Water', 'units');

SELECT @ResourceID = ResourceID FROM Resource WHERE ResourceName = 'Water Pack' AND Unit = 'units';

IF NOT EXISTS (SELECT 1 FROM Warehouse WHERE WarehouseName = 'Central WH')
    INSERT INTO Warehouse (WarehouseName, Street, Area, City, Province, Capacity, ManagerID, ContactPhone, ContactEmail)
    VALUES ('Central WH', 'St 4', 'Area D', 'City D', 'Province D', 1000, @AdminUserID, '12345', 'wh@test.com');

SELECT @WarehouseID = WarehouseID FROM Warehouse WHERE WarehouseName = 'Central WH';

IF NOT EXISTS (SELECT 1 FROM Inventory WHERE WarehouseID = @WarehouseID AND ResourceID = @ResourceID)
    INSERT INTO Inventory (WarehouseID, ResourceID, Quantity, MinThreshold, MaxCapacity)
    VALUES (@WarehouseID, @ResourceID, 100.00, 20.00, 200.00);

SELECT @InventoryID = InventoryID FROM Inventory WHERE WarehouseID = @WarehouseID AND ResourceID = @ResourceID;

IF NOT EXISTS (SELECT 1 FROM Hospital WHERE HospitalName = 'General Hospital')
    INSERT INTO Hospital (HospitalName, Street, Area, City, Province, TotalBeds, AvailableBeds)
    VALUES ('General Hospital', 'St 5', 'Area E', 'City E', 'Province E', 100, 100);

SELECT @HospitalID = HospitalID FROM Hospital WHERE HospitalName = 'General Hospital';

IF NOT EXISTS (SELECT 1 FROM Patient WHERE NationalID = 'PAT001')
    INSERT INTO Patient (FirstName, LastName, Age, NationalID, BloodType)
    VALUES ('Sara', 'Noor', 30, 'PAT001', 'O+');

SELECT @PatientID = PatientID FROM Patient WHERE NationalID = 'PAT001';

IF NOT EXISTS (SELECT 1 FROM Donor WHERE Email = 'donor1@test.com')
    INSERT INTO Donor (FirstName, LastName, DonorType, Email, Street, Area, City, Province)
    VALUES ('Donor', 'One', 'Individual', 'donor1@test.com', 'St 6', 'Area F', 'City F', 'Province F');

SELECT @DonorID = DonorID FROM Donor WHERE Email = 'donor1@test.com';

-- 1) EmergencyReport trigger: counts
INSERT INTO EmergencyReport (CitizenID, EventID, Street, Area, City, Province, DisasterType, SeverityLevel, ReportTime, [Status], [Source], [Description])
VALUES (@CitizenID, @EventID, 'Incident St', 'Area A', 'City A', 'Province A', 'Flood', 'High', SYSUTCDATETIME(), 'Pending', 'Mobile', CONCAT('Initial report ', @UniqueStamp));

SET @ReportID = SCOPE_IDENTITY();

UPDATE EmergencyReport
SET EventID = @EventID, [Status] = 'InProgress'
WHERE ReportID = @ReportID;

SELECT CitizenID, TotalReports FROM Citizen WHERE CitizenID = @CitizenID;
SELECT EventID, TotalReports FROM DisasterEvent WHERE EventID = @EventID;

-- 2) TeamAssignment trigger: team availability + total assignments
INSERT INTO TeamAssignment (TeamID, ReportID, AssignedBy, AssignmentTime, [Status])
VALUES (@TeamID, @ReportID, @AdminUserID, SYSUTCDATETIME(), 'Assigned');

SET @AssignmentID = SCOPE_IDENTITY();

UPDATE TeamAssignment
SET [Status] = 'OnSite'
WHERE AssignmentID = @AssignmentID;

SELECT TeamID, AvailabilityStatus, TotalAssignments FROM RescueTeam WHERE TeamID = @TeamID;

-- 3) TeamActivity trigger: team status from activities
SELECT @ActivityID = ISNULL(MAX(ActivityID), 0) + 1 FROM TeamActivity WHERE TeamID = @TeamID;

INSERT INTO TeamActivity (TeamID, ActivityID, ActivityType, StartTime, EndTime)
VALUES (@TeamID, @ActivityID, 'FieldOperation', SYSUTCDATETIME(), NULL);

UPDATE TeamActivity
SET EndTime = SYSUTCDATETIME()
WHERE TeamID = @TeamID AND ActivityID = @ActivityID;

SELECT TeamID, AvailabilityStatus FROM RescueTeam WHERE TeamID = @TeamID;

-- 4) Inventory trigger: low-stock alert + resolve
UPDATE Inventory
SET Quantity = 10.00
WHERE InventoryID = @InventoryID;

SELECT * FROM InventoryAlert WHERE InventoryID = @InventoryID;

UPDATE Inventory
SET Quantity = 50.00
WHERE InventoryID = @InventoryID;

SELECT * FROM InventoryAlert WHERE InventoryID = @InventoryID ORDER BY AlertID;

-- 5) ResourceAllocation trigger: inventory deduction/restore and negative prevention
INSERT INTO ResourceAllocation (InventoryID, EventID, RequestedBy, Quantity, RequestTime, [Status])
VALUES (@InventoryID, @EventID, @AdminUserID, 30.00, SYSUTCDATETIME(), 'Pending');

SET @AllocationID = SCOPE_IDENTITY();

UPDATE ResourceAllocation
SET [Status] = 'Dispatched', DispatchedAt = SYSUTCDATETIME()
WHERE AllocationID = @AllocationID;

SELECT InventoryID, Quantity FROM Inventory WHERE InventoryID = @InventoryID;

-- 6) PatientAdmission trigger: adjust available beds
INSERT INTO PatientAdmission (PatientID, HospitalID, ReportID, AdmissionTime, [Condition], [Status])
VALUES (@PatientID, @HospitalID, @ReportID, SYSUTCDATETIME(), 'Serious', 'Admitted');

SET @AdmissionID = SCOPE_IDENTITY();

UPDATE PatientAdmission
SET [Status] = 'Discharged', DischargeTime = SYSUTCDATETIME()
WHERE AdmissionID = @AdmissionID;

SELECT HospitalID, TotalBeds, AvailableBeds FROM Hospital WHERE HospitalID = @HospitalID;

-- 7) ApprovalRequest trigger: auto history on approve/reject
INSERT INTO Expense (EventID, ApprovedBy, Category, Amount, [Description], ExpenseDate, PaymentStatus)
VALUES (@EventID, NULL, 'Operations', 1000.00, 'Fuel cost', SYSUTCDATETIME(), 'Pending');

SET @ExpenseID = SCOPE_IDENTITY();

INSERT INTO ApprovalRequest (RequestedBy, ReviewedBy, RequestType, RequestTime, [Status], [Description], ExpenseID)
VALUES (@AdminUserID, @OpsUserID, 'Financial', SYSUTCDATETIME(), 'Pending', 'Approve expense', @ExpenseID);

SET @RequestID = SCOPE_IDENTITY();

UPDATE ApprovalRequest
SET [Status] = 'Approved', ReviewedBy = @OpsUserID
WHERE RequestID = @RequestID;

SELECT * FROM ApprovalHistory WHERE RequestID = @RequestID;

-- 8) Donation audit trigger
SET @ReceiptNo = CONCAT('RCP-', RIGHT(@UniqueStamp, 8));

INSERT INTO Donation (DonorID, EventID, Amount, DonationDate, PaymentMethod, [Status], ReceiptNumber)
VALUES (@DonorID, @EventID, 500.00, SYSUTCDATETIME(), 'Online', 'Confirmed', @ReceiptNo);

SET @DonationID = SCOPE_IDENTITY();

UPDATE Donation SET Amount = 550.00 WHERE DonationID = @DonationID;

SELECT TOP 5 * FROM AuditLog WHERE TableName = 'Donation' ORDER BY LogID DESC;

-- 9) Expense audit trigger
UPDATE Expense SET Amount = 1200.00 WHERE ExpenseID = @ExpenseID;

SELECT TOP 5 * FROM AuditLog WHERE TableName = 'Expense' ORDER BY LogID DESC;

-- 10) AuditLog login trigger
INSERT INTO AuditLog (UserID, Action, TableName, RecordID, OldValue, NewValue, IPAddress)
VALUES (@AdminUserID, 'LOGIN', 'User', CAST(@AdminUserID AS VARCHAR(120)), NULL, 'Login success', '127.0.0.1');

SELECT UserID, LastLoginAt FROM [User] WHERE UserID = @AdminUserID;
