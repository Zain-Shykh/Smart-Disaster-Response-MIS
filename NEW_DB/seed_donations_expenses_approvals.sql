USE Final_DB;
GO

SET NOCOUNT ON;
GO

/* =====================================================
   ROBUST SEED DATA: Pakistani Disaster Response
   ===================================================== */

-- 1. Ensure Events Exist
IF NOT EXISTS (SELECT 1 FROM DisasterEvent WHERE EventName = 'Karachi Urban Flooding 2024')
    INSERT INTO DisasterEvent (EventName, DisasterType, StartTime, EndTime, Street, Area, City, Province, [Status], AffectedPopulation, TotalReports)
    VALUES ('Karachi Urban Flooding 2024', 'Flood', '2024-04-15 08:00:00', NULL, 'I.I. Chundrigar Road', 'Clifton', 'Karachi', 'Sindh', 'Active', 15000, 0);

IF NOT EXISTS (SELECT 1 FROM DisasterEvent WHERE EventName = 'Lahore Heat Wave 2024')
    INSERT INTO DisasterEvent (EventName, DisasterType, StartTime, EndTime, Street, Area, City, Province, [Status], AffectedPopulation, TotalReports)
    VALUES ('Lahore Heat Wave 2024', 'Heatwave', '2024-05-01 06:00:00', NULL, 'Mall Road', 'Defense', 'Lahore', 'Punjab', 'Active', 8000, 0);

IF NOT EXISTS (SELECT 1 FROM DisasterEvent WHERE EventName = 'Islamabad Earthquake Response')
    INSERT INTO DisasterEvent (EventName, DisasterType, StartTime, EndTime, Street, Area, City, Province, [Status], AffectedPopulation, TotalReports)
    VALUES ('Islamabad Earthquake Response', 'Earthquake', '2024-03-20 14:30:00', NULL, 'Civic Centre Road', 'F-7', 'Islamabad', 'Capital', 'Active', 5000, 0);
GO

-- 2. Ensure Donors Exist
IF NOT EXISTS (SELECT 1 FROM Donor WHERE OrganizationName = 'Al-Khidmat Foundation')
    INSERT INTO Donor (FirstName, LastName, DonorType, OrganizationName, Email, Street, Area, City, Province)
    VALUES ('Muhammad', 'Ahmed', 'Organization', 'Al-Khidmat Foundation', 'contact@alkhidmat.org', 'Main Boulevard', 'Karachi', 'Karachi', 'Sindh');

IF NOT EXISTS (SELECT 1 FROM Donor WHERE OrganizationName = 'Edhi Foundation')
    INSERT INTO Donor (FirstName, LastName, DonorType, OrganizationName, Email, Street, Area, City, Province)
    VALUES ('Fatima', 'Khan', 'Organization', 'Edhi Foundation', 'edhi@edhifoundation.org', 'Mithadar', 'Karachi', 'Karachi', 'Sindh');

IF NOT EXISTS (SELECT 1 FROM Donor WHERE OrganizationName = 'Pakistan Red Crescent')
    INSERT INTO Donor (FirstName, LastName, DonorType, OrganizationName, Email, Street, Area, City, Province)
    VALUES ('Noor', 'Ahmed', 'Organization', 'Pakistan Red Crescent', 'donations@prcs.org.pk', 'Jinnah Road', 'Lahore', 'Lahore', 'Punjab');
GO

-- 3. Populate Donations
DECLARE @Eid1 INT = (SELECT TOP 1 EventID FROM DisasterEvent WHERE EventName LIKE '%Karachi%');
DECLARE @Eid2 INT = (SELECT TOP 1 EventID FROM DisasterEvent WHERE EventName LIKE '%Lahore%');
DECLARE @Did1 INT = (SELECT TOP 1 DonorID FROM Donor WHERE OrganizationName LIKE '%Al-Khidmat%');
DECLARE @Did2 INT = (SELECT TOP 1 DonorID FROM Donor WHERE OrganizationName LIKE '%Edhi%');

IF NOT EXISTS (SELECT 1 FROM Donation WHERE ReceiptNumber = 'SEED-DON-001')
    INSERT INTO Donation (DonorID, EventID, Amount, DonationDate, PaymentMethod, [Status], ReceiptNumber)
    VALUES (@Did1, @Eid1, 500000, GETDATE(), 'BankTransfer', 'Confirmed', 'SEED-DON-001');

IF NOT EXISTS (SELECT 1 FROM Donation WHERE ReceiptNumber = 'SEED-DON-002')
    INSERT INTO Donation (DonorID, EventID, Amount, DonationDate, PaymentMethod, [Status], ReceiptNumber)
    VALUES (@Did2, @Eid1, 250000, GETDATE(), 'Online', 'Pending', 'SEED-DON-002');

IF NOT EXISTS (SELECT 1 FROM Donation WHERE ReceiptNumber = 'SEED-DON-003')
    INSERT INTO Donation (DonorID, EventID, Amount, DonationDate, PaymentMethod, [Status], ReceiptNumber)
    VALUES (@Did1, @Eid2, 1000000, GETDATE(), 'BankTransfer', 'Confirmed', 'SEED-DON-003');
GO

-- 4. Populate Expenses
DECLARE @Eid1 INT = (SELECT TOP 1 EventID FROM DisasterEvent WHERE EventName LIKE '%Karachi%');
DECLARE @Eid2 INT = (SELECT TOP 1 EventID FROM DisasterEvent WHERE EventName LIKE '%Lahore%');
DECLARE @FinId INT = (SELECT TOP 1 UserID FROM [User] WHERE Username = 'finance1');

IF NOT EXISTS (SELECT 1 FROM Expense WHERE Description = 'Emergency Food Supplies - Karachi')
    INSERT INTO Expense (EventID, ApprovedBy, Category, Amount, [Description], ExpenseDate, PaymentStatus)
    VALUES (@Eid1, @FinId, 'Procurement', 300000, 'Emergency Food Supplies - Karachi', GETDATE(), 'Approved');

IF NOT EXISTS (SELECT 1 FROM Expense WHERE Description = 'Medical Kits - Lahore')
    INSERT INTO Expense (EventID, ApprovedBy, Category, Amount, [Description], ExpenseDate, PaymentStatus)
    VALUES (@Eid2, @FinId, 'Medical', 150000, 'Medical Kits - Lahore', GETDATE(), 'Pending');

IF NOT EXISTS (SELECT 1 FROM Expense WHERE Description = 'Vehicle Fuel - Rescue Ops')
    INSERT INTO Expense (EventID, ApprovedBy, Category, Amount, [Description], ExpenseDate, PaymentStatus)
    VALUES (@Eid1, @FinId, 'Logistics', 80000, 'Vehicle Fuel - Rescue Ops', GETDATE(), 'Approved');
GO

PRINT 'Database seeding completed successfully.';
GO
