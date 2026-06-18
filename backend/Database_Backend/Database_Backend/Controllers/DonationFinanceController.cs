using Database_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator,FinanceOfficer,Finance Officer")]
public class DonationFinanceController : ControllerBase
{
    private static readonly HashSet<string> AllowedDonorTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Individual",
        "Organization"
    };

    private static readonly HashSet<string> AllowedPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cash",
        "BankTransfer",
        "Online"
    };

    private static readonly HashSet<string> AllowedDonationStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending",
        "Confirmed",
        "Rejected"
    };

    private static readonly HashSet<string> AllowedExpenseCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Procurement",
        "Operations",
        "Medical",
        "Logistics"
    };

    private readonly DatabaseProjectContext _context;

    public DonationFinanceController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpGet("donors")]
    public async Task<ActionResult<IEnumerable<DonorDto>>> GetDonors([FromQuery] string? donorType, CancellationToken cancellationToken)
    {
        IQueryable<Donor> donors = _context.Donors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(donorType))
        {
            var normalizedDonorType = NormalizeDonorType(donorType);
            if (normalizedDonorType is null)
            {
                return BadRequest("DonorType must be one of: Individual, Organization.");
            }

            donors = donors.Where(item => item.DonorType == normalizedDonorType);
        }

        var result = await donors
            .OrderBy(item => item.LastName)
            .ThenBy(item => item.FirstName)
            .Select(item => new DonorDto
            {
                DonorId = item.DonorId,
                FirstName = item.FirstName,
                LastName = item.LastName,
                DonorType = item.DonorType,
                OrganizationName = item.OrganizationName,
                Email = item.Email,
                Street = item.Street,
                Area = item.Area,
                City = item.City,
                Province = item.Province
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("donors")]
    public async Task<ActionResult<DonorDto>> CreateDonor([FromBody] DonorCreateDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.Street) || string.IsNullOrWhiteSpace(request.Area) || string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.Province))
        {
            return BadRequest("FirstName, LastName, Street, Area, City, and Province are required.");
        }

        var donorType = NormalizeDonorType(request.DonorType);
        if (donorType is null)
        {
            return BadRequest("DonorType must be one of: Individual, Organization.");
        }

        if (donorType == "Organization" && string.IsNullOrWhiteSpace(request.OrganizationName))
        {
            return BadRequest("OrganizationName is required for organization donors.");
        }

        var donor = new Donor
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DonorType = donorType,
            OrganizationName = string.IsNullOrWhiteSpace(request.OrganizationName) ? null : request.OrganizationName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Street = request.Street.Trim(),
            Area = request.Area.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim()
        };

        _context.Donors.Add(donor);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Donor could not be saved. Email may already exist.");
        }

        return CreatedAtAction(nameof(GetDonors), new { donorType = donor.DonorType }, MapDonor(donor));
    }

    [HttpGet("donations")]
    public async Task<ActionResult<IEnumerable<DonationDto>>> GetDonations(
        [FromQuery] int? eventId,
        [FromQuery] int? donorId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        IQueryable<Donation> donations = _context.Donations
            .AsNoTracking()
            .Include(item => item.Donor)
            .Include(item => item.Event);

        if (eventId.HasValue)
        {
            donations = donations.Where(item => item.EventId == eventId.Value);
        }

        if (donorId.HasValue)
        {
            donations = donations.Where(item => item.DonorId == donorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = NormalizeDonationStatus(status);
            if (normalizedStatus is null)
            {
                return BadRequest("Status must be one of: Pending, Confirmed, Rejected.");
            }

            donations = donations.Where(item => item.Status == normalizedStatus);
        }

        var result = await donations
            .OrderByDescending(item => item.DonationDate)
            .Select(item => new DonationDto
            {
                DonationId = item.DonationId,
                DonorId = item.DonorId,
                DonorName = $"{item.Donor.FirstName} {item.Donor.LastName}",
                EventId = item.EventId,
                EventName = item.Event.EventName,
                Amount = item.Amount,
                DonationDate = item.DonationDate,
                PaymentMethod = item.PaymentMethod,
                Status = item.Status,
                ReceiptNumber = item.ReceiptNumber
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("donations")]
    public async Task<ActionResult<DonationDto>> CreateDonation([FromBody] DonationCreateDto request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be greater than zero.");
        }

        var paymentMethod = NormalizePaymentMethod(request.PaymentMethod);
        if (paymentMethod is null)
        {
            return BadRequest("PaymentMethod must be one of: Cash, BankTransfer, Online.");
        }

        var status = NormalizeDonationStatus(request.Status);
        if (status is null)
        {
            return BadRequest("Status must be one of: Pending, Confirmed, Rejected.");
        }

        if (!await _context.Donors.AnyAsync(item => item.DonorId == request.DonorId, cancellationToken))
        {
            return NotFound($"Donor {request.DonorId} was not found.");
        }

        if (!await _context.DisasterEvents.AnyAsync(item => item.EventId == request.EventId, cancellationToken))
        {
            return NotFound($"Disaster event {request.EventId} was not found.");
        }

        var donation = new Donation
        {
            DonorId = request.DonorId,
            EventId = request.EventId,
            Amount = request.Amount,
            DonationDate = request.DonationDate ?? DateTime.Now,
            PaymentMethod = paymentMethod,
            Status = status,
            ReceiptNumber = string.IsNullOrWhiteSpace(request.ReceiptNumber) ? null : request.ReceiptNumber.Trim()
        };

        _context.Donations.Add(donation);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Donation could not be saved. ReceiptNumber may already exist.");
        }

        var created = await _context.Donations
            .AsNoTracking()
            .Include(item => item.Donor)
            .Include(item => item.Event)
            .FirstAsync(item => item.DonationId == donation.DonationId, cancellationToken);

        return CreatedAtAction(nameof(GetDonations), new { donorId = created.DonorId }, MapDonation(created));
    }

    [HttpPatch("donations/{donationId:int}/status")]
    public async Task<ActionResult<DonationDto>> UpdateDonationStatus(
        int donationId,
        [FromBody] DonationStatusUpdateDto request,
        CancellationToken cancellationToken)
    {
        var donation = await _context.Donations.FirstOrDefaultAsync(item => item.DonationId == donationId, cancellationToken);
        if (donation is null)
        {
            return NotFound();
        }

        var status = NormalizeDonationStatus(request.Status);
        if (status is null)
        {
            return BadRequest("Status must be one of: Pending, Confirmed, Rejected.");
        }

        donation.Status = status;
        await _context.SaveChangesAsync(cancellationToken);

        var updated = await _context.Donations
            .AsNoTracking()
            .Include(item => item.Donor)
            .Include(item => item.Event)
            .FirstAsync(item => item.DonationId == donationId, cancellationToken);

        return Ok(MapDonation(updated));
    }

    [HttpGet("expenses")]
    public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses(
        [FromQuery] int? eventId,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        IQueryable<Expense> expenses = _context.Expenses
            .AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.ApprovedByNavigation);

        if (eventId.HasValue)
        {
            expenses = expenses.Where(item => item.EventId == eventId.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = NormalizeExpenseCategory(category);
            if (normalizedCategory is null)
            {
                return BadRequest("Category must be one of: Procurement, Operations, Medical, Logistics.");
            }

            expenses = expenses.Where(item => item.Category == normalizedCategory);
        }

        var result = await expenses
            .OrderByDescending(item => item.ExpenseDate)
            .Select(item => new ExpenseDto
            {
                ExpenseId = item.ExpenseId,
                EventId = item.EventId,
                EventName = item.Event.EventName,
                ApprovedBy = item.ApprovedBy,
                ApprovedByName = item.ApprovedByNavigation == null ? null : $"{item.ApprovedByNavigation.FirstName} {item.ApprovedByNavigation.LastName}",
                Category = item.Category,
                Amount = item.Amount,
                Description = item.Description,
                ExpenseDate = item.ExpenseDate,
                PaymentStatus = item.PaymentStatus
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("expenses")]
    public async Task<ActionResult<ExpenseDto>> CreateExpense([FromBody] ExpenseCreateDto request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.PaymentStatus))
        {
            return BadRequest("PaymentStatus is required.");
        }

        var category = NormalizeExpenseCategory(request.Category);
        if (category is null)
        {
            return BadRequest("Category must be one of: Procurement, Operations, Medical, Logistics.");
        }

        if (!await _context.DisasterEvents.AnyAsync(item => item.EventId == request.EventId, cancellationToken))
        {
            return NotFound($"Disaster event {request.EventId} was not found.");
        }

        if (request.ApprovedBy.HasValue && !await _context.Users.AnyAsync(item => item.UserId == request.ApprovedBy.Value, cancellationToken))
        {
            return NotFound($"Approver user {request.ApprovedBy.Value} was not found.");
        }

        var approvalRequestedBy = request.ApprovalRequestedBy ?? request.ApprovedBy;
        if (request.RequiresApproval)
        {
            if (!approvalRequestedBy.HasValue)
            {
                return BadRequest("ApprovalRequestedBy or ApprovedBy is required when RequiresApproval is true.");
            }

            if (!await _context.Users.AnyAsync(item => item.UserId == approvalRequestedBy.Value, cancellationToken))
            {
                return NotFound($"Approval requested-by user {approvalRequestedBy.Value} was not found.");
            }
        }

        var expense = new Expense
        {
            EventId = request.EventId,
            ApprovedBy = request.ApprovedBy,
            Category = category,
            Amount = request.Amount,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ExpenseDate = request.ExpenseDate ?? DateTime.Now,
            PaymentStatus = request.RequiresApproval ? "PendingApproval" : request.PaymentStatus.Trim()
        };

        IDbContextTransaction? transaction = null;
        try
        {
            if (!string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
            {
                transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            }

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync(cancellationToken);

            if (request.RequiresApproval && approvalRequestedBy.HasValue)
            {
                var approvalRequest = new ApprovalRequest
                {
                    RequestedBy = approvalRequestedBy.Value,
                    RequestType = "Financial",
                    RequestTime = DateTime.Now,
                    Status = "Pending",
                    Description = $"Approval required for expense {expense.ExpenseId}.",
                    ExpenseId = expense.ExpenseId
                };

                _context.ApprovalRequests.Add(approvalRequest);
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return Conflict("Expense could not be saved.");
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }

        var created = await _context.Expenses
            .AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.ApprovedByNavigation)
            .FirstAsync(item => item.ExpenseId == expense.ExpenseId, cancellationToken);

        return CreatedAtAction(nameof(GetExpenses), new { eventId = created.EventId }, MapExpense(created));
    }

    [HttpPatch("expenses/{expenseId:int}/payment-status")]
    public async Task<ActionResult<ExpenseDto>> UpdateExpensePaymentStatus(
        int expenseId,
        [FromBody] ExpensePaymentStatusUpdateDto request,
        CancellationToken cancellationToken)
    {
        var expense = await _context.Expenses.FirstOrDefaultAsync(item => item.ExpenseId == expenseId, cancellationToken);
        if (expense is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.PaymentStatus))
        {
            return BadRequest("PaymentStatus is required.");
        }

        var targetPaymentStatus = request.PaymentStatus.Trim();
        if ((targetPaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase)
            || targetPaymentStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            && !await HasApprovedExpenseRequestAsync(expenseId, cancellationToken))
        {
            return BadRequest("Expense cannot be marked paid/completed until an approval request is approved.");
        }

        expense.PaymentStatus = targetPaymentStatus;
        await _context.SaveChangesAsync(cancellationToken);

        var updated = await _context.Expenses
            .AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.ApprovedByNavigation)
            .FirstAsync(item => item.ExpenseId == expenseId, cancellationToken);

        return Ok(MapExpense(updated));
    }

    private async Task<bool> HasApprovedExpenseRequestAsync(int expenseId, CancellationToken cancellationToken)
    {
        return await _context.ApprovalRequests
            .AsNoTracking()
            .AnyAsync(item => item.ExpenseId == expenseId
                && item.RequestType == "Financial"
                && item.Status == "Approved", cancellationToken);
    }

    private static string? NormalizeDonorType(string? donorType)
    {
        if (string.IsNullOrWhiteSpace(donorType) || !AllowedDonorTypes.Contains(donorType.Trim()))
        {
            return null;
        }

        return AllowedDonorTypes.First(item => item.Equals(donorType.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizePaymentMethod(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod) || !AllowedPaymentMethods.Contains(paymentMethod.Trim()))
        {
            return null;
        }

        return AllowedPaymentMethods.First(item => item.Equals(paymentMethod.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeDonationStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || !AllowedDonationStatuses.Contains(status.Trim()))
        {
            return null;
        }

        return AllowedDonationStatuses.First(item => item.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeExpenseCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category) || !AllowedExpenseCategories.Contains(category.Trim()))
        {
            return null;
        }

        return AllowedExpenseCategories.First(item => item.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static DonorDto MapDonor(Donor donor)
    {
        return new DonorDto
        {
            DonorId = donor.DonorId,
            FirstName = donor.FirstName,
            LastName = donor.LastName,
            DonorType = donor.DonorType,
            OrganizationName = donor.OrganizationName,
            Email = donor.Email,
            Street = donor.Street,
            Area = donor.Area,
            City = donor.City,
            Province = donor.Province
        };
    }

    private static DonationDto MapDonation(Donation donation)
    {
        return new DonationDto
        {
            DonationId = donation.DonationId,
            DonorId = donation.DonorId,
            DonorName = $"{donation.Donor.FirstName} {donation.Donor.LastName}",
            EventId = donation.EventId,
            EventName = donation.Event.EventName,
            Amount = donation.Amount,
            DonationDate = donation.DonationDate,
            PaymentMethod = donation.PaymentMethod,
            Status = donation.Status,
            ReceiptNumber = donation.ReceiptNumber
        };
    }

    private static ExpenseDto MapExpense(Expense expense)
    {
        return new ExpenseDto
        {
            ExpenseId = expense.ExpenseId,
            EventId = expense.EventId,
            EventName = expense.Event.EventName,
            ApprovedBy = expense.ApprovedBy,
            ApprovedByName = expense.ApprovedByNavigation == null ? null : $"{expense.ApprovedByNavigation.FirstName} {expense.ApprovedByNavigation.LastName}",
            Category = expense.Category,
            Amount = expense.Amount,
            Description = expense.Description,
            ExpenseDate = expense.ExpenseDate,
            PaymentStatus = expense.PaymentStatus
        };
    }

    /// <summary>
    /// Get donations summary from vw_Donations_Summary view.
    /// Shows donor contributions by event.
    /// </summary>
    [HttpGet("donations/summary")]
    public async Task<ActionResult<IEnumerable<DonationsSummaryDto>>> GetDonationsSummaryFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwDonationsSummary
            .AsNoTracking()
            .Select(v => new DonationsSummaryDto
            {
                DonationId = v.DonationId,
                DonorId = v.DonorId,
                EventId = v.EventId,
                Amount = v.Amount,
                DonationDate = v.DonationDate,
                Status = v.Status
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get expenses summary from vw_Expenses_Summary view.
    /// Shows expense breakdown by category and event.
    /// </summary>
    [HttpGet("expenses/summary")]
    public async Task<ActionResult<IEnumerable<ExpensesSummaryDto>>> GetExpensesSummaryFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwExpensesSummary
            .AsNoTracking()
            .Select(v => new ExpensesSummaryDto
            {
                ExpenseId = v.ExpenseId,
                EventId = v.EventId,
                Category = v.Category,
                Amount = v.Amount,
                ExpenseDate = v.ExpenseDate,
                PaymentStatus = v.PaymentStatus,
                ApprovedBy = v.ApprovedBy
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get budget per event from vw_Budget_PerEvent view.
    /// Shows financial summary and balance per event.
    /// </summary>
    [HttpGet("budget/per-event")]
    public async Task<ActionResult<IEnumerable<BudgetPerEventDto>>> GetBudgetPerEventFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwBudgetPerEvent
            .AsNoTracking()
            .Select(v => new BudgetPerEventDto
            {
                EventId = v.EventId,
                EventName = v.EventName,
                TotalDonations = v.TotalDonations,
                TotalExpenses = v.TotalExpenses,
                NetBudget = v.NetBudget
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Approve an expense using stored procedure (sp_ApproveExpense).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("expenses/{expenseId:int}/approve-sp")]
    public async Task<ActionResult<ExpenseApprovalResult>> ApproveExpenseStoredProc(
        int expenseId,
        [FromBody] ApprovalDecisionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Users.AnyAsync(user => user.UserId == request.ActionBy, cancellationToken))
            {
                return NotFound($"User {request.ActionBy} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@ExpenseID", expenseId),
                new SqlParameter("@ReviewedBy", request.ActionBy),
                new SqlParameter("@Comments", (object?)request.Comments ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_ApproveExpense", parameters);

            if (result.ContainsKey("ResultStatus") && result.ContainsKey("ExpenseID"))
            {
                var resultObj = new ExpenseApprovalResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    ExpenseID = (int)(result["ExpenseID"] ?? 0)
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
    /// Reject an expense using stored procedure (sp_RejectExpense).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("expenses/{expenseId:int}/reject-sp")]
    public async Task<ActionResult<ExpenseApprovalResult>> RejectExpenseStoredProc(
        int expenseId,
        [FromBody] ApprovalDecisionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Users.AnyAsync(user => user.UserId == request.ActionBy, cancellationToken))
            {
                return NotFound($"User {request.ActionBy} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@ExpenseID", expenseId),
                new SqlParameter("@ReviewedBy", request.ActionBy),
                new SqlParameter("@Comments", (object?)request.Comments ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_RejectExpense", parameters);

            if (result.ContainsKey("ResultStatus") && result.ContainsKey("ExpenseID"))
            {
                var resultObj = new ExpenseApprovalResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    ExpenseID = (int)(result["ExpenseID"] ?? 0)
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

public class DonorDto
{
    public int DonorId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string DonorType { get; set; } = string.Empty;

    public string? OrganizationName { get; set; }

    public string? Email { get; set; }

    public string Street { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;
}

public class DonorCreateDto
{
    [Required]
    [MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string DonorType { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? OrganizationName { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [Required]
    [MaxLength(150)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Area { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Province { get; set; } = string.Empty;
}

public class DonationDto
{
    public int DonationId { get; set; }

    public int DonorId { get; set; }

    public string DonorName { get; set; } = string.Empty;

    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime DonationDate { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? ReceiptNumber { get; set; }
}

public class DonationCreateDto
{
    [Range(1, int.MaxValue)]
    public int DonorId { get; set; }

    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    public DateTime? DonationDate { get; set; }

    [Required]
    [MaxLength(30)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Pending";

    [MaxLength(80)]
    public string? ReceiptNumber { get; set; }
}

public class DonationStatusUpdateDto
{
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;
}

public class ExpenseDto
{
    public int ExpenseId { get; set; }

    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public int? ApprovedBy { get; set; }

    public string? ApprovedByName { get; set; }

    public string Category { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime ExpenseDate { get; set; }

    public string PaymentStatus { get; set; } = string.Empty;
}

public class ExpenseCreateDto
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    public int? ApprovedBy { get; set; }

    [Required]
    [MaxLength(40)]
    public string Category { get; set; } = string.Empty;

    public bool RequiresApproval { get; set; }

    public int? ApprovalRequestedBy { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime? ExpenseDate { get; set; }

    [Required]
    [MaxLength(30)]
    public string PaymentStatus { get; set; } = string.Empty;
}

public class ExpensePaymentStatusUpdateDto
{
    [Required]
    [MaxLength(30)]
    public string PaymentStatus { get; set; } = string.Empty;
}

public class ExpenseApprovalResult
{
    public string ResultStatus { get; set; } = string.Empty;
    public int ExpenseID { get; set; }
}
