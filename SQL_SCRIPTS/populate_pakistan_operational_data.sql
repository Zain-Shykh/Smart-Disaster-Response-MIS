USE Final_DB;
GO

/* =====================================================
   PAKISTAN OPERATIONAL TEST DATA (ASSIGNMENTS & ALLOCATIONS)
   ===================================================== */

SET NOCOUNT ON;

-- Get IDs
DECLARE @KHI_FloodID INT = (SELECT TOP 1 EventID FROM DisasterEvent WHERE EventName = 'Karachi Urban Flooding 2024');
DECLARE @Quetta_EQID INT = (SELECT TOP 1 EventID FROM DisasterEvent WHERE EventName = 'Quetta Earthquake 2024');

DECLARE @EdhiTeamID INT = (SELECT TOP 1 TeamID FROM RescueTeam WHERE TeamName = 'Edhi Karachi Alpha');
DECLARE @Rescue1122ID INT = (SELECT TOP 1 TeamID FROM RescueTeam WHERE TeamName = 'Rescue 1122 Lahore Central');

DECLARE @AdminUserID INT = (SELECT TOP 1 UserID FROM [User] WHERE Username = 'admin1');
DECLARE @OpsUserID INT = (SELECT TOP 1 UserID FROM [User] WHERE Username = 'ops2');
DECLARE @WarehouseMgrID INT = (SELECT TOP 1 UserID FROM [User] WHERE Username = 'warehouse1');

-- 1) Create an Emergency Report for Karachi Flood if it doesn't exist
DECLARE @CitizenID INT = (SELECT TOP 1 CitizenID FROM Citizen WHERE NationalID = '42101-1234567-1');
IF @KHI_FloodID IS NOT NULL AND @CitizenID IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM EmergencyReport WHERE CitizenID = @CitizenID AND EventID = @KHI_FloodID)
        INSERT INTO EmergencyReport (CitizenID, EventID, Street, Area, City, Province, DisasterType, SeverityLevel, ReportTime, [Status], [Source], [Description])
        VALUES (@CitizenID, @KHI_FloodID, 'Main Road', 'Saddar', 'Karachi', 'Sindh', 'Flood', 'Critical', SYSUTCDATETIME(), 'Pending', 'Mobile', 'Water entering houses, elderly people stranded.');
END

DECLARE @KHI_ReportID INT = (SELECT TOP 1 ReportID FROM EmergencyReport WHERE EventID = @KHI_FloodID AND [Status] = 'Pending');

-- 2) Create a Team Assignment for Edhi Team to Karachi Flood
IF @EdhiTeamID IS NOT NULL AND @KHI_FloodID IS NOT NULL AND @OpsUserID IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM TeamAssignment WHERE TeamID = @EdhiTeamID AND EventID = @KHI_FloodID)
    BEGIN
        INSERT INTO TeamAssignment (TeamID, EventID, AssignedBy, AssignmentTime, [Status])
        VALUES (@EdhiTeamID, @KHI_FloodID, @OpsUserID, SYSUTCDATETIME(), 'EnRoute');
        
        -- Update team status
        UPDATE RescueTeam SET AvailabilityStatus = 'Busy' WHERE TeamID = @EdhiTeamID;
    END
END

-- 3) Create a Resource Allocation for Karachi Flood (Tents from Karachi Warehouse)
DECLARE @KHI_WarehouseID INT = (SELECT TOP 1 WarehouseID FROM Warehouse WHERE WarehouseName = 'Karachi Port Trust Relief Hub');
DECLARE @TentID INT = (SELECT TOP 1 ResourceID FROM Resource WHERE ResourceName = 'Emergency Tents');
DECLARE @KHI_InventoryID INT = (SELECT TOP 1 InventoryID FROM Inventory WHERE WarehouseID = @KHI_WarehouseID AND ResourceID = @TentID);

IF @KHI_InventoryID IS NOT NULL AND @KHI_FloodID IS NOT NULL AND @AdminUserID IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM ResourceAllocation WHERE InventoryID = @KHI_InventoryID AND EventID = @KHI_FloodID AND Quantity = 10)
        INSERT INTO ResourceAllocation (InventoryID, EventID, RequestedBy, Quantity, RequestTime, [Status])
        VALUES (@KHI_InventoryID, @KHI_FloodID, @AdminUserID, 10, SYSUTCDATETIME(), 'Approved');
END

-- 4) Create a Patient for the hospital
IF NOT EXISTS (SELECT 1 FROM Patient WHERE NationalID = '42101-1111111-1')
    INSERT INTO Patient (FirstName, LastName, Age, Gender, NationalID, BloodType, ContactPhone)
    VALUES ('Zubair', 'Ahmed', 45, 'Male', '42101-1111111-1', 'B+', '0300-1234567');

DECLARE @PatientID INT = (SELECT TOP 1 PatientID FROM Patient WHERE NationalID = '42101-1111111-1');
DECLARE @JPMC_ID INT = (SELECT TOP 1 HospitalID FROM Hospital WHERE HospitalName = 'JPMC (Jinnah Hospital) Karachi');

IF @PatientID IS NOT NULL AND @JPMC_ID IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM PatientAdmission WHERE PatientID = @PatientID AND HospitalID = @JPMC_ID)
        INSERT INTO PatientAdmission (PatientID, HospitalID, AdmissionTime, [Condition], [Status])
        VALUES (@PatientID, @JPMC_ID, SYSUTCDATETIME(), 'Stable', 'Admitted');
    
    -- Update available beds
    UPDATE Hospital SET AvailableBeds = AvailableBeds - 1 WHERE HospitalID = @JPMC_ID;
END

GO
PRINT 'Pakistan operational test data population complete.';
GO
