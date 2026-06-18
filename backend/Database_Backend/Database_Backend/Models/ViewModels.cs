using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

/// <summary>
/// View Models for Database Views Integration
/// These classes map to database views in Final_DB for read-only queries.
/// </summary>

// ============================================================================
// INVENTORY & RESOURCE VIEW MODELS
// ============================================================================

/// <summary>
/// Maps to vw_Inventory_Current
/// </summary>
public class VwInventoryCurrent
{
    public int InventoryId { get; set; }
    public int WarehouseId { get; set; }
    public int ResourceId { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
    public decimal MaxCapacity { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Maps to vw_Inventory_Alerts
/// </summary>
public class VwInventoryAlerts
{
    public int InventoryId { get; set; }
    public int AlertId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public DateTime AlertTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ResolvedAt { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
}

/// <summary>
/// Maps to vw_ResourceAllocation_Status
/// </summary>
public class VwResourceAllocationStatus
{
    public int AllocationId { get; set; }
    public int InventoryId { get; set; }
    public int EventId { get; set; }
    public decimal Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RequestedBy { get; set; }
    public DateTime RequestTime { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}

// ============================================================================
// EMERGENCY REPORT VIEW MODELS
// ============================================================================

/// <summary>
/// Maps to vw_EmergencyReports_Pending
/// </summary>
public class VwEmergencyReportsPending
{
    public int ReportId { get; set; }
    public int CitizenId { get; set; }
    public int? EventId { get; set; }
    public string DisasterType { get; set; } = string.Empty;
    public string SeverityLevel { get; set; } = string.Empty;
    public DateTime ReportTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

/// <summary>
/// Maps to vw_EmergencyReports_ByEvent
/// </summary>
public class VwEmergencyReportsByEvent
{
    public int ReportId { get; set; }
    public int? EventId { get; set; }
    public string? EventName { get; set; }
    public string DisasterType { get; set; } = string.Empty;
    public string SeverityLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ReportTime { get; set; }
}

// ============================================================================
// TEAM & ASSIGNMENT VIEW MODELS
// ============================================================================

/// <summary>
/// Maps to vw_Teams_Availability
/// </summary>
public class VwTeamsAvailability
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string TeamType { get; set; } = string.Empty;
    public string AvailabilityStatus { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int TotalAssignments { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

/// <summary>
/// Maps to vw_Assignments_Detail
/// </summary>
public class VwAssignmentsDetail
{
    public int AssignmentId { get; set; }
    public int TeamId { get; set; }
    public int ReportId { get; set; }
    public string ReportLocation { get; set; } = string.Empty;
    public int AssignedBy { get; set; }
    public DateTime AssignmentTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Maps to vw_TeamActivity_Log
/// </summary>
public class VwTeamActivityLog
{
    public int TeamId { get; set; }
    public int ActivityId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public string? Outcome { get; set; }
}

// ============================================================================
// APPROVAL & WORKFLOW VIEW MODELS
// ============================================================================

/// <summary>
/// Maps to vw_Pending_Approvals
/// </summary>
public class VwPendingApprovals
{
    public int RequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public int RequestedBy { get; set; }
    public DateTime RequestTime { get; set; }
    public int? AllocationId { get; set; }
    public int? AssignmentId { get; set; }
    public int? ExpenseId { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Maps to vw_Approval_History
/// </summary>
public class VwApprovalHistory
{
    public int RequestId { get; set; }
    public int HistoryId { get; set; }
    public int ActionBy { get; set; }
    public DateTime ActionTime { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

// ============================================================================
// HOSPITAL & PATIENT VIEW MODELS
// ============================================================================

/// <summary>
/// Maps to vw_Hospital_Capacity
/// </summary>
public class VwHospitalCapacity
{
    public int HospitalId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public int TotalBeds { get; set; }
    public int AvailableBeds { get; set; }
    public decimal? OccupancyRate { get; set; }
}

/// <summary>
/// Maps to vw_Patient_Admissions
/// </summary>
public class VwPatientAdmissions
{
    public int AdmissionId { get; set; }
    public int PatientId { get; set; }
    public int HospitalId { get; set; }
    public DateTime AdmissionTime { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ReportId { get; set; }
}

// ============================================================================
// FINANCIAL VIEW MODELS
// ============================================================================

/// <summary>
/// Maps to vw_Donations_Summary
/// </summary>
public class VwDonationsSummary
{
    public int DonationId { get; set; }
    public int DonorId { get; set; }
    public int EventId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DonationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Maps to vw_Expenses_Summary
/// </summary>
public class VwExpensesSummary
{
    public int ExpenseId { get; set; }
    public int EventId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public int? ApprovedBy { get; set; }
}

/// <summary>
/// Maps to vw_Budget_PerEvent
/// </summary>
public class VwBudgetPerEvent
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public decimal TotalDonations { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetBudget { get; set; }
}

// ============================================================================
// REPORTING & DASHBOARD VIEW MODELS
// ============================================================================

/// <summary>
/// Maps to vw_Event_Overview
/// </summary>
public class VwEventOverview
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AffectedPopulation { get; set; }
    public int IncidentCount { get; set; }
    public int TotalAllocations { get; set; }
    public decimal TotalDonations { get; set; }
    public decimal TotalExpenses { get; set; }
}

/// <summary>
/// Maps to vw_Response_Performance
/// </summary>
public class VwResponsePerformance
{
    public int EventId { get; set; }
    public decimal AvgResponseTime { get; set; }
    public decimal AvgTeamCompletionTime { get; set; }
    public decimal ResourceUtilizationPercent { get; set; }
}

// ============================================================================
// SECURITY & RBAC VIEW MODELS
// ============================================================================

/// <summary>
/// Maps to vw_User_Roles_Permissions
/// Note: Does NOT include PasswordHash or Email for security
/// </summary>
public class VwUserRolesPermissions
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

// ============================================================================
// AUDIT & MONITORING VIEW MODELS
// ============================================================================

/// <summary>
/// Maps to vw_Audit_Recent
/// Includes TOP 1000 most recent audit entries
/// </summary>
public class VwAuditRecent
{
    public long LogId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

/// <summary>
/// Maps to vw_FinancialAuditTrail
/// Financial-only audit entries for Donation, Expense, ApprovalRequest
/// </summary>
public class VwFinancialAuditTrail
{
    public long LogId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
