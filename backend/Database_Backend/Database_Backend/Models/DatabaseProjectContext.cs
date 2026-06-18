using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Database_Backend.Models;

public partial class DatabaseProjectContext : DbContext
{
    public DatabaseProjectContext()
    {
    }

    public DatabaseProjectContext(DbContextOptions<DatabaseProjectContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApprovalHistory> ApprovalHistories { get; set; }

    public virtual DbSet<ApprovalRequest> ApprovalRequests { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Citizen> Citizens { get; set; }

    public virtual DbSet<CitizenPhone> CitizenPhones { get; set; }

    public virtual DbSet<DisasterEvent> DisasterEvents { get; set; }

    public virtual DbSet<Donation> Donations { get; set; }

    public virtual DbSet<Donor> Donors { get; set; }

    public virtual DbSet<DonorPhone> DonorPhones { get; set; }

    public virtual DbSet<EmergencyReport> EmergencyReports { get; set; }

    public virtual DbSet<Expense> Expenses { get; set; }

    public virtual DbSet<Hospital> Hospitals { get; set; }

    public virtual DbSet<HospitalSpecialization> HospitalSpecializations { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<InventoryAlert> InventoryAlerts { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PatientAdmission> PatientAdmissions { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<RescueTeam> RescueTeams { get; set; }

    public virtual DbSet<RescueTeamSpecialization> RescueTeamSpecializations { get; set; }

    public virtual DbSet<Resource> Resources { get; set; }

    public virtual DbSet<ResourceAllocation> ResourceAllocations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TeamActivity> TeamActivities { get; set; }

    public virtual DbSet<TeamAssignment> TeamAssignments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserPhone> UserPhones { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    // ============================================================================
    // DATABASE VIEWS - READ-ONLY QUERIES
    // ============================================================================

    public virtual DbSet<VwInventoryCurrent> VwInventoryCurrent { get; set; }

    public virtual DbSet<VwInventoryAlerts> VwInventoryAlerts { get; set; }

    public virtual DbSet<VwResourceAllocationStatus> VwResourceAllocationStatus { get; set; }

    public virtual DbSet<VwEmergencyReportsPending> VwEmergencyReportsPending { get; set; }

    public virtual DbSet<VwEmergencyReportsByEvent> VwEmergencyReportsByEvent { get; set; }

    public virtual DbSet<VwTeamsAvailability> VwTeamsAvailability { get; set; }

    public virtual DbSet<VwAssignmentsDetail> VwAssignmentsDetail { get; set; }

    public virtual DbSet<VwTeamActivityLog> VwTeamActivityLog { get; set; }

    public virtual DbSet<VwPendingApprovals> VwPendingApprovals { get; set; }

    public virtual DbSet<VwApprovalHistory> VwApprovalHistory { get; set; }

    public virtual DbSet<VwHospitalCapacity> VwHospitalCapacity { get; set; }

    public virtual DbSet<VwPatientAdmissions> VwPatientAdmissions { get; set; }

    public virtual DbSet<VwDonationsSummary> VwDonationsSummary { get; set; }

    public virtual DbSet<VwExpensesSummary> VwExpensesSummary { get; set; }

    public virtual DbSet<VwBudgetPerEvent> VwBudgetPerEvent { get; set; }

    public virtual DbSet<VwEventOverview> VwEventOverview { get; set; }

    public virtual DbSet<VwResponsePerformance> VwResponsePerformance { get; set; }

    public virtual DbSet<VwUserRolesPermissions> VwUserRolesPermissions { get; set; }

    public virtual DbSet<VwAuditRecent> VwAuditRecent { get; set; }

    public virtual DbSet<VwFinancialAuditTrail> VwFinancialAuditTrail { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalHistory>(entity =>
        {
            entity.HasKey(e => new { e.RequestId, e.HistoryId });

            entity.ToTable("ApprovalHistory");

            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
            entity.Property(e => e.Comments)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.Decision)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ActionByNavigation).WithMany(p => p.ApprovalHistories)
                .HasForeignKey(d => d.ActionBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovalHistory_ActionBy");

            entity.HasOne(d => d.Request).WithMany(p => p.ApprovalHistories)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovalHistory_Request");
        });

        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.ToTable("ApprovalRequest", tb => tb.HasTrigger("trg_ApprovalRequest_WriteHistory"));

            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.AllocationId).HasColumnName("AllocationID");
            entity.Property(e => e.AssignmentId).HasColumnName("AssignmentID");
            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.ExpenseId).HasColumnName("ExpenseID");
            entity.Property(e => e.RequestType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Allocation).WithMany(p => p.ApprovalRequests)
                .HasForeignKey(d => d.AllocationId)
                .HasConstraintName("FK_ApprovalRequest_Allocation");

            entity.HasOne(d => d.Assignment).WithMany(p => p.ApprovalRequests)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("FK_ApprovalRequest_Assignment");

            entity.HasOne(d => d.Expense).WithMany(p => p.ApprovalRequests)
                .HasForeignKey(d => d.ExpenseId)
                .HasConstraintName("FK_ApprovalRequest_Expense");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.ApprovalRequestRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovalRequest_RequestedBy");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.ApprovalRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_ApprovalRequest_ReviewedBy");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.ToTable("AuditLog", tb => tb.HasTrigger("trg_AuditLog_LoginUpdate"));

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.Action)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("IPAddress");
            entity.Property(e => e.NewValue).IsUnicode(false);
            entity.Property(e => e.OldValue).IsUnicode(false);
            entity.Property(e => e.RecordId)
                .HasMaxLength(120)
                .IsUnicode(false)
                .HasColumnName("RecordID");
            entity.Property(e => e.TableName)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AuditLog_User");
        });

        modelBuilder.Entity<Citizen>(entity =>
        {
            entity.ToTable("Citizen");

            entity.HasIndex(e => e.Email, "UQ_Citizen_Email").IsUnique();

            entity.HasIndex(e => e.NationalId, "UQ_Citizen_NationalID").IsUnique();

            entity.Property(e => e.CitizenId).HasColumnName("CitizenID");
            entity.Property(e => e.Area)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NationalId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NationalID");
            entity.Property(e => e.Province)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.Street)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<CitizenPhone>(entity =>
        {
            entity.HasKey(e => new { e.CitizenId, e.Phone });

            entity.ToTable("CitizenPhone");

            entity.Property(e => e.CitizenId).HasColumnName("CitizenID");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.Citizen).WithMany(p => p.CitizenPhones)
                .HasForeignKey(d => d.CitizenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CitizenPhone_Citizen");
        });

        modelBuilder.Entity<DisasterEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);

            entity.ToTable("DisasterEvent");

            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.Area)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.DisasterType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DurationMinutes).HasComputedColumnSql("(case when [EndTime] IS NULL then NULL else datediff(minute,[StartTime],[EndTime]) end)", false);
            entity.Property(e => e.EventName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Province)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Street)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Donation>(entity =>
        {
            entity.ToTable("Donation", tb => tb.HasTrigger("trg_Donation_Audit"));

            entity.HasIndex(e => e.ReceiptNumber, "UQ_Donation_ReceiptNumber").IsUnique();

            entity.Property(e => e.DonationId).HasColumnName("DonationID");
            entity.Property(e => e.Amount).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.DonorId).HasColumnName("DonorID");
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ReceiptNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Donor).WithMany(p => p.Donations)
                .HasForeignKey(d => d.DonorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Donation_Donor");

            entity.HasOne(d => d.Event).WithMany(p => p.Donations)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Donation_Event");
        });

        modelBuilder.Entity<Donor>(entity =>
        {
            entity.ToTable("Donor");

            entity.HasIndex(e => e.Email, "UQ_Donor_Email").IsUnique();

            entity.Property(e => e.DonorId).HasColumnName("DonorID");
            entity.Property(e => e.Area)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.DonorType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OrganizationName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Province)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.Street)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DonorPhone>(entity =>
        {
            entity.HasKey(e => new { e.DonorId, e.Phone });

            entity.ToTable("DonorPhone");

            entity.Property(e => e.DonorId).HasColumnName("DonorID");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.Donor).WithMany(p => p.DonorPhones)
                .HasForeignKey(d => d.DonorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DonorPhone_Donor");
        });

        modelBuilder.Entity<EmergencyReport>(entity =>
        {
            entity.HasKey(e => e.ReportId);

            entity.ToTable("EmergencyReport", tb => tb.HasTrigger("trg_EmergencyReport_MaintainCounts"));

            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.Area)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.CitizenId).HasColumnName("CitizenID");
            entity.Property(e => e.City)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.DisasterType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Province)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.SeverityLevel)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Source)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Street)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.Citizen).WithMany(p => p.EmergencyReports)
                .HasForeignKey(d => d.CitizenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmergencyReport_Citizen");

            entity.HasOne(d => d.Event).WithMany(p => p.EmergencyReports)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK_EmergencyReport_DisasterEvent");
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("Expense", tb => tb.HasTrigger("trg_Expense_Audit"));

            entity.Property(e => e.ExpenseId).HasColumnName("ExpenseID");
            entity.Property(e => e.Amount).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.Category)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_Expense_ApprovedBy");

            entity.HasOne(d => d.Event).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Expense_Event");
        });

        modelBuilder.Entity<Hospital>(entity =>
        {
            entity.ToTable("Hospital");

            entity.Property(e => e.HospitalId).HasColumnName("HospitalID");
            entity.Property(e => e.Area)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.ContactEmail)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.HospitalName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.OccupancyRate)
                .HasComputedColumnSql("(case when [TotalBeds]=(0) then NULL else (([TotalBeds]-[AvailableBeds])*(100.0))/[TotalBeds] end)", false)
                .HasColumnType("numeric(26, 12)");
            entity.Property(e => e.Province)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.Street)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<HospitalSpecialization>(entity =>
        {
            entity.HasKey(e => new { e.HospitalId, e.Specialization });

            entity.ToTable("HospitalSpecialization");

            entity.Property(e => e.HospitalId).HasColumnName("HospitalID");
            entity.Property(e => e.Specialization)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Hospital).WithMany(p => p.HospitalSpecializations)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HospitalSpecialization_Hospital");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("Inventory", tb => tb.HasTrigger("trg_Inventory_ManageAlerts"));

            entity.HasIndex(e => new { e.WarehouseId, e.ResourceId }, "UQ_Inventory_Warehouse_Resource").IsUnique();

            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MaxCapacity).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.MinThreshold).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.ResourceId).HasColumnName("ResourceID");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.Resource).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.ResourceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Resource");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Warehouse");
        });

        modelBuilder.Entity<InventoryAlert>(entity =>
        {
            entity.HasKey(e => new { e.InventoryId, e.AlertId });

            entity.ToTable("InventoryAlert");

            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.AlertId).HasColumnName("AlertID");
            entity.Property(e => e.AlertTime).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.AlertType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Inventory).WithMany(p => p.InventoryAlerts)
                .HasForeignKey(d => d.InventoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryAlert_Inventory");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patient");

            entity.HasIndex(e => e.NationalId, "UQ_Patient_NationalID").IsUnique();

            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.BloodType)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Gender)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NationalId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NationalID");
        });

        modelBuilder.Entity<PatientAdmission>(entity =>
        {
            entity.HasKey(e => e.AdmissionId);

            entity.ToTable("PatientAdmission", tb => tb.HasTrigger("trg_PatientAdmission_UpdateBeds"));

            entity.Property(e => e.AdmissionId).HasColumnName("AdmissionID");
            entity.Property(e => e.Condition)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.HospitalId).HasColumnName("HospitalID");
            entity.Property(e => e.LengthOfStayHours).HasComputedColumnSql("(case when [DischargeTime] IS NULL then NULL else datediff(hour,[AdmissionTime],[DischargeTime]) end)", false);
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Hospital).WithMany(p => p.PatientAdmissions)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatientAdmission_Hospital");

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientAdmissions)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatientAdmission_Patient");

            entity.HasOne(d => d.Report).WithMany(p => p.PatientAdmissions)
                .HasForeignKey(d => d.ReportId)
                .HasConstraintName("FK_PatientAdmission_Report");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permission");

            entity.HasIndex(e => new { e.Module, e.Action, e.PermissionName }, "UQ_Permission_Module_Action_Name").IsUnique();

            entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
            entity.Property(e => e.Action)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Module)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PermissionName)
                .HasMaxLength(120)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RescueTeam>(entity =>
        {
            entity.HasKey(e => e.TeamId);

            entity.ToTable("RescueTeam");

            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.Area)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.AvailabilityStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Province)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.Street)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.TeamName)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.TeamType)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RescueTeamSpecialization>(entity =>
        {
            entity.HasKey(e => new { e.TeamId, e.Specialization });

            entity.ToTable("RescueTeamSpecialization");

            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.Specialization)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Team).WithMany(p => p.RescueTeamSpecializations)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RescueTeamSpecialization_Team");
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.ToTable("Resource");

            entity.Property(e => e.ResourceId).HasColumnName("ResourceID");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.ResourceName)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.ResourceType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Unit)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ResourceAllocation>(entity =>
        {
            entity.HasKey(e => e.AllocationId);

            entity.ToTable("ResourceAllocation", tb => tb.HasTrigger("trg_ResourceAllocation_UpdateInventory"));

            entity.Property(e => e.AllocationId).HasColumnName("AllocationID");
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.Quantity).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Event).WithMany(p => p.ResourceAllocations)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ResourceAllocation_Event");

            entity.HasOne(d => d.Inventory).WithMany(p => p.ResourceAllocations)
                .HasForeignKey(d => d.InventoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ResourceAllocation_Inventory");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.ResourceAllocations)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ResourceAllocation_RequestedBy");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UQ_Role_RoleName").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RoleName)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolePermission_Permission"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolePermission_Role"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId");
                        j.ToTable("RolePermission");
                        j.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
                        j.IndexerProperty<int>("PermissionId").HasColumnName("PermissionID");
                    });
        });

        modelBuilder.Entity<TeamActivity>(entity =>
        {
            entity.HasKey(e => new { e.TeamId, e.ActivityId });

            entity.ToTable("TeamActivity", tb => tb.HasTrigger("trg_TeamActivity_SyncTeamState"));

            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.ActivityId).HasColumnName("ActivityID");
            entity.Property(e => e.ActivityType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DurationMinutes).HasComputedColumnSql("(case when [EndTime] IS NULL then NULL else datediff(minute,[StartTime],[EndTime]) end)", false);
            entity.Property(e => e.Notes)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.Outcome)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.Team).WithMany(p => p.TeamActivities)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamActivity_Team");
        });

        modelBuilder.Entity<TeamAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId);

            entity.ToTable("TeamAssignment", tb => tb.HasTrigger("trg_TeamAssignment_SyncTeamState"));

            entity.Property(e => e.AssignmentId).HasColumnName("AssignmentID");
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TeamId).HasColumnName("TeamID");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.TeamAssignments)
                .HasForeignKey(d => d.AssignedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamAssignment_AssignedBy");

            entity.HasOne(d => d.Event).WithMany(p => p.TeamAssignments)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamAssignment_Event");

            entity.HasOne(d => d.Team).WithMany(p => p.TeamAssignments)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamAssignment_Team");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ_User_Email").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_User_Username").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UserPhone>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.Phone });

            entity.ToTable("UserPhone");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.UserPhones)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserPhone_User");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.ToTable("UserRole");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.UserRoleAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("FK_UserRole_AssignedBy");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRole_Role");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoleUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRole_User");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("Warehouse");

            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.Area)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.ContactEmail)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.ManagerId).HasColumnName("ManagerID");
            entity.Property(e => e.Province)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.Street)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseName)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.Manager).WithMany(p => p.Warehouses)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Warehouse_Manager");
        });

        OnModelCreatingPartial(modelBuilder);

        // ============================================================================
        // CONFIGURE DATABASE VIEWS - READ-ONLY ENTITIES
        // ============================================================================

        // Inventory & Resource Views
        modelBuilder.Entity<VwInventoryCurrent>().HasNoKey().ToView("vw_Inventory_Current");
        modelBuilder.Entity<VwInventoryAlerts>().HasNoKey().ToView("vw_Inventory_Alerts");
        modelBuilder.Entity<VwResourceAllocationStatus>().HasNoKey().ToView("vw_ResourceAllocation_Status");

        // Emergency Report Views
        modelBuilder.Entity<VwEmergencyReportsPending>().HasNoKey().ToView("vw_EmergencyReports_Pending");
        modelBuilder.Entity<VwEmergencyReportsByEvent>().HasNoKey().ToView("vw_EmergencyReports_ByEvent");

        // Team & Assignment Views
        modelBuilder.Entity<VwTeamsAvailability>().HasNoKey().ToView("vw_Teams_Availability");
        modelBuilder.Entity<VwAssignmentsDetail>().HasNoKey().ToView("vw_Assignments_Detail");
        modelBuilder.Entity<VwTeamActivityLog>().HasNoKey().ToView("vw_TeamActivity_Log");

        // Approval & Workflow Views
        modelBuilder.Entity<VwPendingApprovals>().HasNoKey().ToView("vw_Pending_Approvals");
        modelBuilder.Entity<VwApprovalHistory>().HasNoKey().ToView("vw_Approval_History");

        // Hospital & Patient Views
        modelBuilder.Entity<VwHospitalCapacity>().HasNoKey().ToView("vw_Hospital_Capacity");
        modelBuilder.Entity<VwPatientAdmissions>().HasNoKey().ToView("vw_Patient_Admissions");

        // Financial Views
        modelBuilder.Entity<VwDonationsSummary>().HasNoKey().ToView("vw_Donations_Summary");
        modelBuilder.Entity<VwExpensesSummary>().HasNoKey().ToView("vw_Expenses_Summary");
        modelBuilder.Entity<VwBudgetPerEvent>().HasNoKey().ToView("vw_Budget_PerEvent");

        // Reporting & Dashboard Views
        modelBuilder.Entity<VwEventOverview>().HasNoKey().ToView("vw_Event_Overview");
        modelBuilder.Entity<VwResponsePerformance>().HasNoKey().ToView("vw_Response_Performance");

        // Security & RBAC Views
        modelBuilder.Entity<VwUserRolesPermissions>().HasNoKey().ToView("vw_User_Roles_Permissions");

        // Audit & Monitoring Views
        modelBuilder.Entity<VwAuditRecent>().HasNoKey().ToView("vw_Audit_Recent");
        modelBuilder.Entity<VwFinancialAuditTrail>().HasNoKey().ToView("vw_FinancialAuditTrail");
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
