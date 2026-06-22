USE Final_DB;
GO

/* =====================================================
   PAKISTAN REAL-TIME TEST DATA POPULATION
   Purpose: Seed the database with realistic Pakistani scenarios
   ===================================================== */

SET NOCOUNT ON;

-- 1) DISASTER EVENTS
IF NOT EXISTS (SELECT 1 FROM DisasterEvent WHERE EventName = 'Karachi Urban Flooding 2024')
    INSERT INTO DisasterEvent (EventName, DisasterType, StartTime, EndTime, Street, Area, City, Province, [Status], AffectedPopulation, TotalReports)
    VALUES ('Karachi Urban Flooding 2024', 'Flood', '2024-05-01 08:00:00', NULL, 'M.A Jinnah Road', 'Saddar', 'Karachi', 'Sindh', 'Active', 50000, 150);

IF NOT EXISTS (SELECT 1 FROM DisasterEvent WHERE EventName = 'Swat River Flash Flood')
    INSERT INTO DisasterEvent (EventName, DisasterType, StartTime, EndTime, Street, Area, City, Province, [Status], AffectedPopulation, TotalReports)
    VALUES ('Swat River Flash Flood', 'Flood', '2024-05-02 14:30:00', NULL, 'Main River Road', 'Kalam', 'Swat', 'KPK', 'Active', 12000, 45);

IF NOT EXISTS (SELECT 1 FROM DisasterEvent WHERE EventName = 'Lahore Heatwave Emergency')
    INSERT INTO DisasterEvent (EventName, DisasterType, StartTime, EndTime, Street, Area, City, Province, [Status], AffectedPopulation, TotalReports)
    VALUES ('Lahore Heatwave Emergency', 'Heatwave', '2024-04-20 10:00:00', '2024-04-28 18:00:00', 'Mall Road', 'Anarkali', 'Lahore', 'Punjab', 'Resolved', 100000, 300);

IF NOT EXISTS (SELECT 1 FROM DisasterEvent WHERE EventName = 'Quetta Earthquake 2024')
    INSERT INTO DisasterEvent (EventName, DisasterType, StartTime, EndTime, Street, Area, City, Province, [Status], AffectedPopulation, TotalReports)
    VALUES ('Quetta Earthquake 2024', 'Earthquake', '2024-05-03 23:15:00', NULL, 'Zarghoon Road', 'Cantonment', 'Quetta', 'Balochistan', 'Active', 25000, 80);

-- 2) RESCUE TEAMS
IF NOT EXISTS (SELECT 1 FROM RescueTeam WHERE TeamName = 'Edhi Karachi Alpha')
    INSERT INTO RescueTeam (TeamName, TeamType, Street, Area, City, Province, AvailabilityStatus, Capacity)
    VALUES ('Edhi Karachi Alpha', 'Medical', 'Tower Road', 'Kharadar', 'Karachi', 'Sindh', 'Available', 20);

IF NOT EXISTS (SELECT 1 FROM RescueTeam WHERE TeamName = 'Rescue 1122 Lahore Central')
    INSERT INTO RescueTeam (TeamName, TeamType, Street, Area, City, Province, AvailabilityStatus, Capacity)
    VALUES ('Rescue 1122 Lahore Central', 'Rescue', 'Ferozepur Road', 'Gulberg', 'Lahore', 'Punjab', 'Available', 50);

IF NOT EXISTS (SELECT 1 FROM RescueTeam WHERE TeamName = 'Chhipa Peshawar Response')
    INSERT INTO RescueTeam (TeamName, TeamType, Street, Area, City, Province, AvailabilityStatus, Capacity)
    VALUES ('Chhipa Peshawar Response', 'Medical', 'University Road', 'Hayatabad', 'Peshawar', 'KPK', 'Available', 15);

IF NOT EXISTS (SELECT 1 FROM RescueTeam WHERE TeamName = 'Saylani Sukkur Relief')
    INSERT INTO RescueTeam (TeamName, TeamType, Street, Area, City, Province, AvailabilityStatus, Capacity)
    VALUES ('Saylani Sukkur Relief', 'Rescue', 'Military Road', 'Pano Akil', 'Sukkur', 'Sindh', 'Available', 30);

-- 3) HOSPITALS
IF NOT EXISTS (SELECT 1 FROM Hospital WHERE HospitalName = 'JPMC (Jinnah Hospital) Karachi')
    INSERT INTO Hospital (HospitalName, Street, Area, City, Province, TotalBeds, AvailableBeds, ContactPhone)
    VALUES ('JPMC (Jinnah Hospital) Karachi', 'Rafiqui Shaheed Road', 'Cantt', 'Karachi', 'Sindh', 1500, 200, '021-99201300');

IF NOT EXISTS (SELECT 1 FROM Hospital WHERE HospitalName = 'Mayo Hospital Lahore')
    INSERT INTO Hospital (HospitalName, Street, Area, City, Province, TotalBeds, AvailableBeds, ContactPhone)
    VALUES ('Mayo Hospital Lahore', 'Hospital Road', 'Anarkali', 'Lahore', 'Punjab', 2400, 350, '042-99211100');

IF NOT EXISTS (SELECT 1 FROM Hospital WHERE HospitalName = 'Lady Reading Hospital Peshawar')
    INSERT INTO Hospital (HospitalName, Street, Area, City, Province, TotalBeds, AvailableBeds, ContactPhone)
    VALUES ('Lady Reading Hospital Peshawar', 'LRH Road', 'Old City', 'Peshawar', 'KPK', 1800, 150, '091-9211430');

-- 4) WAREHOUSES (Linking to existing warehouse1 user)
DECLARE @WarehouseMgrID INT = (SELECT TOP 1 UserID FROM [User] WHERE Username = 'warehouse1');

IF NOT EXISTS (SELECT 1 FROM Warehouse WHERE WarehouseName = 'Karachi Port Trust Relief Hub')
    INSERT INTO Warehouse (WarehouseName, Street, Area, City, Province, Capacity, ManagerID, ContactPhone)
    VALUES ('Karachi Port Trust Relief Hub', 'Kemari Road', 'West Wharf', 'Karachi', 'Sindh', 5000, @WarehouseMgrID, '021-32314141');

IF NOT EXISTS (SELECT 1 FROM Warehouse WHERE WarehouseName = 'Lahore Central Logistics Hub')
    INSERT INTO Warehouse (WarehouseName, Street, Area, City, Province, Capacity, ManagerID, ContactPhone)
    VALUES ('Lahore Central Logistics Hub', 'GT Road', 'Shahdara', 'Lahore', 'Punjab', 8000, @WarehouseMgrID, '042-37911223');

-- 5) RESOURCES
IF NOT EXISTS (SELECT 1 FROM Resource WHERE ResourceName = 'Wheat Flour (Atta)')
    INSERT INTO Resource (ResourceName, ResourceType, Unit, [Description])
    VALUES ('Wheat Flour (Atta)', 'Food', '10kg Bag', 'Basic staple food supply');

IF NOT EXISTS (SELECT 1 FROM Resource WHERE ResourceName = 'Mineral Water')
    INSERT INTO Resource (ResourceName, ResourceType, Unit, [Description])
    VALUES ('Mineral Water', 'Water', '1.5L Bottle', 'Safe drinking water');

IF NOT EXISTS (SELECT 1 FROM Resource WHERE ResourceName = 'ORS Packets')
    INSERT INTO Resource (ResourceName, ResourceType, Unit, [Description])
    VALUES ('ORS Packets', 'Medicine', 'Box of 20', 'Oral Rehydration Salts for heatwave/cholera');

IF NOT EXISTS (SELECT 1 FROM Resource WHERE ResourceName = 'Emergency Tents')
    INSERT INTO Resource (ResourceName, ResourceType, Unit, [Description])
    VALUES ('Emergency Tents', 'Shelter', 'Per Tent', 'Weatherproof 4-person tents');

-- 6) INVENTORY (Populating Karachi Warehouse)
DECLARE @KHI_WarehouseID INT = (SELECT TOP 1 WarehouseID FROM Warehouse WHERE WarehouseName = 'Karachi Port Trust Relief Hub');
DECLARE @AttaID INT = (SELECT TOP 1 ResourceID FROM Resource WHERE ResourceName = 'Wheat Flour (Atta)');
DECLARE @WaterID INT = (SELECT TOP 1 ResourceID FROM Resource WHERE ResourceName = 'Mineral Water');
DECLARE @OrsID INT = (SELECT TOP 1 ResourceID FROM Resource WHERE ResourceName = 'ORS Packets');
DECLARE @TentID INT = (SELECT TOP 1 ResourceID FROM Resource WHERE ResourceName = 'Emergency Tents');

IF @KHI_WarehouseID IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Inventory WHERE WarehouseID = @KHI_WarehouseID AND ResourceID = @AttaID)
        INSERT INTO Inventory (WarehouseID, ResourceID, Quantity, MinThreshold, MaxCapacity)
        VALUES (@KHI_WarehouseID, @AttaID, 500, 100, 1000);

    IF NOT EXISTS (SELECT 1 FROM Inventory WHERE WarehouseID = @KHI_WarehouseID AND ResourceID = @WaterID)
        INSERT INTO Inventory (WarehouseID, ResourceID, Quantity, MinThreshold, MaxCapacity)
        VALUES (@KHI_WarehouseID, @WaterID, 2000, 500, 5000);

    IF NOT EXISTS (SELECT 1 FROM Inventory WHERE WarehouseID = @KHI_WarehouseID AND ResourceID = @OrsID)
        INSERT INTO Inventory (WarehouseID, ResourceID, Quantity, MinThreshold, MaxCapacity)
        VALUES (@KHI_WarehouseID, @OrsID, 1500, 200, 3000);

    IF NOT EXISTS (SELECT 1 FROM Inventory WHERE WarehouseID = @KHI_WarehouseID AND ResourceID = @TentID)
        INSERT INTO Inventory (WarehouseID, ResourceID, Quantity, MinThreshold, MaxCapacity)
        VALUES (@KHI_WarehouseID, @TentID, 50, 20, 200);
END

-- 7) CITIZENS
IF NOT EXISTS (SELECT 1 FROM Citizen WHERE NationalID = '42101-1234567-1')
    INSERT INTO Citizen (FirstName, LastName, NationalID, Email, Street, Area, City, Province)
    VALUES ('Ahmed', 'Khan', '42101-1234567-1', 'ahmed.khan@email.com', 'Street 5', 'DHA Phase 6', 'Karachi', 'Sindh');

IF NOT EXISTS (SELECT 1 FROM Citizen WHERE NationalID = '35202-9876543-2')
    INSERT INTO Citizen (FirstName, LastName, NationalID, Email, Street, Area, City, Province)
    VALUES ('Fatima', 'Bibi', '35202-9876543-2', 'fatima.bibi@email.com', 'Lane 12', 'Model Town', 'Lahore', 'Punjab');

-- 8) DONORS
IF NOT EXISTS (SELECT 1 FROM Donor WHERE Email = 'ali.foundation@email.com')
    INSERT INTO Donor (FirstName, LastName, DonorType, OrganizationName, Email, Street, Area, City, Province)
    VALUES ('Ali', 'Raza', 'Organization', 'Ali Raza Welfare Trust', 'ali.foundation@email.com', 'Main Blvd', 'Gulberg', 'Lahore', 'Punjab');

IF NOT EXISTS (SELECT 1 FROM Donor WHERE Email = 'sarah.malik@email.com')
    INSERT INTO Donor (FirstName, LastName, DonorType, Email, Street, Area, City, Province)
    VALUES ('Sarah', 'Malik', 'Individual', 'sarah.malik@email.com', 'Apartment 4B', 'E-11', 'Islamabad', 'Federal');

-- 9) DONATIONS (Linking to Karachi Flood Event)
DECLARE @FloodEventID INT = (SELECT TOP 1 EventID FROM DisasterEvent WHERE EventName = 'Karachi Urban Flooding 2024');
DECLARE @AliDonorID INT = (SELECT TOP 1 DonorID FROM Donor WHERE Email = 'ali.foundation@email.com');

IF @FloodEventID IS NOT NULL AND @AliDonorID IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Donation WHERE DonorID = @AliDonorID AND EventID = @FloodEventID AND Amount = 500000)
        INSERT INTO Donation (DonorID, EventID, Amount, DonationDate, PaymentMethod, [Status], ReceiptNumber)
        VALUES (@AliDonorID, @FloodEventID, 500000, SYSUTCDATETIME(), 'BankTransfer', 'Confirmed', 'REC-KHI-2024-001');
END

-- 10) EMERGENCY REPORTS (Linking to Quetta Earthquake)
DECLARE @QuettaEventID INT = (SELECT TOP 1 EventID FROM DisasterEvent WHERE EventName = 'Quetta Earthquake 2024');
DECLARE @AhmedCitizenID INT = (SELECT TOP 1 CitizenID FROM Citizen WHERE NationalID = '42101-1234567-1');

IF @QuettaEventID IS NOT NULL AND @AhmedCitizenID IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM EmergencyReport WHERE CitizenID = @AhmedCitizenID AND EventID = @QuettaEventID)
        INSERT INTO EmergencyReport (CitizenID, EventID, Street, Area, City, Province, DisasterType, SeverityLevel, ReportTime, [Status], [Source], [Description])
        VALUES (@AhmedCitizenID, @QuettaEventID, 'Civil Lines', 'Cantt', 'Quetta', 'Balochistan', 'Earthquake', 'High', SYSUTCDATETIME(), 'Pending', 'Mobile', 'Building walls cracked, need immediate inspection.');
END

GO
PRINT 'Pakistan real-time test data population complete.';
GO
