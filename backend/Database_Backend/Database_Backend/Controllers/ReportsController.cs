using Database_Backend.Models;
using Database_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator,EmergencyOperator,FieldOfficer,Field Officer,WarehouseManager,Warehouse Manager,FinanceOfficer,Finance Officer")]
public class ReportsController : ControllerBase
{
    private readonly DatabaseProjectContext _context;

    public ReportsController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<OverviewReportDto>> GetOverview(CancellationToken cancellationToken)
    {
        var totalEvents = await _context.DisasterEvents.CountAsync(cancellationToken);
        var activeEvents = await _context.DisasterEvents.CountAsync(item => item.Status == "Active", cancellationToken);

        var totalReports = await _context.EmergencyReports.CountAsync(cancellationToken);
        var pendingReports = await _context.EmergencyReports.CountAsync(item => item.Status == "Pending", cancellationToken);
        var inProgressReports = await _context.EmergencyReports.CountAsync(item => item.Status == "InProgress", cancellationToken);
        var resolvedReports = await _context.EmergencyReports.CountAsync(item => item.Status == "Resolved" || item.Status == "Closed", cancellationToken);

        var availableTeams = await _context.RescueTeams.CountAsync(item => item.AvailabilityStatus == "Available", cancellationToken);
        var busyTeams = await _context.RescueTeams.CountAsync(item => item.AvailabilityStatus == "Busy", cancellationToken);

        var lowStockInventories = await _context.Inventories.CountAsync(item => item.Quantity <= item.MinThreshold, cancellationToken);

        var confirmedDonations = await _context.Donations
            .Where(item => item.Status == "Confirmed")
            .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;

        var totalExpenses = await _context.Expenses
            .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;

        var avgResponseMinutes = await _context.EmergencyReports
            .Where(item => item.ResponseTimeMinutes.HasValue)
            .AverageAsync(item => (double?)item.ResponseTimeMinutes, cancellationToken);

        var avgResolutionMinutes = await _context.EmergencyReports
            .Where(item => item.ResolutionTimeMinutes.HasValue)
            .AverageAsync(item => (double?)item.ResolutionTimeMinutes, cancellationToken);

        return Ok(new OverviewReportDto
        {
            TotalEvents = totalEvents,
            ActiveEvents = activeEvents,
            TotalReports = totalReports,
            PendingReports = pendingReports,
            InProgressReports = inProgressReports,
            ResolvedReports = resolvedReports,
            AvailableTeams = availableTeams,
            BusyTeams = busyTeams,
            LowStockInventories = lowStockInventories,
            ConfirmedDonationAmount = confirmedDonations,
            TotalExpenseAmount = totalExpenses,
            AverageResponseMinutes = avgResponseMinutes,
            AverageResolutionMinutes = avgResolutionMinutes
        });
    }

    [HttpGet("incidents/by-location")]
    public async Task<ActionResult<IEnumerable<IncidentLocationReportDto>>> GetIncidentByLocation(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        IQueryable<EmergencyReport> reports = _context.EmergencyReports.AsNoTracking();

        if (from.HasValue)
        {
            reports = reports.Where(item => item.ReportTime >= from.Value);
        }

        if (to.HasValue)
        {
            reports = reports.Where(item => item.ReportTime <= to.Value);
        }

        var result = await reports
            .GroupBy(item => new { item.City, item.Province })
            .Select(group => new IncidentLocationReportDto
            {
                City = group.Key.City,
                Province = group.Key.Province,
                TotalReports = group.Count(),
                CriticalReports = group.Count(item => item.SeverityLevel == "Critical"),
                HighReports = group.Count(item => item.SeverityLevel == "High")
            })
            .OrderByDescending(item => item.TotalReports)
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("incidents/by-type")]
    public async Task<ActionResult<IEnumerable<IncidentTypeReportDto>>> GetIncidentByType(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        IQueryable<EmergencyReport> reports = _context.EmergencyReports.AsNoTracking();

        if (from.HasValue)
        {
            reports = reports.Where(item => item.ReportTime >= from.Value);
        }

        if (to.HasValue)
        {
            reports = reports.Where(item => item.ReportTime <= to.Value);
        }

        var result = await reports
            .GroupBy(item => item.DisasterType)
            .Select(group => new IncidentTypeReportDto
            {
                DisasterType = group.Key,
                TotalReports = group.Count(),
                AverageResponseMinutes = group.Average(item => (double?)item.ResponseTimeMinutes),
                AverageResolutionMinutes = group.Average(item => (double?)item.ResolutionTimeMinutes)
            })
            .OrderByDescending(item => item.TotalReports)
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("incidents/by-severity")]
    public async Task<ActionResult<IEnumerable<IncidentSeverityReportDto>>> GetIncidentsBySeverity(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        IQueryable<EmergencyReport> reports = _context.EmergencyReports.AsNoTracking();

        if (from.HasValue)
        {
            reports = reports.Where(item => item.ReportTime >= from.Value);
        }

        if (to.HasValue)
        {
            reports = reports.Where(item => item.ReportTime <= to.Value);
        }

        var result = await reports
            .GroupBy(item => item.SeverityLevel)
            .Select(group => new IncidentSeverityReportDto
            {
                SeverityLevel = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.SeverityLevel)
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("incidents/trend")]
    public async Task<ActionResult<IEnumerable<IncidentTrendPointDto>>> GetIncidentTrend(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        IQueryable<EmergencyReport> reports = _context.EmergencyReports.AsNoTracking();

        if (from.HasValue)
        {
            reports = reports.Where(item => item.ReportTime >= from.Value);
        }

        if (to.HasValue)
        {
            reports = reports.Where(item => item.ReportTime <= to.Value);
        }

        var grouped = await reports
            .GroupBy(item => new { item.ReportTime.Year, item.ReportTime.Month, item.ReportTime.Day })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.Day,
                Count = group.Count()
            })
            .OrderBy(item => item.Year)
            .ThenBy(item => item.Month)
            .ThenBy(item => item.Day)
            .ToListAsync(cancellationToken);

        var result = grouped
            .Select(item => new IncidentTrendPointDto
            {
                Date = new DateTime(item.Year, item.Month, item.Day),
                Count = item.Count
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("incidents/prioritized")]
    public async Task<ActionResult<IEnumerable<PrioritizedIncidentDto>>> GetPrioritizedIncidents(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            limit = 50;
        }

        if (limit > 500)
        {
            limit = 500;
        }

        var reports = await _context.EmergencyReports
            .AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.Citizen)
            .ToListAsync(cancellationToken);

        var result = reports
            .Select(item =>
            {
                var priority = IncidentPriorityService.Calculate(item.SeverityLevel, item.Event?.AffectedPopulation ?? 0);

                return new PrioritizedIncidentDto
                {
                    ReportId = item.ReportId,
                    EventId = item.EventId,
                    EventName = item.Event?.EventName,
                    City = item.City,
                    DisasterType = item.DisasterType,
                    SeverityLevel = item.SeverityLevel,
                    PriorityLevel = priority.PriorityLevel,
                    PriorityLabel = priority.PriorityLabel,
                    PriorityScore = priority.PriorityScore,
                    EstimatedResponseMinutes = priority.EstimatedResponseMinutes,
                    ReportTime = item.ReportTime
                };
            })
            .OrderBy(item => item.PriorityLevel)
            .ThenByDescending(item => item.PriorityScore)
            .ThenBy(item => item.ReportTime)
            .Take(limit)
            .ToList();

        return Ok(result);
    }

    [HttpGet("resources/utilization")]
    public async Task<ActionResult<IEnumerable<ResourceUtilizationReportDto>>> GetResourceUtilization(
        [FromQuery] int? eventId,
        CancellationToken cancellationToken)
    {
        IQueryable<ResourceAllocation> allocations = _context.ResourceAllocations
            .AsNoTracking()
            .Include(item => item.Inventory)
            .ThenInclude(item => item.Resource);

        if (eventId.HasValue)
        {
            allocations = allocations.Where(item => item.EventId == eventId.Value);
        }

        var allocationAgg = await allocations
            .GroupBy(item => new { item.Inventory.ResourceId, item.Inventory.Resource.ResourceName, item.Inventory.Resource.ResourceType })
            .Select(group => new
            {
                group.Key.ResourceId,
                group.Key.ResourceName,
                group.Key.ResourceType,
                Requested = group.Sum(item => item.Quantity),
                Dispatched = group.Where(item => item.Status == "Dispatched" || item.Status == "Consumed").Sum(item => item.Quantity),
                Consumed = group.Where(item => item.Status == "Consumed").Sum(item => item.Quantity)
            })
            .ToListAsync(cancellationToken);

        var stockAgg = await _context.Inventories
            .AsNoTracking()
            .Include(item => item.Resource)
            .GroupBy(item => new { item.ResourceId, item.Resource.ResourceName, item.Resource.ResourceType })
            .Select(group => new
            {
                group.Key.ResourceId,
                CurrentStock = group.Sum(item => item.Quantity)
            })
            .ToListAsync(cancellationToken);

        var stockMap = stockAgg.ToDictionary(item => item.ResourceId, item => item.CurrentStock);

        var result = allocationAgg
            .Select(item => new ResourceUtilizationReportDto
            {
                ResourceId = item.ResourceId,
                ResourceName = item.ResourceName,
                ResourceType = item.ResourceType,
                RequestedQuantity = item.Requested,
                DispatchedQuantity = item.Dispatched,
                ConsumedQuantity = item.Consumed,
                CurrentStock = stockMap.TryGetValue(item.ResourceId, out var currentStock) ? currentStock : 0m
            })
            .OrderByDescending(item => item.RequestedQuantity)
            .ToList();

        return Ok(result);
    }

    [HttpGet("financial/summary")]
    public async Task<ActionResult<FinancialSummaryReportDto>> GetFinancialSummary(
        [FromQuery] int? eventId,
        CancellationToken cancellationToken)
    {
        IQueryable<Donation> donations = _context.Donations.AsNoTracking();
        IQueryable<Expense> expenses = _context.Expenses.AsNoTracking();

        if (eventId.HasValue)
        {
            donations = donations.Where(item => item.EventId == eventId.Value);
            expenses = expenses.Where(item => item.EventId == eventId.Value);
        }

        var donationSummary = await donations
            .GroupBy(item => item.Status)
            .Select(group => new FinancialDonationStatusDto
            {
                Status = group.Key,
                Count = group.Count(),
                Amount = group.Sum(item => item.Amount)
            })
            .OrderBy(item => item.Status)
            .ToListAsync(cancellationToken);

        var expenseSummary = await expenses
            .GroupBy(item => item.Category)
            .Select(group => new FinancialExpenseCategoryDto
            {
                Category = group.Key,
                Count = group.Count(),
                Amount = group.Sum(item => item.Amount)
            })
            .OrderBy(item => item.Category)
            .ToListAsync(cancellationToken);

        var confirmedDonations = donationSummary
            .Where(item => item.Status == "Confirmed")
            .Sum(item => item.Amount);

        var totalExpenses = expenseSummary.Sum(item => item.Amount);

        return Ok(new FinancialSummaryReportDto
        {
            DonationSummary = donationSummary,
            ExpenseSummary = expenseSummary,
            ConfirmedDonationAmount = confirmedDonations,
            TotalExpenseAmount = totalExpenses,
            NetBalance = confirmedDonations - totalExpenses
        });
    }

    [HttpGet("approvals/summary")]
    public async Task<ActionResult<ApprovalSummaryReportDto>> GetApprovalSummary(CancellationToken cancellationToken)
    {
        var requestStatusSummary = await _context.ApprovalRequests
            .AsNoTracking()
            .GroupBy(item => new { item.RequestType, item.Status })
            .Select(group => new ApprovalRequestStatusSummaryDto
            {
                RequestType = group.Key.RequestType,
                Status = group.Key.Status,
                Count = group.Count()
            })
            .OrderBy(item => item.RequestType)
            .ThenBy(item => item.Status)
            .ToListAsync(cancellationToken);

        var decisionSummary = await _context.ApprovalHistories
            .AsNoTracking()
            .GroupBy(item => item.Decision)
            .Select(group => new ApprovalDecisionSummaryDto
            {
                Decision = group.Key,
                Count = group.Count()
            })
            .OrderBy(item => item.Decision)
            .ToListAsync(cancellationToken);

        return Ok(new ApprovalSummaryReportDto
        {
            RequestStatusSummary = requestStatusSummary,
            DecisionSummary = decisionSummary
        });
    }

    [HttpGet("audit/logs")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? tableName,
        [FromQuery] string? action,
        [FromQuery] int? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            limit = 100;
        }

        if (limit > 500)
        {
            limit = 500;
        }

        IQueryable<AuditLog> logs = _context.AuditLogs
            .AsNoTracking()
            .Include(item => item.User);

        if (!string.IsNullOrWhiteSpace(tableName))
        {
            var table = tableName.Trim();
            logs = logs.Where(item => item.TableName == table);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var actionValue = action.Trim();
            logs = logs.Where(item => item.Action == actionValue);
        }

        if (userId.HasValue)
        {
            logs = logs.Where(item => item.UserId == userId.Value);
        }

        if (from.HasValue)
        {
            logs = logs.Where(item => item.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            logs = logs.Where(item => item.Timestamp <= to.Value);
        }

        var result = await logs
            .OrderByDescending(item => item.Timestamp)
            .Take(limit)
            .Select(item => new AuditLogDto
            {
                LogId = item.LogId,
                UserId = item.UserId,
                Username = item.User == null ? null : item.User.Username,
                Action = item.Action,
                TableName = item.TableName,
                RecordId = item.RecordId,
                OldValue = item.OldValue,
                NewValue = item.NewValue,
                Timestamp = item.Timestamp,
                IpAddress = item.Ipaddress
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get event overview from vw_Event_Overview view.
    /// Shows aggregated event statistics and metrics.
    /// </summary>
    [HttpGet("events/overview")]
    public async Task<ActionResult<IEnumerable<EventOverviewDto>>> GetEventOverviewFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwEventOverview
            .AsNoTracking()
            .Select(v => new EventOverviewDto
            {
                EventId = v.EventId,
                EventName = v.EventName,
                StartTime = v.StartTime,
                EndTime = v.EndTime,
                Status = v.Status,
                AffectedPopulation = v.AffectedPopulation,
                IncidentCount = v.IncidentCount,
                TotalAllocations = v.TotalAllocations,
                TotalDonations = v.TotalDonations,
                TotalExpenses = v.TotalExpenses
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get response performance from vw_Response_Performance view.
    /// Shows performance metrics including response times.
    /// </summary>
    [HttpGet("performance")]
    public async Task<ActionResult<IEnumerable<ResponsePerformanceDto>>> GetResponsePerformanceFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwResponsePerformance
            .AsNoTracking()
            .Select(v => new ResponsePerformanceDto
            {
                EventId = v.EventId,
                AvgResponseTime = v.AvgResponseTime,
                AvgTeamCompletionTime = v.AvgTeamCompletionTime,
                ResourceUtilizationPercent = v.ResourceUtilizationPercent
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get recent audit logs from vw_Audit_Recent view.
    /// Shows latest audit trail entries.
    /// </summary>
    [HttpGet("audit/recent")]
    public async Task<ActionResult<IEnumerable<AuditRecentDto>>> GetAuditRecentFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwAuditRecent
            .AsNoTracking()
            .Select(v => new AuditRecentDto
            {
                LogId = v.LogId,
                UserId = v.UserId,
                Action = v.Action,
                TableName = v.TableName,
                RecordId = v.RecordId,
                Timestamp = v.Timestamp,
                OldValue = v.OldValue,
                NewValue = v.NewValue
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get financial audit trail from vw_FinancialAuditTrail view.
    /// Shows all financial transaction audits.
    /// </summary>
    [HttpGet("audit/financial-trail")]
    public async Task<ActionResult<IEnumerable<FinancialAuditTrailDto>>> GetFinancialAuditTrailFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwFinancialAuditTrail
            .AsNoTracking()
            .Select(v => new FinancialAuditTrailDto
            {
                LogId = v.LogId,
                UserId = v.UserId,
                Action = v.Action,
                TableName = v.TableName,
                RecordId = v.RecordId,
                Timestamp = v.Timestamp,
                OldValue = v.OldValue,
                NewValue = v.NewValue
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get dashboard statistics using stored procedure (sp_GetDashboardStats).
    /// Provides ACID-compliant aggregation at the database level.
    /// </summary>
    [HttpGet("dashboard-stats-sp")]
    public async Task<ActionResult<DashboardStatsResult>> GetDashboardStatsStoredProc(
        [FromQuery] int? eventId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new List<SqlParameter>();
            
            if (eventId.HasValue)
            {
                parameters.Add(new SqlParameter("@EventID", eventId.Value));
            }
            
            if (startDate.HasValue)
            {
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }
            
            if (endDate.HasValue)
            {
                parameters.Add(new SqlParameter("@EndDate", endDate.Value));
            }

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_GetDashboardStats", parameters.ToArray());

            if (result.ContainsKey("IncidentCount"))
            {
                var resultObj = new DashboardStatsResult
                {
                    IncidentCount = (int)(result["IncidentCount"] ?? 0),
                    ResourceAllocationCount = (int)(result["ResourceAllocationCount"] ?? 0),
                    TeamAssignmentCount = (int)(result["TeamAssignmentCount"] ?? 0),
                    ActiveTeamsCount = (int)(result["ActiveTeamsCount"] ?? 0),
                    OngoingOperationsCount = (int)(result["OngoingOperationsCount"] ?? 0),
                    CompletedOperationsCount = (int)(result["CompletedOperationsCount"] ?? 0),
                    TotalResourcesDispatched = (int)(result["TotalResourcesDispatched"] ?? 0),
                    RespondingTeamsCount = (int)(result["RespondingTeamsCount"] ?? 0)
                };
                return Ok(resultObj);
            }

            return StatusCode(500, "Stored procedure did not return expected result.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
        }
    }
}

public class OverviewReportDto
{
    public int TotalEvents { get; set; }

    public int ActiveEvents { get; set; }

    public int TotalReports { get; set; }

    public int PendingReports { get; set; }

    public int InProgressReports { get; set; }

    public int ResolvedReports { get; set; }

    public int AvailableTeams { get; set; }

    public int BusyTeams { get; set; }

    public int LowStockInventories { get; set; }

    public decimal ConfirmedDonationAmount { get; set; }

    public decimal TotalExpenseAmount { get; set; }

    public double? AverageResponseMinutes { get; set; }

    public double? AverageResolutionMinutes { get; set; }
}

public class IncidentLocationReportDto
{
    public string City { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public int TotalReports { get; set; }

    public int CriticalReports { get; set; }

    public int HighReports { get; set; }
}

public class IncidentTypeReportDto
{
    public string DisasterType { get; set; } = string.Empty;

    public int TotalReports { get; set; }

    public double? AverageResponseMinutes { get; set; }

    public double? AverageResolutionMinutes { get; set; }
}

public class IncidentSeverityReportDto
{
    public string SeverityLevel { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class IncidentTrendPointDto
{
    public DateTime Date { get; set; }

    public int Count { get; set; }
}

public class PrioritizedIncidentDto
{
    public int ReportId { get; set; }

    public int? EventId { get; set; }

    public string? EventName { get; set; }

    public string City { get; set; } = string.Empty;

    public string DisasterType { get; set; } = string.Empty;

    public string SeverityLevel { get; set; } = string.Empty;

    public int PriorityLevel { get; set; }

    public string PriorityLabel { get; set; } = string.Empty;

    public decimal PriorityScore { get; set; }

    public int EstimatedResponseMinutes { get; set; }

    public DateTime ReportTime { get; set; }
}

public class ResourceUtilizationReportDto
{
    public int ResourceId { get; set; }

    public string ResourceName { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public decimal RequestedQuantity { get; set; }

    public decimal DispatchedQuantity { get; set; }

    public decimal ConsumedQuantity { get; set; }

    public decimal CurrentStock { get; set; }
}

public class FinancialSummaryReportDto
{
    public List<FinancialDonationStatusDto> DonationSummary { get; set; } = new();

    public List<FinancialExpenseCategoryDto> ExpenseSummary { get; set; } = new();

    public decimal ConfirmedDonationAmount { get; set; }

    public decimal TotalExpenseAmount { get; set; }

    public decimal NetBalance { get; set; }
}

public class FinancialDonationStatusDto
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }

    public decimal Amount { get; set; }
}

public class FinancialExpenseCategoryDto
{
    public string Category { get; set; } = string.Empty;

    public int Count { get; set; }

    public decimal Amount { get; set; }
}

public class ApprovalSummaryReportDto
{
    public List<ApprovalRequestStatusSummaryDto> RequestStatusSummary { get; set; } = new();

    public List<ApprovalDecisionSummaryDto> DecisionSummary { get; set; } = new();
}

public class ApprovalRequestStatusSummaryDto
{
    public string RequestType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class ApprovalDecisionSummaryDto
{
    public string Decision { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class AuditLogDto
{
    public long LogId { get; set; }

    public int? UserId { get; set; }

    public string? Username { get; set; }

    public string Action { get; set; } = string.Empty;

    public string TableName { get; set; } = string.Empty;

    public string RecordId { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime Timestamp { get; set; }

    public string? IpAddress { get; set; }
}

public class DashboardStatsResult
{
    public int IncidentCount { get; set; }
    public int ResourceAllocationCount { get; set; }
    public int TeamAssignmentCount { get; set; }
    public int ActiveTeamsCount { get; set; }
    public int OngoingOperationsCount { get; set; }
    public int CompletedOperationsCount { get; set; }
    public int TotalResourcesDispatched { get; set; }
    public int RespondingTeamsCount { get; set; }
}

// ============================================================================
// DATABASE VIEW DTOs
// ============================================================================

public class EventOverviewDto
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

public class ResponsePerformanceDto
{
    public int EventId { get; set; }
    public decimal AvgResponseTime { get; set; }
    public decimal AvgTeamCompletionTime { get; set; }
    public decimal ResourceUtilizationPercent { get; set; }
}

public class AuditRecentDto
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

public class FinancialAuditTrailDto
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
