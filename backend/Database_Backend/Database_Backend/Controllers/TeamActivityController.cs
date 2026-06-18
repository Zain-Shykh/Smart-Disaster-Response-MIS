using Database_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TeamActivityController : ControllerBase
{
    private readonly DatabaseProjectContext _context;

    public TeamActivityController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "EmergencyOperator,FieldOfficer")]
    public async Task<ActionResult<TeamActivityResponseDto>> CreateActivity(
        [FromBody] TeamActivityCreateDto request,
        CancellationToken cancellationToken)
    {
        var team = await _context.RescueTeams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TeamId == request.TeamId, cancellationToken);

        if (team is null)
        {
            return NotFound($"Team {request.TeamId} was not found.");
        }

        if (request.StartTime > DateTime.Now)
        {
            return BadRequest("StartTime cannot be in the future.");
        }

        if (request.EndTime.HasValue)
        {
            if (request.EndTime.Value > DateTime.Now)
            {
                return BadRequest("EndTime cannot be in the future.");
            }

            if (request.EndTime.Value < request.StartTime)
            {
                return BadRequest("EndTime must be greater than or equal to StartTime.");
            }
        }

        var nextActivityId = await _context.TeamActivities
            .Where(item => item.TeamId == request.TeamId)
            .Select(item => (int?)item.ActivityId)
            .MaxAsync(cancellationToken) ?? 0;

        var entity = new TeamActivity
        {
            TeamId = request.TeamId,
            ActivityId = nextActivityId + 1,
            ActivityType = request.ActivityType.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Outcome = string.IsNullOrWhiteSpace(request.Outcome) ? null : request.Outcome.Trim()
        };

        _context.TeamActivities.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetActivities),
            new { teamId = entity.TeamId },
            MapToResponse(entity));
    }

    [HttpGet]
    [Authorize(Roles = "Administrator,EmergencyOperator,FieldOfficer")]
    public async Task<ActionResult<IEnumerable<TeamActivityResponseDto>>> GetActivities(
        [FromQuery] TeamActivityQueryDto query,
        CancellationToken cancellationToken)
    {
        IQueryable<TeamActivity> activities = _context.TeamActivities.AsNoTracking();

        if (query.TeamId.HasValue)
        {
            activities = activities.Where(item => item.TeamId == query.TeamId.Value);
        }

        if (query.StartDate.HasValue)
        {
            activities = activities.Where(item => item.StartTime >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            activities = activities.Where(item => item.StartTime <= query.EndDate.Value);
        }

        var result = await activities
            .OrderByDescending(item => item.StartTime)
            .Select(item => new TeamActivityResponseDto
            {
                TeamId = item.TeamId,
                ActivityId = item.ActivityId,
                ActivityType = item.ActivityType,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                DurationMinutes = item.DurationMinutes,
                Notes = item.Notes,
                Outcome = item.Outcome
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("summary/{teamId:int}")]
    [Authorize(Roles = "Administrator,EmergencyOperator,FieldOfficer")]
    public async Task<ActionResult<TeamActivitySummaryDto>> GetSummary(int teamId, CancellationToken cancellationToken)
    {
        if (!await _context.RescueTeams.AnyAsync(item => item.TeamId == teamId, cancellationToken))
        {
            return NotFound($"Team {teamId} was not found.");
        }

        var activities = await _context.TeamActivities
            .AsNoTracking()
            .Where(item => item.TeamId == teamId)
            .ToListAsync(cancellationToken);

        var completedCount = activities.Count(item => item.EndTime.HasValue);
        var pendingCount = activities.Count - completedCount;

        return Ok(new TeamActivitySummaryDto
        {
            TeamId = teamId,
            TotalActivities = activities.Count,
            CompletedActivities = completedCount,
            PendingActivities = pendingCount,
            LastActivityAt = activities.OrderByDescending(item => item.StartTime).Select(item => (DateTime?)item.StartTime).FirstOrDefault()
        });
    }

    private static TeamActivityResponseDto MapToResponse(TeamActivity item)
    {
        return new TeamActivityResponseDto
        {
            TeamId = item.TeamId,
            ActivityId = item.ActivityId,
            ActivityType = item.ActivityType,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            DurationMinutes = item.DurationMinutes,
            Notes = item.Notes,
            Outcome = item.Outcome
        };
    }

    /// <summary>
    /// Complete a team assignment using stored procedure (sp_CompleteAssignment).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("assignments/{assignmentId:int}/complete-sp")]
    [Authorize(Roles = "EmergencyOperator,FieldOfficer")]
    public async Task<ActionResult<AssignmentCompletionResult>> CompleteAssignmentStoredProc(
        int assignmentId,
        [FromBody] AssignmentCompletionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await _context.TeamAssignments.FirstOrDefaultAsync(a => a.AssignmentId == assignmentId, cancellationToken);
            if (assignment is null)
            {
                return NotFound($"Assignment {assignmentId} was not found.");
            }

            if (!await _context.Users.AnyAsync(u => u.UserId == request.CompletedBy, cancellationToken))
            {
                return NotFound($"User {request.CompletedBy} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@AssignmentID", assignmentId),
                new SqlParameter("@CompletedBy", request.CompletedBy),
                new SqlParameter("@CompletionNotes", (object?)request.CompletionNotes ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_CompleteAssignment", parameters);

            if (result.ContainsKey("ResultStatus") && result.ContainsKey("AssignmentID"))
            {
                var resultObj = new AssignmentCompletionResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    AssignmentID = (int)(result["AssignmentID"] ?? 0)
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

public class TeamActivityCreateDto
{
    [Range(1, int.MaxValue)]
    public int TeamId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ActivityType { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? Outcome { get; set; }
}

public class TeamActivityQueryDto
{
    public int? TeamId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

public class TeamActivityResponseDto
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

public class TeamActivitySummaryDto
{
    public int TeamId { get; set; }

    public int TotalActivities { get; set; }

    public int CompletedActivities { get; set; }

    public int PendingActivities { get; set; }

    public DateTime? LastActivityAt { get; set; }
}

// ============================================================================
// STORED PROCEDURE DTOs
// ============================================================================

public class AssignmentCompletionDto
{
    [Range(1, int.MaxValue)]
    public int CompletedBy { get; set; }

    [MaxLength(1000)]
    public string? CompletionNotes { get; set; }
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
