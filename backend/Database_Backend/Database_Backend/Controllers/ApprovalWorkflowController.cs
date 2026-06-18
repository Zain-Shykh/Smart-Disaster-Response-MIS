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
[Authorize(Roles = "Administrator,EmergencyOperator,WarehouseManager,Warehouse Manager,FinanceOfficer,Finance Officer")]
public class ApprovalWorkflowController : ControllerBase
{
    private static readonly HashSet<string> AllowedRequestTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ResourceDistribution",
        "RescueDeployment",
        "Financial"
    };

    private static readonly HashSet<string> AllowedRequestStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending",
        "Approved",
        "Rejected"
    };

    private static readonly HashSet<string> AllowedHistoryDecisions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Approved",
        "Rejected",
        "Escalated"
    };

    private readonly DatabaseProjectContext _context;

    public ApprovalWorkflowController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpGet("requests")]
    public async Task<ActionResult<IEnumerable<ApprovalRequestDto>>> GetApprovalRequests(
        [FromQuery] string? requestType,
        [FromQuery] string? status,
        [FromQuery] int? requestedBy,
        [FromQuery] int? reviewedBy,
        CancellationToken cancellationToken)
    {
        IQueryable<ApprovalRequest> requests = _context.ApprovalRequests
            .AsNoTracking()
            .Include(item => item.RequestedByNavigation)
            .Include(item => item.ReviewedByNavigation);

        if (!string.IsNullOrWhiteSpace(requestType))
        {
            var normalizedRequestType = NormalizeRequestType(requestType);
            if (normalizedRequestType is null)
            {
                return BadRequest("RequestType must be one of: ResourceDistribution, RescueDeployment, Financial.");
            }

            requests = requests.Where(item => item.RequestType == normalizedRequestType);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = NormalizeRequestStatus(status);
            if (normalizedStatus is null)
            {
                return BadRequest("Status must be one of: Pending, Approved, Rejected.");
            }

            requests = requests.Where(item => item.Status == normalizedStatus);
        }

        if (requestedBy.HasValue)
        {
            requests = requests.Where(item => item.RequestedBy == requestedBy.Value);
        }

        if (reviewedBy.HasValue)
        {
            requests = requests.Where(item => item.ReviewedBy == reviewedBy.Value);
        }

        var result = await requests
            .OrderByDescending(item => item.RequestTime)
            .Select(item => MapRequest(item))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("requests/{requestId:int}")]
    public async Task<ActionResult<ApprovalRequestDto>> GetApprovalRequest(int requestId, CancellationToken cancellationToken)
    {
        var request = await _context.ApprovalRequests
            .AsNoTracking()
            .Include(item => item.RequestedByNavigation)
            .Include(item => item.ReviewedByNavigation)
            .FirstOrDefaultAsync(item => item.RequestId == requestId, cancellationToken);

        if (request is null)
        {
            return NotFound();
        }

        return Ok(MapRequest(request));
    }

    [HttpPost("requests")]
    public async Task<ActionResult<ApprovalRequestDto>> CreateApprovalRequest(
        [FromBody] ApprovalRequestCreateDto request,
        CancellationToken cancellationToken)
    {
        var requestType = NormalizeRequestType(request.RequestType);
        if (requestType is null)
        {
            return BadRequest("RequestType must be one of: ResourceDistribution, RescueDeployment, Financial.");
        }

        var targetValidation = ValidateTarget(requestType, request.AllocationId, request.AssignmentId, request.ExpenseId);
        if (targetValidation is not null)
        {
            return BadRequest(targetValidation);
        }

        if (!await _context.Users.AnyAsync(item => item.UserId == request.RequestedBy, cancellationToken))
        {
            return NotFound($"RequestedBy user {request.RequestedBy} was not found.");
        }

        if (request.AllocationId.HasValue && !await _context.ResourceAllocations.AnyAsync(item => item.AllocationId == request.AllocationId.Value, cancellationToken))
        {
            return NotFound($"Resource allocation {request.AllocationId.Value} was not found.");
        }

        if (request.AssignmentId.HasValue && !await _context.TeamAssignments.AnyAsync(item => item.AssignmentId == request.AssignmentId.Value, cancellationToken))
        {
            return NotFound($"Team assignment {request.AssignmentId.Value} was not found.");
        }

        if (request.ExpenseId.HasValue && !await _context.Expenses.AnyAsync(item => item.ExpenseId == request.ExpenseId.Value, cancellationToken))
        {
            return NotFound($"Expense {request.ExpenseId.Value} was not found.");
        }

        var approvalRequest = new ApprovalRequest
        {
            RequestedBy = request.RequestedBy,
            ReviewedBy = null,
            RequestType = requestType,
            RequestTime = request.RequestTime ?? DateTime.Now,
            Status = "Pending",
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            AllocationId = request.AllocationId,
            AssignmentId = request.AssignmentId,
            ExpenseId = request.ExpenseId
        };

        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync(cancellationToken);

        var created = await _context.ApprovalRequests
            .AsNoTracking()
            .Include(item => item.RequestedByNavigation)
            .Include(item => item.ReviewedByNavigation)
            .FirstAsync(item => item.RequestId == approvalRequest.RequestId, cancellationToken);

        return CreatedAtAction(nameof(GetApprovalRequest), new { requestId = created.RequestId }, MapRequest(created));
    }

    [HttpPatch("requests/{requestId:int}/decision")]
    public async Task<ActionResult<ApprovalRequestDto>> DecideApprovalRequest(
        int requestId,
        [FromBody] ApprovalDecisionDto request,
        CancellationToken cancellationToken)
    {
        var decision = NormalizeHistoryDecision(request.Decision);
        if (decision is null)
        {
            return BadRequest("Decision must be one of: Approved, Rejected, Escalated.");
        }

        if (!await _context.Users.AnyAsync(item => item.UserId == request.ActionBy, cancellationToken))
        {
            return NotFound($"ActionBy user {request.ActionBy} was not found.");
        }

        var approvalRequest = await _context.ApprovalRequests.FirstOrDefaultAsync(item => item.RequestId == requestId, cancellationToken);
        if (approvalRequest is null)
        {
            return NotFound();
        }

        var currentToken = ComputeVersionToken(approvalRequest);
        if (!string.IsNullOrWhiteSpace(request.VersionToken) && !string.Equals(request.VersionToken, currentToken, StringComparison.Ordinal))
        {
            return Conflict(new ApprovalConcurrencyConflictDto
            {
                Message = "Approval request was modified by another operation.",
                CurrentVersionToken = currentToken
            });
        }

        if (decision == "Escalated")
        {
            var maxHistoryId = await _context.ApprovalHistories
                .Where(item => item.RequestId == requestId)
                .Select(item => (int?)item.HistoryId)
                .MaxAsync(cancellationToken) ?? 0;

            var historyEntry = new ApprovalHistory
            {
                RequestId = requestId,
                HistoryId = maxHistoryId + 1,
                ActionBy = request.ActionBy,
                ActionTime = DateTime.Now,
                Decision = "Escalated",
                Comments = string.IsNullOrWhiteSpace(request.Comments) ? "Escalated by workflow endpoint." : request.Comments.Trim()
            };

            approvalRequest.ReviewedBy = request.ActionBy;
            _context.ApprovalHistories.Add(historyEntry);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            approvalRequest.Status = decision;
            approvalRequest.ReviewedBy = request.ActionBy;

            await ApplyDecisionToTargetAsync(approvalRequest, decision, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            // History row for Approved/Rejected is written by trigger.
        }

        var updated = await _context.ApprovalRequests
            .AsNoTracking()
            .Include(item => item.RequestedByNavigation)
            .Include(item => item.ReviewedByNavigation)
            .FirstAsync(item => item.RequestId == requestId, cancellationToken);

        return Ok(MapRequest(updated));
    }

    private async Task ApplyDecisionToTargetAsync(ApprovalRequest approvalRequest, string decision, CancellationToken cancellationToken)
    {
        if (approvalRequest.AllocationId.HasValue)
        {
            var allocation = await _context.ResourceAllocations
                .FirstOrDefaultAsync(item => item.AllocationId == approvalRequest.AllocationId.Value, cancellationToken);

            if (allocation is not null)
            {
                if (decision == "Approved" && allocation.Status == "Pending")
                {
                    allocation.Status = "Approved";
                }
                else if (decision == "Rejected")
                {
                    allocation.Status = "Rejected";
                }
            }
        }

        if (approvalRequest.ExpenseId.HasValue)
        {
            var expense = await _context.Expenses
                .FirstOrDefaultAsync(item => item.ExpenseId == approvalRequest.ExpenseId.Value, cancellationToken);

            if (expense is not null)
            {
                if (decision == "Approved" && expense.PaymentStatus.Equals("PendingApproval", StringComparison.OrdinalIgnoreCase))
                {
                    expense.PaymentStatus = "Pending";
                }
                else if (decision == "Rejected")
                {
                    expense.PaymentStatus = "RejectedByApproval";
                }
            }
        }
    }

    [HttpGet("requests/{requestId:int}/history")]
    public async Task<ActionResult<IEnumerable<ApprovalHistoryDto>>> GetApprovalHistory(
        int requestId,
        CancellationToken cancellationToken)
    {
        if (!await _context.ApprovalRequests.AnyAsync(item => item.RequestId == requestId, cancellationToken))
        {
            return NotFound();
        }

        var result = await _context.ApprovalHistories
            .AsNoTracking()
            .Include(item => item.ActionByNavigation)
            .Where(item => item.RequestId == requestId)
            .OrderBy(item => item.HistoryId)
            .Select(item => new ApprovalHistoryDto
            {
                RequestId = item.RequestId,
                HistoryId = item.HistoryId,
                ActionBy = item.ActionBy,
                ActionByName = $"{item.ActionByNavigation.FirstName} {item.ActionByNavigation.LastName}",
                ActionTime = item.ActionTime,
                Decision = item.Decision,
                Comments = item.Comments
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<ApprovalHistoryDto>>> GetAllApprovalHistory(
        [FromQuery] string? decision,
        CancellationToken cancellationToken)
    {
        IQueryable<ApprovalHistory> history = _context.ApprovalHistories
            .AsNoTracking()
            .Include(item => item.ActionByNavigation);

        if (!string.IsNullOrWhiteSpace(decision))
        {
            var normalizedDecision = NormalizeHistoryDecision(decision);
            if (normalizedDecision is null)
            {
                return BadRequest("Decision must be one of: Approved, Rejected, Escalated.");
            }

            history = history.Where(item => item.Decision == normalizedDecision);
        }

        var result = await history
            .OrderByDescending(item => item.ActionTime)
            .Select(item => new ApprovalHistoryDto
            {
                RequestId = item.RequestId,
                HistoryId = item.HistoryId,
                ActionBy = item.ActionBy,
                ActionByName = $"{item.ActionByNavigation.FirstName} {item.ActionByNavigation.LastName}",
                ActionTime = item.ActionTime,
                Decision = item.Decision,
                Comments = item.Comments
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    private static string? ValidateTarget(string requestType, int? allocationId, int? assignmentId, int? expenseId)
    {
        var targetCount = 0;
        targetCount += allocationId.HasValue ? 1 : 0;
        targetCount += assignmentId.HasValue ? 1 : 0;
        targetCount += expenseId.HasValue ? 1 : 0;

        if (targetCount != 1)
        {
            return "Exactly one target reference is required: AllocationId, AssignmentId, or ExpenseId.";
        }

        if (requestType == "ResourceDistribution" && !allocationId.HasValue)
        {
            return "ResourceDistribution requests must reference AllocationId.";
        }

        if (requestType == "RescueDeployment" && !assignmentId.HasValue)
        {
            return "RescueDeployment requests must reference AssignmentId.";
        }

        if (requestType == "Financial" && !expenseId.HasValue)
        {
            return "Financial requests must reference ExpenseId.";
        }

        return null;
    }

    private static string? NormalizeRequestType(string? requestType)
    {
        if (string.IsNullOrWhiteSpace(requestType) || !AllowedRequestTypes.Contains(requestType.Trim()))
        {
            return null;
        }

        return AllowedRequestTypes.First(item => item.Equals(requestType.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeRequestStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || !AllowedRequestStatuses.Contains(status.Trim()))
        {
            return null;
        }

        return AllowedRequestStatuses.First(item => item.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeHistoryDecision(string? decision)
    {
        if (string.IsNullOrWhiteSpace(decision) || !AllowedHistoryDecisions.Contains(decision.Trim()))
        {
            return null;
        }

        return AllowedHistoryDecisions.First(item => item.Equals(decision.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static ApprovalRequestDto MapRequest(ApprovalRequest request)
    {
        return new ApprovalRequestDto
        {
            RequestId = request.RequestId,
            RequestedBy = request.RequestedBy,
            RequestedByName = $"{request.RequestedByNavigation.FirstName} {request.RequestedByNavigation.LastName}",
            ReviewedBy = request.ReviewedBy,
            ReviewedByName = request.ReviewedByNavigation == null ? null : $"{request.ReviewedByNavigation.FirstName} {request.ReviewedByNavigation.LastName}",
            RequestType = request.RequestType,
            RequestTime = request.RequestTime,
            Status = request.Status,
            Description = request.Description,
            AllocationId = request.AllocationId,
            AssignmentId = request.AssignmentId,
            ExpenseId = request.ExpenseId,
            VersionToken = ComputeVersionToken(request)
        };
    }

    private static string ComputeVersionToken(ApprovalRequest request)
    {
        return ConcurrencyTokenService.Compute(
            request.RequestId,
            request.RequestedBy,
            request.ReviewedBy,
            request.RequestType,
            request.RequestTime,
            request.Status,
            request.Description,
            request.AllocationId,
            request.AssignmentId,
            request.ExpenseId);
    }

    /// <summary>
    /// Get pending approvals from vw_Pending_Approvals view.
    /// Shows all requests awaiting review.
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<PendingApprovalsDto>>> GetPendingApprovalsFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwPendingApprovals
            .AsNoTracking()
            .Select(v => new PendingApprovalsDto
            {
                RequestId = v.RequestId,
                RequestType = v.RequestType,
                RequestedBy = v.RequestedBy,
                RequestTime = v.RequestTime,
                AllocationId = v.AllocationId,
                AssignmentId = v.AssignmentId,
                ExpenseId = v.ExpenseId,
                Description = v.Description
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get approval history from vw_Approval_History view.
    /// Shows historical decisions and actions on requests.
    /// </summary>
    [HttpGet("requests/{requestId:int}/history/view")]
    public async Task<ActionResult<IEnumerable<PendingApprovalsHistoryDto>>> GetApprovalHistoryFromView(
        int requestId,
        CancellationToken cancellationToken)
    {
        if (!await _context.ApprovalRequests.AnyAsync(r => r.RequestId == requestId, cancellationToken))
        {
            return NotFound($"Request {requestId} was not found.");
        }

        var result = await _context.VwApprovalHistory
            .AsNoTracking()
            .Where(v => v.RequestId == requestId)
            .Select(v => new PendingApprovalsHistoryDto
            {
                RequestId = v.RequestId,
                HistoryId = v.HistoryId,
                ActionBy = v.ActionBy,
                ActionTime = v.ActionTime,
                Decision = v.Decision,
                Comments = v.Comments
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Approve a request using stored procedure (sp_ApproveRequest).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("requests/{requestId:int}/approve-sp")]
    public async Task<ActionResult<RequestApprovalResult>> ApproveRequestStoredProc(
        int requestId,
        [FromBody] ApprovalDecisionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Users.AnyAsync(item => item.UserId == request.ActionBy, cancellationToken))
            {
                return NotFound($"ActionBy user {request.ActionBy} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@RequestID", requestId),
                new SqlParameter("@ReviewedBy", request.ActionBy),
                new SqlParameter("@Comments", (object?)request.Comments ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_ApproveRequest", parameters);

            if (result.ContainsKey("ResultStatus"))
            {
                var resultObj = new RequestApprovalResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    RequestID = requestId
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

    /// <summary>
    /// Reject a request using stored procedure (sp_RejectRequest).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("requests/{requestId:int}/reject-sp")]
    public async Task<ActionResult<RequestApprovalResult>> RejectRequestStoredProc(
        int requestId,
        [FromBody] ApprovalDecisionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Users.AnyAsync(item => item.UserId == request.ActionBy, cancellationToken))
            {
                return NotFound($"ActionBy user {request.ActionBy} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@RequestID", requestId),
                new SqlParameter("@ReviewedBy", request.ActionBy),
                new SqlParameter("@Comments", (object?)request.Comments ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_RejectRequest", parameters);

            if (result.ContainsKey("ResultStatus"))
            {
                var resultObj = new RequestApprovalResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    RequestID = requestId
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

public class ApprovalRequestDto
{
    public int RequestId { get; set; }

    public int RequestedBy { get; set; }

    public string RequestedByName { get; set; } = string.Empty;

    public int? ReviewedBy { get; set; }

    public string? ReviewedByName { get; set; }

    public string RequestType { get; set; } = string.Empty;

    public DateTime RequestTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? AllocationId { get; set; }

    public int? AssignmentId { get; set; }

    public int? ExpenseId { get; set; }

    public string VersionToken { get; set; } = string.Empty;
}

public class ApprovalRequestCreateDto
{
    [Range(1, int.MaxValue)]
    public int RequestedBy { get; set; }

    [Required]
    [MaxLength(60)]
    public string RequestType { get; set; } = string.Empty;

    public DateTime? RequestTime { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int? AllocationId { get; set; }

    public int? AssignmentId { get; set; }

    public int? ExpenseId { get; set; }
}

public class ApprovalDecisionDto
{
    [Required]
    [MaxLength(30)]
    public string Decision { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ActionBy { get; set; }

    [MaxLength(2000)]
    public string? Comments { get; set; }

    public string? VersionToken { get; set; }
}

public class ApprovalConcurrencyConflictDto
{
    public string Message { get; set; } = string.Empty;

    public string CurrentVersionToken { get; set; } = string.Empty;
}

public class ApprovalHistoryDto
{
    public int RequestId { get; set; }

    public int HistoryId { get; set; }

    public int ActionBy { get; set; }

    public string ActionByName { get; set; } = string.Empty;

    public DateTime ActionTime { get; set; }

    public string Decision { get; set; } = string.Empty;

    public string? Comments { get; set; }
}

// ============================================================================
// DATABASE VIEW DTOs
// ============================================================================

public class PendingApprovalsDto
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

public class PendingApprovalsHistoryDto
{
    public int RequestId { get; set; }
    public int HistoryId { get; set; }
    public int ActionBy { get; set; }
    public DateTime ActionTime { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
}
