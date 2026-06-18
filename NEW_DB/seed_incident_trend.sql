USE Final_DB;
GO

SET NOCOUNT ON;
GO

/* =====================================================
   SEED DATA: Incident Volume Trend (Multi-day) - Corrected Sources
   ===================================================== */

DECLARE @CitId INT = (SELECT TOP 1 CitizenId FROM Citizen);
DECLARE @EvId INT = (SELECT TOP 1 EventID FROM DisasterEvent);

IF @CitId IS NOT NULL AND @EvId IS NOT NULL
BEGIN
    -- Insert reports for the last 10 days
    INSERT INTO EmergencyReport (CitizenID, EventID, Street, Area, City, Province, DisasterType, SeverityLevel, ReportTime, [Status], [Source], [Description])
    VALUES 
        (@CitId, @EvId, 'Street 1', 'Area 1', 'City 1', 'Sindh', 'Flood', 'Medium', DATEADD(day, -1, GETUTCDATE()), 'Pending', 'Helpline', 'Test 1'),
        (@CitId, @EvId, 'Street 2', 'Area 1', 'City 1', 'Sindh', 'Fire', 'Medium', DATEADD(day, -2, GETUTCDATE()), 'Pending', 'Helpline', 'Test 2'),
        (@CitId, @EvId, 'Street 2', 'Area 1', 'City 1', 'Sindh', 'Fire', 'Medium', DATEADD(day, -2, GETUTCDATE()), 'Pending', 'Helpline', 'Test 2b'),
        (@CitId, @EvId, 'Street 3', 'Area 1', 'City 1', 'Sindh', 'Flood', 'Critical', DATEADD(day, -3, GETUTCDATE()), 'Pending', 'MonitoringSystem', 'Test 3'),
        (@CitId, @EvId, 'Street 5', 'Area 1', 'City 1', 'Sindh', 'Fire', 'Low', DATEADD(day, -5, GETUTCDATE()), 'Pending', 'Mobile', 'Test 5'),
        (@CitId, @EvId, 'Street 5', 'Area 1', 'City 1', 'Sindh', 'Fire', 'Low', DATEADD(day, -5, GETUTCDATE()), 'Pending', 'Mobile', 'Test 5b'),
        (@CitId, @EvId, 'Street 5', 'Area 1', 'City 1', 'Sindh', 'Fire', 'Low', DATEADD(day, -5, GETUTCDATE()), 'Pending', 'Mobile', 'Test 5c'),
        (@CitId, @EvId, 'Street 7', 'Area 1', 'City 1', 'Sindh', 'Flood', 'High', DATEADD(day, -7, GETUTCDATE()), 'Pending', 'Helpline', 'Test 7'),
        (@CitId, @EvId, 'Street 10', 'Area 1', 'City 1', 'Sindh', 'Earthquake', 'Critical', DATEADD(day, -10, GETUTCDATE()), 'Pending', 'MonitoringSystem', 'Test 10');
END
GO

PRINT 'Multi-day incident trend data seeded successfully.';
GO
