USE master;
GO
CREATE DATABASE Final_DB;
GO
USE Final_DB;

-- Smart Disaster Response MIS
-- SQL Server (SSMS) DDL: CREATE TABLE statements

CREATE TABLE [User] (
    UserID INT IDENTITY(1,1) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Username VARCHAR(100) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_User_CreatedAt DEFAULT SYSUTCDATETIME(),
    LastLoginAt DATETIME2 NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_User_IsActive DEFAULT 1,
    CONSTRAINT PK_User PRIMARY KEY (UserID),
    CONSTRAINT UQ_User_Username UNIQUE (Username),
    CONSTRAINT UQ_User_Email UNIQUE (Email)
);
GO

CREATE TABLE [Role] (
    RoleID INT IDENTITY(1,1) NOT NULL,
    RoleName VARCHAR(100) NOT NULL,
    [Description] VARCHAR(500) NULL,
    CONSTRAINT PK_Role PRIMARY KEY (RoleID),
    CONSTRAINT UQ_Role_RoleName UNIQUE (RoleName)
);
GO

CREATE TABLE Permission (
    PermissionID INT IDENTITY(1,1) NOT NULL,
    PermissionName VARCHAR(120) NOT NULL,
    [Module] VARCHAR(100) NOT NULL,
    [Action] VARCHAR(20) NOT NULL,
    CONSTRAINT PK_Permission PRIMARY KEY (PermissionID),
    CONSTRAINT CK_Permission_Action CHECK ([Action] IN ('CREATE','READ','UPDATE','DELETE')),
    CONSTRAINT UQ_Permission_Module_Action_Name UNIQUE ([Module], [Action], PermissionName)
);
GO

CREATE TABLE Citizen (
    CitizenID INT IDENTITY(1,1) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    NationalID VARCHAR(50) NOT NULL,
    Email VARCHAR(255) NULL,
    Street VARCHAR(200) NOT NULL,
    Area VARCHAR(120) NOT NULL,
    City VARCHAR(120) NOT NULL,
    Province VARCHAR(120) NOT NULL,
    TotalReports INT NOT NULL CONSTRAINT DF_Citizen_TotalReports DEFAULT 0,
    CONSTRAINT PK_Citizen PRIMARY KEY (CitizenID),
    CONSTRAINT UQ_Citizen_NationalID UNIQUE (NationalID),
    CONSTRAINT UQ_Citizen_Email UNIQUE (Email),
    CONSTRAINT CK_Citizen_TotalReports CHECK (TotalReports >= 0)
);
GO

CREATE TABLE DisasterEvent (
    EventID INT IDENTITY(1,1) NOT NULL,
    EventName VARCHAR(200) NOT NULL,
    DisasterType VARCHAR(50) NOT NULL,
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NULL,
    DurationMinutes AS (CASE WHEN EndTime IS NULL THEN NULL ELSE DATEDIFF(MINUTE, StartTime, EndTime) END),
    Street VARCHAR(200) NOT NULL,
    Area VARCHAR(120) NOT NULL,
    City VARCHAR(120) NOT NULL,
    Province VARCHAR(120) NOT NULL,
    [Status] VARCHAR(20) NOT NULL,
    AffectedPopulation INT NOT NULL CONSTRAINT DF_DisasterEvent_AffectedPopulation DEFAULT 0,
    TotalReports INT NOT NULL CONSTRAINT DF_DisasterEvent_TotalReports DEFAULT 0,
    CONSTRAINT PK_DisasterEvent PRIMARY KEY (EventID),
    CONSTRAINT CK_DisasterEvent_Status CHECK ([Status] IN ('Active','Contained','Resolved')),
    CONSTRAINT CK_DisasterEvent_Time CHECK (EndTime IS NULL OR EndTime >= StartTime),
    CONSTRAINT CK_DisasterEvent_AffectedPopulation CHECK (AffectedPopulation >= 0),
    CONSTRAINT CK_DisasterEvent_TotalReports CHECK (TotalReports >= 0)
);
GO

CREATE TABLE EmergencyReport (
    ReportID INT IDENTITY(1,1) NOT NULL,
    CitizenID INT NOT NULL,
    EventID INT NULL,
    Street VARCHAR(200) NOT NULL,
    Area VARCHAR(120) NOT NULL,
    City VARCHAR(120) NOT NULL,
    Province VARCHAR(120) NOT NULL,
    Latitude DECIMAL(10,7) NULL,
    Longitude DECIMAL(10,7) NULL,
    DisasterType VARCHAR(50) NOT NULL,
    SeverityLevel VARCHAR(20) NOT NULL,
    ReportTime DATETIME2 NOT NULL,
    [Status] VARCHAR(20) NOT NULL,
    [Source] VARCHAR(30) NOT NULL,
    [Description] VARCHAR(2000) NULL,
    ResponseTimeMinutes INT NULL,
    ResolutionTimeMinutes INT NULL,
    CONSTRAINT PK_EmergencyReport PRIMARY KEY (ReportID),
    CONSTRAINT FK_EmergencyReport_Citizen FOREIGN KEY (CitizenID) REFERENCES Citizen(CitizenID),
    CONSTRAINT FK_EmergencyReport_DisasterEvent FOREIGN KEY (EventID) REFERENCES DisasterEvent(EventID),
    CONSTRAINT CK_EmergencyReport_Severity CHECK (SeverityLevel IN ('Low','Medium','High','Critical')),
    CONSTRAINT CK_EmergencyReport_Status CHECK ([Status] IN ('Pending','InProgress','Resolved','Closed')),
    CONSTRAINT CK_EmergencyReport_Source CHECK ([Source] IN ('Mobile','Helpline','MonitoringSystem')),
    CONSTRAINT CK_EmergencyReport_Latitude CHECK (Latitude IS NULL OR (Latitude BETWEEN -90 AND 90)),
    CONSTRAINT CK_EmergencyReport_Longitude CHECK (Longitude IS NULL OR (Longitude BETWEEN -180 AND 180))
);
GO

CREATE TABLE RescueTeam (
    TeamID INT IDENTITY(1,1) NOT NULL,
    TeamName VARCHAR(150) NOT NULL,
    TeamType VARCHAR(20) NOT NULL,
    Street VARCHAR(200) NOT NULL,
    Area VARCHAR(120) NOT NULL,
    City VARCHAR(120) NOT NULL,
    Province VARCHAR(120) NOT NULL,
    Latitude DECIMAL(10,7) NULL,
    Longitude DECIMAL(10,7) NULL,
    AvailabilityStatus VARCHAR(20) NOT NULL,
    Capacity INT NOT NULL,
    TotalAssignments INT NOT NULL CONSTRAINT DF_RescueTeam_TotalAssignments DEFAULT 0,
    CONSTRAINT PK_RescueTeam PRIMARY KEY (TeamID),
    CONSTRAINT CK_RescueTeam_TeamType CHECK (TeamType IN ('Medical','Fire','Rescue','Search')),
    CONSTRAINT CK_RescueTeam_AvailabilityStatus CHECK (AvailabilityStatus IN ('Available','Assigned','Busy','Completed')),
    CONSTRAINT CK_RescueTeam_Capacity CHECK (Capacity >= 0),
    CONSTRAINT CK_RescueTeam_TotalAssignments CHECK (TotalAssignments >= 0),
    CONSTRAINT CK_RescueTeam_Latitude CHECK (Latitude IS NULL OR (Latitude BETWEEN -90 AND 90)),
    CONSTRAINT CK_RescueTeam_Longitude CHECK (Longitude IS NULL OR (Longitude BETWEEN -180 AND 180))
);
GO

CREATE TABLE Resource (
    ResourceID INT IDENTITY(1,1) NOT NULL,
    ResourceName VARCHAR(120) NOT NULL,
    ResourceType VARCHAR(20) NOT NULL,
    Unit VARCHAR(30) NOT NULL,
    [Description] VARCHAR(1000) NULL,
    CONSTRAINT PK_Resource PRIMARY KEY (ResourceID),
    CONSTRAINT CK_Resource_ResourceType CHECK (ResourceType IN ('Food','Water','Medicine','Shelter'))
);
GO

CREATE TABLE Warehouse (
    WarehouseID INT IDENTITY(1,1) NOT NULL,
    WarehouseName VARCHAR(150) NOT NULL,
    Street VARCHAR(200) NOT NULL,
    Area VARCHAR(120) NOT NULL,
    City VARCHAR(120) NOT NULL,
    Province VARCHAR(120) NOT NULL,
    Latitude DECIMAL(10,7) NULL,
    Longitude DECIMAL(10,7) NULL,
    Capacity INT NOT NULL,
    ManagerID INT NOT NULL,
    ContactPhone VARCHAR(30) NULL,
    ContactEmail VARCHAR(255) NULL,
    CONSTRAINT PK_Warehouse PRIMARY KEY (WarehouseID),
    CONSTRAINT FK_Warehouse_Manager FOREIGN KEY (ManagerID) REFERENCES [User](UserID),
    CONSTRAINT CK_Warehouse_Capacity CHECK (Capacity >= 0),
    CONSTRAINT CK_Warehouse_Latitude CHECK (Latitude IS NULL OR (Latitude BETWEEN -90 AND 90)),
    CONSTRAINT CK_Warehouse_Longitude CHECK (Longitude IS NULL OR (Longitude BETWEEN -180 AND 180))
);
GO

CREATE TABLE Hospital (
    HospitalID INT IDENTITY(1,1) NOT NULL,
    HospitalName VARCHAR(200) NOT NULL,
    Street VARCHAR(200) NOT NULL,
    Area VARCHAR(120) NOT NULL,
    City VARCHAR(120) NOT NULL,
    Province VARCHAR(120) NOT NULL,
    TotalBeds INT NOT NULL,
    AvailableBeds INT NOT NULL,
    OccupancyRate AS (CASE WHEN TotalBeds = 0 THEN NULL ELSE ((TotalBeds - AvailableBeds) * 100.0 / TotalBeds) END),
    ContactPhone VARCHAR(30) NULL,
    ContactEmail VARCHAR(255) NULL,
    CONSTRAINT PK_Hospital PRIMARY KEY (HospitalID),
    CONSTRAINT CK_Hospital_TotalBeds CHECK (TotalBeds >= 0),
    CONSTRAINT CK_Hospital_AvailableBeds CHECK (AvailableBeds >= 0 AND AvailableBeds <= TotalBeds)
);
GO

CREATE TABLE Patient (
    PatientID INT IDENTITY(1,1) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Age INT NULL,
    Gender VARCHAR(20) NULL,
    NationalID VARCHAR(50) NULL,
    BloodType VARCHAR(5) NULL,
    ContactPhone VARCHAR(30) NULL,
    CONSTRAINT PK_Patient PRIMARY KEY (PatientID),
    CONSTRAINT UQ_Patient_NationalID UNIQUE (NationalID),
    CONSTRAINT CK_Patient_Age CHECK (Age IS NULL OR (Age BETWEEN 0 AND 130)),
    CONSTRAINT CK_Patient_BloodType CHECK (BloodType IS NULL OR BloodType IN ('A+','A-','B+','B-','AB+','AB-','O+','O-'))
);
GO

CREATE TABLE Donor (
    DonorID INT IDENTITY(1,1) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    DonorType VARCHAR(20) NOT NULL,
    OrganizationName VARCHAR(200) NULL,
    Email VARCHAR(255) NULL,
    Street VARCHAR(200) NOT NULL,
    Area VARCHAR(120) NOT NULL,
    City VARCHAR(120) NOT NULL,
    Province VARCHAR(120) NOT NULL,
    CONSTRAINT PK_Donor PRIMARY KEY (DonorID),
    CONSTRAINT UQ_Donor_Email UNIQUE (Email),
    CONSTRAINT CK_Donor_DonorType CHECK (DonorType IN ('Individual','Organization'))
);
GO

CREATE TABLE Inventory (
    InventoryID INT IDENTITY(1,1) NOT NULL,
    WarehouseID INT NOT NULL,
    ResourceID INT NOT NULL,
    Quantity DECIMAL(14,2) NOT NULL,
    MinThreshold DECIMAL(14,2) NOT NULL,
    MaxCapacity DECIMAL(14,2) NOT NULL,
    LastUpdated DATETIME2 NOT NULL CONSTRAINT DF_Inventory_LastUpdated DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Inventory PRIMARY KEY (InventoryID),
    CONSTRAINT FK_Inventory_Warehouse FOREIGN KEY (WarehouseID) REFERENCES Warehouse(WarehouseID),
    CONSTRAINT FK_Inventory_Resource FOREIGN KEY (ResourceID) REFERENCES Resource(ResourceID),
    CONSTRAINT UQ_Inventory_Warehouse_Resource UNIQUE (WarehouseID, ResourceID),
    CONSTRAINT CK_Inventory_Quantity CHECK (Quantity >= 0),
    CONSTRAINT CK_Inventory_MinThreshold CHECK (MinThreshold >= 0),
    CONSTRAINT CK_Inventory_MaxCapacity CHECK (MaxCapacity >= 0),
    CONSTRAINT CK_Inventory_Quantity_Max CHECK (Quantity <= MaxCapacity)
);
GO

CREATE TABLE TeamAssignment (
    AssignmentID INT IDENTITY(1,1) NOT NULL,
    TeamID INT NOT NULL,
    ReportID INT NOT NULL,
    AssignedBy INT NOT NULL,
    AssignmentTime DATETIME2 NOT NULL,
    CompletionTime DATETIME2 NULL,
    [Status] VARCHAR(20) NOT NULL,
    CONSTRAINT PK_TeamAssignment PRIMARY KEY (AssignmentID),
    CONSTRAINT FK_TeamAssignment_Team FOREIGN KEY (TeamID) REFERENCES RescueTeam(TeamID),
    CONSTRAINT FK_TeamAssignment_Report FOREIGN KEY (ReportID) REFERENCES EmergencyReport(ReportID),
    CONSTRAINT FK_TeamAssignment_AssignedBy FOREIGN KEY (AssignedBy) REFERENCES [User](UserID),
    CONSTRAINT CK_TeamAssignment_Status CHECK ([Status] IN ('Assigned','EnRoute','OnSite','Completed')),
    CONSTRAINT CK_TeamAssignment_Time CHECK (CompletionTime IS NULL OR CompletionTime >= AssignmentTime)
);
GO

CREATE TABLE TeamActivity (
    TeamID INT NOT NULL,
    ActivityID INT NOT NULL,
    ActivityType VARCHAR(100) NOT NULL,
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NULL,
    DurationMinutes AS (CASE WHEN EndTime IS NULL THEN NULL ELSE DATEDIFF(MINUTE, StartTime, EndTime) END),
    Notes VARCHAR(2000) NULL,
    Outcome VARCHAR(500) NULL,
    CONSTRAINT PK_TeamActivity PRIMARY KEY (TeamID, ActivityID),
    CONSTRAINT FK_TeamActivity_Team FOREIGN KEY (TeamID) REFERENCES RescueTeam(TeamID),
    CONSTRAINT CK_TeamActivity_Time CHECK (EndTime IS NULL OR EndTime >= StartTime)
);
GO

CREATE TABLE ResourceAllocation (
    AllocationID INT IDENTITY(1,1) NOT NULL,
    InventoryID INT NOT NULL,
    EventID INT NOT NULL,
    RequestedBy INT NOT NULL,
    Quantity DECIMAL(14,2) NOT NULL,
    RequestTime DATETIME2 NOT NULL,
    [Status] VARCHAR(20) NOT NULL,
    DispatchedAt DATETIME2 NULL,
    ConsumedAt DATETIME2 NULL,
    CONSTRAINT PK_ResourceAllocation PRIMARY KEY (AllocationID),
    CONSTRAINT FK_ResourceAllocation_Inventory FOREIGN KEY (InventoryID) REFERENCES Inventory(InventoryID),
    CONSTRAINT FK_ResourceAllocation_Event FOREIGN KEY (EventID) REFERENCES DisasterEvent(EventID),
    CONSTRAINT FK_ResourceAllocation_RequestedBy FOREIGN KEY (RequestedBy) REFERENCES [User](UserID),
    CONSTRAINT CK_ResourceAllocation_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_ResourceAllocation_Status CHECK ([Status] IN ('Pending','Approved','Dispatched','Consumed','Rejected')),
    CONSTRAINT CK_ResourceAllocation_DispatchedAt CHECK (DispatchedAt IS NULL OR DispatchedAt >= RequestTime),
    CONSTRAINT CK_ResourceAllocation_ConsumedAt CHECK (ConsumedAt IS NULL OR (DispatchedAt IS NOT NULL AND ConsumedAt >= DispatchedAt))
);
GO

CREATE TABLE InventoryAlert (
    InventoryID INT NOT NULL,
    AlertID INT NOT NULL,
    AlertType VARCHAR(20) NOT NULL,
    AlertTime DATETIME2 NOT NULL CONSTRAINT DF_InventoryAlert_AlertTime DEFAULT SYSUTCDATETIME(),
    [Status] VARCHAR(20) NOT NULL,
    ResolvedAt DATETIME2 NULL,
    CONSTRAINT PK_InventoryAlert PRIMARY KEY (InventoryID, AlertID),
    CONSTRAINT FK_InventoryAlert_Inventory FOREIGN KEY (InventoryID) REFERENCES Inventory(InventoryID),
    CONSTRAINT CK_InventoryAlert_Type CHECK (AlertType IN ('LowStock','OutOfStock')),
    CONSTRAINT CK_InventoryAlert_Status CHECK ([Status] IN ('Active','Resolved')),
    CONSTRAINT CK_InventoryAlert_ResolvedAt CHECK (ResolvedAt IS NULL OR ResolvedAt >= AlertTime)
);
GO

CREATE TABLE Donation (
    DonationID INT IDENTITY(1,1) NOT NULL,
    DonorID INT NOT NULL,
    EventID INT NOT NULL,
    Amount DECIMAL(14,2) NOT NULL,
    DonationDate DATETIME2 NOT NULL,
    PaymentMethod VARCHAR(20) NOT NULL,
    [Status] VARCHAR(20) NOT NULL,
    ReceiptNumber VARCHAR(100) NULL,
    CONSTRAINT PK_Donation PRIMARY KEY (DonationID),
    CONSTRAINT FK_Donation_Donor FOREIGN KEY (DonorID) REFERENCES Donor(DonorID),
    CONSTRAINT FK_Donation_Event FOREIGN KEY (EventID) REFERENCES DisasterEvent(EventID),
    CONSTRAINT UQ_Donation_ReceiptNumber UNIQUE (ReceiptNumber),
    CONSTRAINT CK_Donation_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Donation_PaymentMethod CHECK (PaymentMethod IN ('Cash','BankTransfer','Online')),
    CONSTRAINT CK_Donation_Status CHECK ([Status] IN ('Pending','Confirmed','Rejected'))
);
GO

CREATE TABLE Expense (
    ExpenseID INT IDENTITY(1,1) NOT NULL,
    EventID INT NOT NULL,
    ApprovedBy INT NULL,
    Category VARCHAR(20) NOT NULL,
    Amount DECIMAL(14,2) NOT NULL,
    [Description] VARCHAR(2000) NULL,
    ExpenseDate DATETIME2 NOT NULL,
    PaymentStatus VARCHAR(30) NOT NULL,
    CONSTRAINT PK_Expense PRIMARY KEY (ExpenseID),
    CONSTRAINT FK_Expense_Event FOREIGN KEY (EventID) REFERENCES DisasterEvent(EventID),
    CONSTRAINT FK_Expense_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES [User](UserID),
    CONSTRAINT CK_Expense_Category CHECK (Category IN ('Procurement','Operations','Medical','Logistics')),
    CONSTRAINT CK_Expense_Amount CHECK (Amount > 0)
);
GO

CREATE TABLE PatientAdmission (
    AdmissionID INT IDENTITY(1,1) NOT NULL,
    PatientID INT NOT NULL,
    HospitalID INT NOT NULL,
    ReportID INT NULL,
    AdmissionTime DATETIME2 NOT NULL,
    DischargeTime DATETIME2 NULL,
    [Condition] VARCHAR(20) NOT NULL,
    [Status] VARCHAR(20) NOT NULL,
    LengthOfStayHours AS (CASE WHEN DischargeTime IS NULL THEN NULL ELSE DATEDIFF(HOUR, AdmissionTime, DischargeTime) END),
    CONSTRAINT PK_PatientAdmission PRIMARY KEY (AdmissionID),
    CONSTRAINT FK_PatientAdmission_Patient FOREIGN KEY (PatientID) REFERENCES Patient(PatientID),
    CONSTRAINT FK_PatientAdmission_Hospital FOREIGN KEY (HospitalID) REFERENCES Hospital(HospitalID),
    CONSTRAINT FK_PatientAdmission_Report FOREIGN KEY (ReportID) REFERENCES EmergencyReport(ReportID),
    CONSTRAINT CK_PatientAdmission_Condition CHECK ([Condition] IN ('Critical','Serious','Stable')),
    CONSTRAINT CK_PatientAdmission_Status CHECK ([Status] IN ('Admitted','Discharged','Transferred')),
    CONSTRAINT CK_PatientAdmission_Time CHECK (DischargeTime IS NULL OR DischargeTime >= AdmissionTime)
);
GO

CREATE TABLE ApprovalRequest (
    RequestID INT IDENTITY(1,1) NOT NULL,
    RequestedBy INT NOT NULL,
    ReviewedBy INT NULL,
    RequestType VARCHAR(30) NOT NULL,
    RequestTime DATETIME2 NOT NULL,
    [Status] VARCHAR(20) NOT NULL,
    [Description] VARCHAR(2000) NULL,
    AllocationID INT NULL,
    AssignmentID INT NULL,
    ExpenseID INT NULL,
    CONSTRAINT PK_ApprovalRequest PRIMARY KEY (RequestID),
    CONSTRAINT FK_ApprovalRequest_RequestedBy FOREIGN KEY (RequestedBy) REFERENCES [User](UserID),
    CONSTRAINT FK_ApprovalRequest_ReviewedBy FOREIGN KEY (ReviewedBy) REFERENCES [User](UserID),
    CONSTRAINT FK_ApprovalRequest_Allocation FOREIGN KEY (AllocationID) REFERENCES ResourceAllocation(AllocationID),
    CONSTRAINT FK_ApprovalRequest_Assignment FOREIGN KEY (AssignmentID) REFERENCES TeamAssignment(AssignmentID),
    CONSTRAINT FK_ApprovalRequest_Expense FOREIGN KEY (ExpenseID) REFERENCES Expense(ExpenseID),
    CONSTRAINT CK_ApprovalRequest_RequestType CHECK (RequestType IN ('ResourceDistribution','RescueDeployment','Financial')),
    CONSTRAINT CK_ApprovalRequest_Status CHECK ([Status] IN ('Pending','Approved','Rejected')),
    CONSTRAINT CK_ApprovalRequest_OneTarget CHECK (
        (CASE WHEN AllocationID IS NOT NULL THEN 1 ELSE 0 END) +
        (CASE WHEN AssignmentID IS NOT NULL THEN 1 ELSE 0 END) +
        (CASE WHEN ExpenseID IS NOT NULL THEN 1 ELSE 0 END) = 1
    )
);
GO

CREATE TABLE ApprovalHistory (
    RequestID INT NOT NULL,
    HistoryID INT NOT NULL,
    ActionBy INT NOT NULL,
    ActionTime DATETIME2 NOT NULL,
    Decision VARCHAR(20) NOT NULL,
    Comments VARCHAR(2000) NULL,
    CONSTRAINT PK_ApprovalHistory PRIMARY KEY (RequestID, HistoryID),
    CONSTRAINT FK_ApprovalHistory_Request FOREIGN KEY (RequestID) REFERENCES ApprovalRequest(RequestID),
    CONSTRAINT FK_ApprovalHistory_ActionBy FOREIGN KEY (ActionBy) REFERENCES [User](UserID),
    CONSTRAINT CK_ApprovalHistory_Decision CHECK (Decision IN ('Approved','Rejected','Escalated'))
);
GO

CREATE TABLE AuditLog (
    LogID BIGINT IDENTITY(1,1) NOT NULL,
    UserID INT NULL,
    [Action] VARCHAR(120) NOT NULL,
    TableName VARCHAR(120) NOT NULL,
    RecordID VARCHAR(120) NOT NULL,
    OldValue VARCHAR(MAX) NULL,
    NewValue VARCHAR(MAX) NULL,
    [Timestamp] DATETIME2 NOT NULL CONSTRAINT DF_AuditLog_Timestamp DEFAULT SYSUTCDATETIME(),
    IPAddress VARCHAR(64) NULL,
    CONSTRAINT PK_AuditLog PRIMARY KEY (LogID),
    CONSTRAINT FK_AuditLog_User FOREIGN KEY (UserID) REFERENCES [User](UserID) ON DELETE SET NULL
);
GO

CREATE TABLE UserRole (
    UserID INT NOT NULL,
    RoleID INT NOT NULL,
    AssignedAt DATETIME2 NOT NULL CONSTRAINT DF_UserRole_AssignedAt DEFAULT SYSUTCDATETIME(),
    AssignedBy INT NULL,
    CONSTRAINT PK_UserRole PRIMARY KEY (UserID, RoleID),
    CONSTRAINT FK_UserRole_User FOREIGN KEY (UserID) REFERENCES [User](UserID),
    CONSTRAINT FK_UserRole_Role FOREIGN KEY (RoleID) REFERENCES [Role](RoleID),
    CONSTRAINT FK_UserRole_AssignedBy FOREIGN KEY (AssignedBy) REFERENCES [User](UserID)
);
GO

CREATE TABLE RolePermission (
    RoleID INT NOT NULL,
    PermissionID INT NOT NULL,
    CONSTRAINT PK_RolePermission PRIMARY KEY (RoleID, PermissionID),
    CONSTRAINT FK_RolePermission_Role FOREIGN KEY (RoleID) REFERENCES [Role](RoleID),
    CONSTRAINT FK_RolePermission_Permission FOREIGN KEY (PermissionID) REFERENCES Permission(PermissionID)
);
GO

CREATE TABLE UserPhone (
    UserID INT NOT NULL,
    Phone VARCHAR(30) NOT NULL,
    CONSTRAINT PK_UserPhone PRIMARY KEY (UserID, Phone),
    CONSTRAINT FK_UserPhone_User FOREIGN KEY (UserID) REFERENCES [User](UserID)
);
GO

CREATE TABLE CitizenPhone (
    CitizenID INT NOT NULL,
    Phone VARCHAR(30) NOT NULL,
    CONSTRAINT PK_CitizenPhone PRIMARY KEY (CitizenID, Phone),
    CONSTRAINT FK_CitizenPhone_Citizen FOREIGN KEY (CitizenID) REFERENCES Citizen(CitizenID)
);
GO

CREATE TABLE DonorPhone (
    DonorID INT NOT NULL,
    Phone VARCHAR(30) NOT NULL,
    CONSTRAINT PK_DonorPhone PRIMARY KEY (DonorID, Phone),
    CONSTRAINT FK_DonorPhone_Donor FOREIGN KEY (DonorID) REFERENCES Donor(DonorID)
);
GO

CREATE TABLE RescueTeamSpecialization (
    TeamID INT NOT NULL,
    Specialization VARCHAR(100) NOT NULL,
    CONSTRAINT PK_RescueTeamSpecialization PRIMARY KEY (TeamID, Specialization),
    CONSTRAINT FK_RescueTeamSpecialization_Team FOREIGN KEY (TeamID) REFERENCES RescueTeam(TeamID)
);
GO

CREATE TABLE HospitalSpecialization (
    HospitalID INT NOT NULL,
    Specialization VARCHAR(100) NOT NULL,
    CONSTRAINT PK_HospitalSpecialization PRIMARY KEY (HospitalID, Specialization),
    CONSTRAINT FK_HospitalSpecialization_Hospital FOREIGN KEY (HospitalID) REFERENCES Hospital(HospitalID)
);
GO




