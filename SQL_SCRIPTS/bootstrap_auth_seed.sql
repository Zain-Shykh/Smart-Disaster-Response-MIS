USE Final_DB;
GO

/* =====================================================
   AUTH/RBAC BOOTSTRAP SEED (IDEMPOTENT)
   Purpose: first-run clone setup on a fresh local database.
   Creates default roles, users, and user-role mappings.

   NOTE:
   - Passwords are inserted as plain text intentionally for bootstrap.
   - Backend login migrates to PBKDF2 hash on successful login.
   - Change these passwords after first successful admin login.
   ===================================================== */

SET NOCOUNT ON;

-- 1) Roles
IF NOT EXISTS (SELECT 1 FROM [Role] WHERE RoleName = 'Administrator')
    INSERT INTO [Role] (RoleName, [Description]) VALUES ('Administrator', 'Full system access');

IF NOT EXISTS (SELECT 1 FROM [Role] WHERE RoleName = 'EmergencyOperator')
    INSERT INTO [Role] (RoleName, [Description]) VALUES ('EmergencyOperator', 'Incident and response operations');

IF NOT EXISTS (SELECT 1 FROM [Role] WHERE RoleName = 'FieldOfficer')
    INSERT INTO [Role] (RoleName, [Description]) VALUES ('FieldOfficer', 'Field execution and medical/routing operations');

IF NOT EXISTS (SELECT 1 FROM [Role] WHERE RoleName = 'WarehouseManager')
    INSERT INTO [Role] (RoleName, [Description]) VALUES ('WarehouseManager', 'Logistics, inventory, and allocation operations');

IF NOT EXISTS (SELECT 1 FROM [Role] WHERE RoleName = 'FinanceOfficer')
    INSERT INTO [Role] (RoleName, [Description]) VALUES ('FinanceOfficer', 'Donation, expense, and audit governance operations');

-- 2) Users (bootstrap passwords)
IF EXISTS (SELECT 1 FROM [User] WHERE Username = 'admin1')
    UPDATE [User]
    SET FirstName = 'Admin',
        LastName = 'One',
        PasswordHash = 'Admin@1234',
        Email = 'admin1@test.com',
        IsActive = 1
    WHERE Username = 'admin1';
ELSE
    INSERT INTO [User] (FirstName, LastName, Username, PasswordHash, Email, IsActive)
    VALUES ('Admin', 'One', 'admin1', 'Admin@1234', 'admin1@test.com', 1);

IF EXISTS (SELECT 1 FROM [User] WHERE Username = 'ops2')
    UPDATE [User]
    SET FirstName = 'Ops',
        LastName = 'Two',
        PasswordHash = 'Ops@1234',
        Email = 'ops2@test.com',
        IsActive = 1
    WHERE Username = 'ops2';
ELSE
    INSERT INTO [User] (FirstName, LastName, Username, PasswordHash, Email, IsActive)
    VALUES ('Ops', 'Two', 'ops2', 'Ops@1234', 'ops2@test.com', 1);

IF EXISTS (SELECT 1 FROM [User] WHERE Username = 'field1')
    UPDATE [User]
    SET FirstName = 'Field',
        LastName = 'One',
        PasswordHash = 'Field@1234',
        Email = 'field1@test.com',
        IsActive = 1
    WHERE Username = 'field1';
ELSE
    INSERT INTO [User] (FirstName, LastName, Username, PasswordHash, Email, IsActive)
    VALUES ('Field', 'One', 'field1', 'Field@1234', 'field1@test.com', 1);

IF EXISTS (SELECT 1 FROM [User] WHERE Username = 'warehouse1')
    UPDATE [User]
    SET FirstName = 'Warehouse',
        LastName = 'One',
        PasswordHash = 'Warehouse@1234',
        Email = 'warehouse1@test.com',
        IsActive = 1
    WHERE Username = 'warehouse1';
ELSE
    INSERT INTO [User] (FirstName, LastName, Username, PasswordHash, Email, IsActive)
    VALUES ('Warehouse', 'One', 'warehouse1', 'Warehouse@1234', 'warehouse1@test.com', 1);

IF EXISTS (SELECT 1 FROM [User] WHERE Username = 'finance1')
    UPDATE [User]
    SET FirstName = 'Finance',
        LastName = 'One',
        PasswordHash = 'Finance@1234',
        Email = 'finance1@test.com',
        IsActive = 1
    WHERE Username = 'finance1';
ELSE
    INSERT INTO [User] (FirstName, LastName, Username, PasswordHash, Email, IsActive)
    VALUES ('Finance', 'One', 'finance1', 'Finance@1234', 'finance1@test.com', 1);

DECLARE @AdminUserID INT = (SELECT TOP 1 UserID FROM [User] WHERE Username = 'admin1');
DECLARE @OpsUserID INT = (SELECT TOP 1 UserID FROM [User] WHERE Username = 'ops2');
DECLARE @FieldUserID INT = (SELECT TOP 1 UserID FROM [User] WHERE Username = 'field1');
DECLARE @WarehouseUserID INT = (SELECT TOP 1 UserID FROM [User] WHERE Username = 'warehouse1');
DECLARE @FinanceUserID INT = (SELECT TOP 1 UserID FROM [User] WHERE Username = 'finance1');

DECLARE @AdminRoleID INT = (SELECT TOP 1 RoleID FROM [Role] WHERE RoleName = 'Administrator');
DECLARE @OpsRoleID INT = (SELECT TOP 1 RoleID FROM [Role] WHERE RoleName = 'EmergencyOperator');
DECLARE @FieldRoleID INT = (SELECT TOP 1 RoleID FROM [Role] WHERE RoleName = 'FieldOfficer');
DECLARE @WarehouseRoleID INT = (SELECT TOP 1 RoleID FROM [Role] WHERE RoleName = 'WarehouseManager');
DECLARE @FinanceRoleID INT = (SELECT TOP 1 RoleID FROM [Role] WHERE RoleName = 'FinanceOfficer');

-- 3) UserRole mappings
IF NOT EXISTS (SELECT 1 FROM UserRole WHERE UserID = @AdminUserID AND RoleID = @AdminRoleID)
    INSERT INTO UserRole (UserID, RoleID, AssignedBy) VALUES (@AdminUserID, @AdminRoleID, NULL);

IF NOT EXISTS (SELECT 1 FROM UserRole WHERE UserID = @OpsUserID AND RoleID = @OpsRoleID)
    INSERT INTO UserRole (UserID, RoleID, AssignedBy) VALUES (@OpsUserID, @OpsRoleID, @AdminUserID);

IF NOT EXISTS (SELECT 1 FROM UserRole WHERE UserID = @FieldUserID AND RoleID = @FieldRoleID)
    INSERT INTO UserRole (UserID, RoleID, AssignedBy) VALUES (@FieldUserID, @FieldRoleID, @AdminUserID);

IF NOT EXISTS (SELECT 1 FROM UserRole WHERE UserID = @WarehouseUserID AND RoleID = @WarehouseRoleID)
    INSERT INTO UserRole (UserID, RoleID, AssignedBy) VALUES (@WarehouseUserID, @WarehouseRoleID, @AdminUserID);

IF NOT EXISTS (SELECT 1 FROM UserRole WHERE UserID = @FinanceUserID AND RoleID = @FinanceRoleID)
    INSERT INTO UserRole (UserID, RoleID, AssignedBy) VALUES (@FinanceUserID, @FinanceRoleID, @AdminUserID);

-- 4) Verification snapshot
SELECT u.Username, r.RoleName
FROM [User] u
JOIN UserRole ur ON ur.UserID = u.UserID
JOIN [Role] r ON r.RoleID = ur.RoleID
WHERE u.Username IN ('admin1', 'ops2', 'field1', 'warehouse1', 'finance1')
ORDER BY u.Username, r.RoleName;
GO
