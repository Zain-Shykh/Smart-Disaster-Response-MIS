using System;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Models;

/// <summary>
/// Shared DTOs used across multiple controllers for:
/// - Stored Procedure Results
/// - View Query Responses  
/// - API Request/Response contracts
/// </summary>

// ============================================================================
// APPROVAL WORKFLOW DTOs
// ============================================================================

public class ApprovalDecisionDto
{
    [Range(1, int.MaxValue)]
    public int ActionBy { get; set; }

    [Range(1, int.MaxValue)]
    public int? ReviewedBy { get; set; }

    [Range(1, int.MaxValue)]
    public int? RequestID { get; set; }

    [MaxLength(500)]
    public string? Comments { get; set; }

    [MaxLength(30)]
    public string? Decision { get; set; }

    public string? VersionToken { get; set; }
}

public class PendingApprovalsDto
{
    public int RequestId { get; set; }
    public int RequestID { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public int RequestedBy { get; set; }
    public int? AllocationId { get; set; }
    public int? AssignmentId { get; set; }
    public int? ExpenseId { get; set; }
    public string? Description { get; set; }
    public int EventID { get; set; }
    public DateTime RequestTime { get; set; }
    public int SubmittedBy { get; set; }
    public string SubmitterName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class PendingApprovalsHistoryDto
{
    public int RequestId { get; set; }
    public int HistoryId { get; set; }
    public int ActionBy { get; set; }
    public DateTime ActionTime { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public int RequestID { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ApprovalTime { get; set; }
    public int ReviewedBy { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
}

// ============================================================================
// RESOURCE ALLOCATION DTOs
// ============================================================================

public class InventoryAlertDto
{
    public int InventoryId { get; set; }
    public int AlertId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public DateTime AlertTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ResolvedAt { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
}

public class ResourceAllocationStatusDto
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
// RESCUE TEAM DTOs
// ============================================================================

public class TeamAvailabilityDto
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

public class AssignmentDetailDto
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

public class TeamActivityLogDto
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
// DONATION & FINANCE DTOs
// ============================================================================

public class DonationsSummaryDto
{
    public int DonationId { get; set; }
    public int DonorId { get; set; }
    public int EventId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DonationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ExpensesSummaryDto
{
    public int ExpenseId { get; set; }
    public int EventId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public int? ApprovedBy { get; set; }
}

public class BudgetPerEventDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public decimal TotalDonations { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetBudget { get; set; }
}

// ============================================================================
// HOSPITAL & PATIENT DTOs
// ============================================================================

public class HospitalCapacityDto
{
    public int HospitalId { get; set; }
    public int HospitalID { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
    public int AvailableBeds { get; set; }
    public decimal CapacityPercentage { get; set; }
    public decimal? OccupancyRate { get; set; }
}

public class PatientAdmissionsViewDto
{
    public int AdmissionId { get; set; }
    public int PatientId { get; set; }
    public int HospitalId { get; set; }
    public string Condition { get; set; } = string.Empty;
    public int? ReportId { get; set; }
    public int PatientID { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int HospitalID { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public DateTime AdmissionTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
}

// ============================================================================
// INVENTORY DTOs
// ============================================================================

public class InventoryCurrentDto
{
    public int InventoryId { get; set; }
    public int WarehouseId { get; set; }
    public int ResourceId { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
    public decimal MaxCapacity { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class InventoryMovementDto
{
    public int AllocationId { get; set; }
    public int InventoryId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime MovementTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class WarehouseInventoryHistoryDto
{
    public int InventoryId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public int TotalAllocations { get; set; }
    public decimal TotalRequestedQuantity { get; set; }
    public decimal TotalDispatchedQuantity { get; set; }
    public decimal TotalConsumedQuantity { get; set; }
}

// ============================================================================
// REPORTS DTOs
// ============================================================================

public class EventOverviewDto
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int TotalResponsePersonnel { get; set; }
    public int TotalResourcesDeployed { get; set; }
}

public class ResponsePerformanceDto
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public TimeSpan AverageResponseTime { get; set; }
    public int ResourcesAllocated { get; set; }
    public int TeamsDeployed { get; set; }
    public int PatientsAssisted { get; set; }
}

public class AuditRecentDto
{
    public int AuditID { get; set; }
    public DateTime AuditTime { get; set; }
    public string Action { get; set; } = string.Empty;
    public int PerformedBy { get; set; }
    public string PerformedByName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public class FinancialAuditTrailDto
{
    public int AuditID { get; set; }
    public DateTime AuditTime { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int PerformedBy { get; set; }
    public string PerformedByName { get; set; } = string.Empty;
}

// ============================================================================
// EMERGENCY REPORT DTOs
// ============================================================================

public class EmergencyReportPendingDto
{
    public int ReportID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime ReportTime { get; set; }
    public int SubmittedBy { get; set; }
    public string SubmitterName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class EmergencyReportByEventDto
{
    public int ReportID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime ReportTime { get; set; }
    public string Details { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

// ============================================================================
// USER DTOs
// ============================================================================

public class UserRolesPermissionsDto
{
    public int UserID { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public string Permissions { get; set; } = string.Empty;
}

// ============================================================================
// STORED PROCEDURE REQUEST DTOs
// ============================================================================

public class AllocationApprovalSpRequestDto
{
    [Range(1, int.MaxValue)]
    public int AllocationID { get; set; }

    [Range(1, int.MaxValue)]
    public int ReviewedBy { get; set; }

    [MaxLength(500)]
    public string? Comments { get; set; }
}

public class ResourceDispatchSpRequestDto
{
    [Range(1, int.MaxValue)]
    public int AllocationID { get; set; }

    [Range(1, int.MaxValue)]
    public int DispatchedBy { get; set; }

    [Range(1, int.MaxValue)]
    public int? DispatchedByUserID { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class TeamAssignmentSpRequestDto
{
    [Range(1, int.MaxValue)]
    public int? ReportID { get; set; }

    [Range(1, int.MaxValue)]
    public int TeamID { get; set; }

    [Range(1, int.MaxValue)]
    public int EventID { get; set; }

    [Range(1, int.MaxValue)]
    public int AssignedBy { get; set; }

    [MaxLength(1000)]
    public string? SpecialInstructions { get; set; }
}

public class DeploymentApprovalSpRequestDto
{
    [Range(1, int.MaxValue)]
    public int AssignmentID { get; set; }

    [Range(1, int.MaxValue)]
    public int ApprovedBy { get; set; }

    [MaxLength(500)]
    public string? Comments { get; set; }
}

public class PatientAdmitRequestDto
{
    [Range(1, int.MaxValue)]
    public int? HospitalId { get; set; }

    [Range(1, int.MaxValue)]
    public int? PatientId { get; set; }

    [Range(1, int.MaxValue)]
    public int PatientID { get; set; }

    [Range(1, int.MaxValue)]
    public int HospitalID { get; set; }

    [Range(1, int.MaxValue)]
    public int AdmittedBy { get; set; }

    [Required]
    [MaxLength(500)]
    public string Diagnosis { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class PatientDischargeRequestDto
{
    [Range(1, int.MaxValue)]
    public int? PatientId { get; set; }

    [Range(1, int.MaxValue)]
    public int PatientID { get; set; }

    [Range(1, int.MaxValue)]
    public int DischargedBy { get; set; }

    [MaxLength(500)]
    public string? DischargeNotes { get; set; }
}

public class CompletionSpRequestDto
{
    [Range(1, int.MaxValue)]
    public int CompletedBy { get; set; }

    [Range(1, int.MaxValue)]
    public int DurationMinutes { get; set; }

    [MaxLength(2000)]
    public string? Summary { get; set; }
}

public class InventoryUpdateSpRequestDto
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int UpdatedBy { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

// ============================================================================
// STORED PROCEDURE RESULT DTOs
// ============================================================================

public class ApprovalResult
{
    public string ResultStatus { get; set; } = string.Empty;
    public int RequestID { get; set; }
    public DateTime? ApprovalTime { get; set; }
}

public class AssignmentCompletionResult
{
    public string ResultStatus { get; set; } = string.Empty;
    public int AssignmentID { get; set; }
}

