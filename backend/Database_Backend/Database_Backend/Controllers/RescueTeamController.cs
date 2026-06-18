using Database_Backend.Models;
using Database_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator,EmergencyOperator,FieldOfficer,Field Officer")]
public class RescueTeamController : ControllerBase
{
    private static readonly HashSet<string> AllowedTeamTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Medical",
        "Fire",
        "Rescue",
        "Search"
    };

    private static readonly HashSet<string> AllowedAvailabilityStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Available",
        "Assigned",
        "Busy",
        "Completed"
    };

    private static readonly HashSet<string> AllowedAssignmentStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Assigned",
        "EnRoute",
        "OnSite",
        "Completed"
    };

    private readonly DatabaseProjectContext _context;

    public RescueTeamController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RescueTeamResponseDto>>> GetRescueTeams(
        [FromQuery] RescueTeamQueryDto query,
        CancellationToken cancellationToken)
    {
        IQueryable<RescueTeam> teams = _context.RescueTeams.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.TeamType))
        {
            var teamType = NormalizeTeamType(query.TeamType);
            if (teamType is null)
            {
                return BadRequest("TeamType must be one of: Medical, Fire, Rescue, Search.");
            }

            teams = teams.Where(team => team.TeamType == teamType);
        }

        if (!string.IsNullOrWhiteSpace(query.AvailabilityStatus))
        {
            var availabilityStatus = NormalizeAvailabilityStatus(query.AvailabilityStatus);
            if (availabilityStatus is null)
            {
                return BadRequest("AvailabilityStatus must be one of: Available, Assigned, Busy, Completed.");
            }

            teams = teams.Where(team => team.AvailabilityStatus == availabilityStatus);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            teams = teams.Where(team => team.City == city);
        }

        var result = await teams
            .OrderBy(team => team.TeamName)
            .Select(team => new RescueTeamResponseDto
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                TeamType = team.TeamType,
                Street = team.Street,
                Area = team.Area,
                City = team.City,
                Province = team.Province,
                Latitude = team.Latitude,
                Longitude = team.Longitude,
                AvailabilityStatus = team.AvailabilityStatus,
                Capacity = team.Capacity,
                TotalAssignments = team.TotalAssignments
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RescueTeamResponseDto>> GetRescueTeam(int id, CancellationToken cancellationToken)
    {
        var team = await _context.RescueTeams.AsNoTracking().FirstOrDefaultAsync(item => item.TeamId == id, cancellationToken);
        if (team is null)
        {
            return NotFound();
        }

        return Ok(MapToTeamResponse(team));
    }

    [HttpPost]
    public async Task<ActionResult<RescueTeamResponseDto>> CreateRescueTeam(
        [FromBody] RescueTeamCreateDto request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateTeamRequest(request.TeamName, request.TeamType, request.Street, request.Area, request.City, request.Province, request.AvailabilityStatus, request.Capacity);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var team = new RescueTeam
        {
            TeamName = request.TeamName.Trim(),
            TeamType = NormalizeTeamType(request.TeamType)!,
            Street = request.Street.Trim(),
            Area = request.Area.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AvailabilityStatus = NormalizeAvailabilityStatus(request.AvailabilityStatus)!,
            Capacity = request.Capacity,
            TotalAssignments = 0
        };

        _context.RescueTeams.Add(team);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The rescue team could not be saved.");
        }

        return CreatedAtAction(nameof(GetRescueTeam), new { id = team.TeamId }, MapToTeamResponse(team));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RescueTeamResponseDto>> UpdateRescueTeam(
        int id,
        [FromBody] RescueTeamUpdateDto request,
        CancellationToken cancellationToken)
    {
        var team = await _context.RescueTeams.FirstOrDefaultAsync(item => item.TeamId == id, cancellationToken);
        if (team is null)
        {
            return NotFound();
        }

        var validationError = ValidateTeamRequest(request.TeamName, request.TeamType, request.Street, request.Area, request.City, request.Province, request.AvailabilityStatus, request.Capacity);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        team.TeamName = request.TeamName.Trim();
        team.TeamType = NormalizeTeamType(request.TeamType)!;
        team.Street = request.Street.Trim();
        team.Area = request.Area.Trim();
        team.City = request.City.Trim();
        team.Province = request.Province.Trim();
        team.Latitude = request.Latitude;
        team.Longitude = request.Longitude;
        team.AvailabilityStatus = NormalizeAvailabilityStatus(request.AvailabilityStatus)!;
        team.Capacity = request.Capacity;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The rescue team could not be updated.");
        }

        return Ok(MapToTeamResponse(team));
    }

    [HttpPatch("{id:int}/availability")]
    public async Task<ActionResult<RescueTeamResponseDto>> UpdateAvailabilityStatus(
        int id,
        [FromBody] RescueTeamAvailabilityUpdateDto request,
        CancellationToken cancellationToken)
    {
        var team = await _context.RescueTeams.FirstOrDefaultAsync(item => item.TeamId == id, cancellationToken);
        if (team is null)
        {
            return NotFound();
        }

        var availabilityStatus = NormalizeAvailabilityStatus(request.AvailabilityStatus);
        if (availabilityStatus is null)
        {
            return BadRequest("AvailabilityStatus must be one of: Available, Assigned, Busy, Completed.");
        }

        var currentToken = ComputeVersionToken(team);
        if (!string.IsNullOrWhiteSpace(request.VersionToken) && !string.Equals(request.VersionToken, currentToken, StringComparison.Ordinal))
        {
            return Conflict(new RescueTeamConcurrencyConflictDto
            {
                Message = "Rescue team availability was modified by another operation.",
                CurrentVersionToken = currentToken
            });
        }

        team.AvailabilityStatus = availabilityStatus;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The rescue team availability could not be updated.");
        }

        return Ok(MapToTeamResponse(team));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRescueTeam(int id, CancellationToken cancellationToken)
    {
        var team = await _context.RescueTeams.FirstOrDefaultAsync(item => item.TeamId == id, cancellationToken);
        if (team is null)
        {
            return NotFound();
        }

        _context.RescueTeams.Remove(team);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The rescue team could not be deleted.");
        }

        return NoContent();
    }

    [HttpGet("{teamId:int}/assignments")]
    public async Task<ActionResult<IEnumerable<TeamAssignmentResponseDto>>> GetAssignmentsForTeam(
        int teamId,
        CancellationToken cancellationToken)
    {
        var assignments = await _context.TeamAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Event)
            .Include(assignment => assignment.AssignedByNavigation)
            .Where(assignment => assignment.TeamId == teamId)
            .OrderByDescending(assignment => assignment.AssignmentTime)
            .Select(assignment => MapToAssignmentResponse(assignment))
            .ToListAsync(cancellationToken);

        return Ok(assignments);
    }

    [HttpGet("recommendations")]
    public async Task<ActionResult<IEnumerable<RescueTeamRecommendationDto>>> GetAssignmentRecommendations(
        [FromQuery] int reportId,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (reportId <= 0)
        {
            return BadRequest("reportId must be greater than zero.");
        }

        if (limit < 1 || limit > 20)
        {
            return BadRequest("limit must be between 1 and 20.");
        }

        var report = await _context.EmergencyReports
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ReportId == reportId, cancellationToken);

        if (report is null)
        {
            return NotFound($"Emergency report {reportId} was not found.");
        }

        if (string.IsNullOrWhiteSpace(report.City))
        {
            return BadRequest("Emergency report must include a city for recommendation.");
        }

        var reportCity = report.City.Trim();
        var candidates = await _context.RescueTeams
            .AsNoTracking()
            .Where(item => item.Capacity > 0)
            .ToListAsync(cancellationToken);

        var sameCityCandidates = candidates
            .Where(item => !string.IsNullOrWhiteSpace(item.City)
                && item.City.Trim().Equals(reportCity, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (sameCityCandidates.Count == 0)
        {
            return NotFound("No rescue teams with capacity are available in the same city for recommendation.");
        }

        var preferredTeamType = GuessPreferredTeamType(report.DisasterType);
        var severityMultiplier = GetSeverityMultiplier(report.SeverityLevel);

        var ranked = sameCityCandidates
            .Select(team =>
            {
                var cityScore = 1.0;
                var availabilityScore = GetAvailabilityScore(team.AvailabilityStatus);
                var capacityScore = Math.Min(team.Capacity / 20.0, 1.0);
                var teamTypeScore = GetTeamTypeScore(team.TeamType, preferredTeamType);

                var priorityScore = ((cityScore * 0.5) + (availabilityScore * 0.25) + (capacityScore * 0.15) + (teamTypeScore * 0.10))
                    * 100
                    * severityMultiplier;

                return new RescueTeamRecommendationDto
                {
                    TeamId = team.TeamId,
                    TeamName = team.TeamName,
                    TeamType = team.TeamType,
                    AvailabilityStatus = team.AvailabilityStatus,
                    Capacity = team.Capacity,
                    DistanceKm = 0,
                    PriorityScore = Math.Round(priorityScore, 2),
                    PreferredTeamType = preferredTeamType,
                    SeverityLevel = report.SeverityLevel,
                    RecommendationReason = BuildRecommendationReason(team, preferredTeamType, cityScore)
                };
            })
            .OrderByDescending(item => item.PriorityScore)
            .ThenBy(item => item.TeamId)
            .Take(limit)
            .ToList();

        return Ok(ranked);
    }

    [HttpPost("{teamId:int}/assignments")]
    public async Task<ActionResult<TeamAssignmentResponseDto>> CreateAssignment(
        int teamId,
        [FromBody] TeamAssignmentCreateDto request,
        CancellationToken cancellationToken)
    {
        if ((request.EventId ?? 0) <= 0 && (request.ReportId ?? 0) <= 0)
        {
            return BadRequest("eventId or reportId must be greater than zero.");
        }

        var team = await _context.RescueTeams.FirstOrDefaultAsync(item => item.TeamId == teamId, cancellationToken);
        if (team is null)
        {
            return NotFound($"Rescue team {teamId} was not found.");
        }

        int eventId;
        if ((request.EventId ?? 0) > 0)
        {
            eventId = request.EventId!.Value;
        }
        else
        {
            var sourceReport = await _context.EmergencyReports.FirstOrDefaultAsync(item => item.ReportId == request.ReportId, cancellationToken);
            if (sourceReport is null)
            {
                return NotFound($"Emergency report {request.ReportId} was not found.");
            }

            if (sourceReport.EventId is null || sourceReport.EventId <= 0)
            {
                return BadRequest($"Emergency report {request.ReportId} is not linked to a disaster event.");
            }

            eventId = sourceReport.EventId.Value;
        }

        var disasterEvent = await _context.DisasterEvents.FirstOrDefaultAsync(item => item.EventId == eventId, cancellationToken);
        if (disasterEvent is null)
        {
            return NotFound($"Disaster event {eventId} was not found.");
        }

        if (team.Capacity <= 0)
        {
            return BadRequest("Selected rescue team has no available capacity.");
        }

        if (team.AvailabilityStatus.Equals("Busy", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Selected rescue team is Busy and cannot take new assignments.");
        }

        if (string.IsNullOrWhiteSpace(team.City) || string.IsNullOrWhiteSpace(disasterEvent.City))
        {
            return BadRequest("Team and disaster event must both include city for assignment validation.");
        }

        if (!team.City.Trim().Equals(disasterEvent.City.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest($"Assignment requires same city. Team city '{team.City}' does not match event city '{disasterEvent.City}'.");
        }

        if (!await _context.Users.AnyAsync(user => user.UserId == request.AssignedBy, cancellationToken))
        {
            return NotFound($"User {request.AssignedBy} was not found.");
        }

        if (NormalizeAssignmentStatus(request.Status) is null)
        {
            return BadRequest("Status must be one of: Assigned, EnRoute, OnSite, Completed.");
        }

        var approvalRequestedBy = request.ApprovalRequestedBy ?? request.AssignedBy;
        if (request.RequiresApproval)
        {
            if (!await _context.Users.AnyAsync(user => user.UserId == approvalRequestedBy, cancellationToken))
            {
                return NotFound($"Approval requested-by user {approvalRequestedBy} was not found.");
            }
        }

        var normalizedStatus = request.RequiresApproval ? "Assigned" : NormalizeAssignmentStatus(request.Status)!;

        var assignment = new TeamAssignment
        {
            TeamId = teamId,
            EventId = eventId,
            AssignedBy = request.AssignedBy,
            AssignmentTime = request.AssignmentTime ?? DateTime.Now,
            CompletionTime = request.CompletionTime,
            Status = normalizedStatus
        };

        _context.TeamAssignments.Add(assignment);

        IDbContextTransaction? transaction = null;
        try
        {
            if (!string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
            {
                transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (request.RequiresApproval)
            {
                var approvalRequest = new ApprovalRequest
                {
                    RequestedBy = approvalRequestedBy,
                    RequestType = "RescueDeployment",
                    RequestTime = DateTime.Now,
                    Status = "Pending",
                    Description = $"Approval required for assignment {assignment.AssignmentId}.",
                    AssignmentId = assignment.AssignmentId
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

            return Conflict("The team assignment could not be saved.");
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }

        var createdAssignment = await _context.TeamAssignments
            .AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.AssignedByNavigation)
            .FirstAsync(item => item.AssignmentId == assignment.AssignmentId, cancellationToken);

        return CreatedAtAction(nameof(GetAssignment), new { teamId, assignmentId = assignment.AssignmentId }, MapToAssignmentResponse(createdAssignment));
    }

    [HttpGet("{teamId:int}/assignments/{assignmentId:int}")]
    public async Task<ActionResult<TeamAssignmentResponseDto>> GetAssignment(
        int teamId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var assignment = await _context.TeamAssignments
            .AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.AssignedByNavigation)
            .FirstOrDefaultAsync(item => item.TeamId == teamId && item.AssignmentId == assignmentId, cancellationToken);

        if (assignment is null)
        {
            return NotFound();
        }

        return Ok(MapToAssignmentResponse(assignment));
    }

    [HttpPatch("{teamId:int}/assignments/{assignmentId:int}/status")]
    public async Task<ActionResult<TeamAssignmentResponseDto>> UpdateAssignmentStatus(
        int teamId,
        int assignmentId,
        [FromBody] TeamAssignmentStatusUpdateDto request,
        CancellationToken cancellationToken)
    {
        var assignment = await _context.TeamAssignments.FirstOrDefaultAsync(item => item.TeamId == teamId && item.AssignmentId == assignmentId, cancellationToken);
        if (assignment is null)
        {
            return NotFound();
        }

        var status = NormalizeAssignmentStatus(request.Status);
        if (status is null)
        {
            return BadRequest("Status must be one of: Assigned, EnRoute, OnSite, Completed.");
        }

        if (!status.Equals("Assigned", StringComparison.OrdinalIgnoreCase)
            && !await HasApprovedAssignmentRequestAsync(assignmentId, cancellationToken))
        {
            return BadRequest("Assignment cannot transition beyond Assigned until an approval request is approved.");
        }

        assignment.Status = status;

        if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && assignment.CompletionTime is null)
        {
            assignment.CompletionTime = DateTime.Now;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The team assignment status could not be updated.");
        }

        var updatedAssignment = await _context.TeamAssignments
            .AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.AssignedByNavigation)
            .FirstAsync(item => item.TeamId == teamId && item.AssignmentId == assignmentId, cancellationToken);

        return Ok(MapToAssignmentResponse(updatedAssignment));
    }

    [HttpDelete("{teamId:int}/assignments/{assignmentId:int}")]
    public async Task<IActionResult> DeleteAssignment(
        int teamId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var assignment = await _context.TeamAssignments.FirstOrDefaultAsync(item => item.TeamId == teamId && item.AssignmentId == assignmentId, cancellationToken);
        if (assignment is null)
        {
            return NotFound();
        }

        _context.TeamAssignments.Remove(assignment);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The team assignment could not be deleted.");
        }

        return NoContent();
    }

    private static string? NormalizeTeamType(string? teamType)
    {
        if (string.IsNullOrWhiteSpace(teamType) || !AllowedTeamTypes.Contains(teamType.Trim()))
        {
            return null;
        }

        return AllowedTeamTypes.First(item => item.Equals(teamType.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeAvailabilityStatus(string? availabilityStatus)
    {
        if (string.IsNullOrWhiteSpace(availabilityStatus) || !AllowedAvailabilityStatuses.Contains(availabilityStatus.Trim()))
        {
            return null;
        }

        return AllowedAvailabilityStatuses.First(item => item.Equals(availabilityStatus.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeAssignmentStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || !AllowedAssignmentStatuses.Contains(status.Trim()))
        {
            return null;
        }

        return AllowedAssignmentStatuses.First(item => item.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? GuessPreferredTeamType(string? disasterType)
    {
        if (string.IsNullOrWhiteSpace(disasterType))
        {
            return null;
        }

        return disasterType.Trim().ToLowerInvariant() switch
        {
            "fire" => "Fire",
            "wildfire" => "Fire",
            "earthquake" => "Rescue",
            "flood" => "Rescue",
            "landslide" => "Rescue",
            "building collapse" => "Rescue",
            "medical" => "Medical",
            "epidemic" => "Medical",
            _ => null
        };
    }

    private static double GetSeverityMultiplier(string? severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
        {
            return 1.0;
        }

        return severity.Trim().ToLowerInvariant() switch
        {
            "critical" => 1.30,
            "high" => 1.15,
            "medium" => 1.00,
            "low" => 0.90,
            _ => 1.00
        };
    }

    private static double GetAvailabilityScore(string availabilityStatus)
    {
        return availabilityStatus.Trim().ToLowerInvariant() switch
        {
            "available" => 1.00,
            "completed" => 0.80,
            "assigned" => 0.55,
            "busy" => 0.25,
            _ => 0.20
        };
    }

    private static double GetTeamTypeScore(string teamType, string? preferredTeamType)
    {
        if (string.IsNullOrWhiteSpace(preferredTeamType))
        {
            return 0.60;
        }

        return teamType.Equals(preferredTeamType, StringComparison.OrdinalIgnoreCase) ? 1.00 : 0.35;
    }

    private static string BuildRecommendationReason(RescueTeam team, string? preferredTeamType, double cityScore)
    {
        var typeMatch = string.IsNullOrWhiteSpace(preferredTeamType)
            ? "No strict team-type preference"
            : team.TeamType.Equals(preferredTeamType, StringComparison.OrdinalIgnoreCase)
                ? $"Team type matches preferred {preferredTeamType}"
                : $"Team type differs from preferred {preferredTeamType}";

        var locationMatch = cityScore == 1.0 ? "Same city" : "Different city";

        return $"{typeMatch}; {locationMatch}; availability={team.AvailabilityStatus}; capacity={team.Capacity}.";
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;

        static double ToRadians(double value) => value * Math.PI / 180.0;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2)
            + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static string? ValidateTeamRequest(
        string teamName,
        string teamType,
        string street,
        string area,
        string city,
        string province,
        string availabilityStatus,
        int capacity)
    {
        if (string.IsNullOrWhiteSpace(teamName) || string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(area) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(province))
        {
            return "TeamName, Street, Area, City, and Province are required.";
        }

        if (NormalizeTeamType(teamType) is null)
        {
            return "TeamType must be one of: Medical, Fire, Rescue, Search.";
        }

        if (NormalizeAvailabilityStatus(availabilityStatus) is null)
        {
            return "AvailabilityStatus must be one of: Available, Assigned, Busy, Completed.";
        }

        if (capacity < 0)
        {
            return "Capacity cannot be negative.";
        }

        return null;
    }

    private static RescueTeamResponseDto MapToTeamResponse(RescueTeam team)
    {
        return new RescueTeamResponseDto
        {
            TeamId = team.TeamId,
            TeamName = team.TeamName,
            TeamType = team.TeamType,
            Street = team.Street,
            Area = team.Area,
            City = team.City,
            Province = team.Province,
            Latitude = team.Latitude,
            Longitude = team.Longitude,
            AvailabilityStatus = team.AvailabilityStatus,
            Capacity = team.Capacity,
            TotalAssignments = team.TotalAssignments,
            VersionToken = ComputeVersionToken(team)
        };
    }

    private static string ComputeVersionToken(RescueTeam team)
    {
        return ConcurrencyTokenService.Compute(
            team.TeamId,
            team.TeamName,
            team.TeamType,
            team.Street,
            team.Area,
            team.City,
            team.Province,
            team.Latitude,
            team.Longitude,
            team.AvailabilityStatus,
            team.Capacity,
            team.TotalAssignments);
    }

    private static TeamAssignmentResponseDto MapToAssignmentResponse(TeamAssignment assignment)
    {
        return new TeamAssignmentResponseDto
        {
            AssignmentId = assignment.AssignmentId,
            TeamId = assignment.TeamId,
            TeamName = assignment.Team?.TeamName,
            ReportId = assignment.EventId,
            ReportCity = assignment.Event?.City,
            ReportDisasterType = assignment.Event?.DisasterType,
            AssignedBy = assignment.AssignedBy,
            AssignedByName = assignment.AssignedByNavigation is null ? null : $"{assignment.AssignedByNavigation.FirstName} {assignment.AssignedByNavigation.LastName}",
            AssignmentTime = assignment.AssignmentTime,
            CompletionTime = assignment.CompletionTime,
            Status = assignment.Status
        };
    }

    /// <summary>
    /// Get team availability status from vw_Teams_Availability view.
    /// Shows real-time team status for assignment decisions.
    /// </summary>
    [HttpGet("availability")]
    public async Task<ActionResult<IEnumerable<TeamAvailabilityDto>>> GetTeamsAvailabilityFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwTeamsAvailability
            .AsNoTracking()
            .Select(v => new TeamAvailabilityDto
            {
                TeamId = v.TeamId,
                TeamName = v.TeamName,
                TeamType = v.TeamType,
                AvailabilityStatus = v.AvailabilityStatus,
                Capacity = v.Capacity,
                TotalAssignments = v.TotalAssignments,
                Latitude = v.Latitude,
                Longitude = v.Longitude
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get detailed assignment information from vw_Assignments_Detail view.
    /// Shows assignment history with location details.
    /// </summary>
    [HttpGet("assignments/details")]
    public async Task<ActionResult<IEnumerable<AssignmentDetailDto>>> GetAssignmentsDetailFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwAssignmentsDetail
            .AsNoTracking()
            .Select(v => new AssignmentDetailDto
            {
                AssignmentId = v.AssignmentId,
                TeamId = v.TeamId,
                ReportId = v.ReportId,
                ReportLocation = v.ReportLocation,
                AssignedBy = v.AssignedBy,
                AssignmentTime = v.AssignmentTime,
                CompletionTime = v.CompletionTime,
                Status = v.Status
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get team activity log from vw_TeamActivity_Log view.
    /// Shows historical activity for audit and analytics.
    /// </summary>
    [HttpGet("{teamId:int}/activity-log")]
    public async Task<ActionResult<IEnumerable<TeamActivityLogDto>>> GetTeamActivityLogFromView(
        int teamId,
        CancellationToken cancellationToken)
    {
        if (!await _context.RescueTeams.AnyAsync(t => t.TeamId == teamId, cancellationToken))
        {
            return NotFound($"Team {teamId} was not found.");
        }

        var result = await _context.VwTeamActivityLog
            .AsNoTracking()
            .Where(v => v.TeamId == teamId)
            .Select(v => new TeamActivityLogDto
            {
                TeamId = v.TeamId,
                ActivityId = v.ActivityId,
                ActivityType = v.ActivityType,
                StartTime = v.StartTime,
                EndTime = v.EndTime,
                DurationMinutes = v.DurationMinutes,
                Notes = v.Notes,
                Outcome = v.Outcome
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    private async Task<bool> HasApprovedAssignmentRequestAsync(int assignmentId, CancellationToken cancellationToken)
    {
        return await _context.ApprovalRequests
            .AsNoTracking()
            .AnyAsync(item => item.AssignmentId == assignmentId
                && item.RequestType == "RescueDeployment"
                && item.Status == "Approved", cancellationToken);
    }

    /// <summary>
    /// Assign a team to an emergency report using stored procedure (sp_AssignTeam).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("{teamId:int}/assign-team-sp")]
    public async Task<ActionResult<TeamAssignmentResult>> AssignTeamStoredProc(
        int teamId,
        [FromBody] TeamAssignmentSpRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.RescueTeams.AnyAsync(team => team.TeamId == teamId, cancellationToken))
            {
                return NotFound($"Rescue team {teamId} was not found.");
            }

            if (!await _context.EmergencyReports.AnyAsync(report => report.ReportId == request.ReportID, cancellationToken))
            {
                return NotFound($"Emergency report {request.ReportID} was not found.");
            }

            if (!await _context.Users.AnyAsync(user => user.UserId == request.AssignedBy, cancellationToken))
            {
                return NotFound($"User {request.AssignedBy} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@TeamID", teamId),
                new SqlParameter("@ReportID", request.ReportID),
                new SqlParameter("@AssignedBy", request.AssignedBy)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_AssignTeam", parameters);

            if (result.ContainsKey("ResultStatus") && result.ContainsKey("AssignmentID"))
            {
                var resultObj = new TeamAssignmentResult
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

    /// <summary>
    /// Approve a team deployment using stored procedure (sp_ApproveDeployment).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("{teamId:int}/assignments/{assignmentId:int}/approve-deployment-sp")]
    public async Task<ActionResult<DeploymentApprovalResult>> ApproveDeploymentStoredProc(
        int teamId,
        int assignmentId,
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
                new SqlParameter("@AssignmentID", assignmentId),
                new SqlParameter("@ReviewedBy", request.ActionBy),
                new SqlParameter("@Comments", (object?)request.Comments ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_ApproveDeployment", parameters);

            if (result.ContainsKey("ResultStatus") && result.ContainsKey("AssignmentID") && result.ContainsKey("RequestID"))
            {
                var resultObj = new DeploymentApprovalResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    AssignmentID = (int)(result["AssignmentID"] ?? 0),
                    RequestID = (int)(result["RequestID"] ?? 0)
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
    /// Reject a team deployment using stored procedure (sp_RejectDeployment).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("{teamId:int}/assignments/{assignmentId:int}/reject-deployment-sp")]
    public async Task<ActionResult<DeploymentApprovalResult>> RejectDeploymentStoredProc(
        int teamId,
        int assignmentId,
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
                new SqlParameter("@AssignmentID", assignmentId),
                new SqlParameter("@ReviewedBy", request.ActionBy),
                new SqlParameter("@Comments", (object?)request.Comments ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_RejectDeployment", parameters);

            if (result.ContainsKey("ResultStatus") && result.ContainsKey("AssignmentID") && result.ContainsKey("RequestID"))
            {
                var resultObj = new DeploymentApprovalResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    AssignmentID = (int)(result["AssignmentID"] ?? 0),
                    RequestID = (int)(result["RequestID"] ?? 0)
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

public class RescueTeamUpdateDto
{
    [Required]
    [MaxLength(150)]
    public string TeamName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TeamType { get; set; } = string.Empty;

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

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [Required]
    [MaxLength(30)]
    public string AvailabilityStatus { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    public string? VersionToken { get; set; }
}

public class RescueTeamQueryDto
{
    public string? TeamType { get; set; }
    public string? AvailabilityStatus { get; set; }
    public string? City { get; set; }
}

public class RescueTeamCreateDto
{
    [Required]
    [MaxLength(150)]
    public string TeamName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TeamType { get; set; } = string.Empty;

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

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    [Required]
    [MaxLength(30)]
    public string AvailabilityStatus { get; set; } = "Available";

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
}

public class TeamAssignmentSpRequestDto
{
    [Range(1, int.MaxValue)]
    public int ReportID { get; set; }

    [Range(1, int.MaxValue)]
    public int AssignedBy { get; set; }
}

public class RescueTeamAvailabilityUpdateDto
{
    [Required]
    [MaxLength(30)]
    public string AvailabilityStatus { get; set; } = string.Empty;

    public string? VersionToken { get; set; }
}

public class TeamAssignmentCreateDto
{
    [Range(1, int.MaxValue)]
    public int? EventId { get; set; }

    [Range(1, int.MaxValue)]
    public int? ReportId { get; set; }

    [Range(1, int.MaxValue)]
    public int AssignedBy { get; set; }

    public DateTime? AssignmentTime { get; set; }

    public DateTime? CompletionTime { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Assigned";

    public bool RequiresApproval { get; set; }

    [Range(1, int.MaxValue)]
    public int? ApprovalRequestedBy { get; set; }
}

public class TeamAssignmentStatusUpdateDto
{
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;
}

public class RescueTeamResponseDto
{
    public int TeamId { get; set; }

    public string TeamName { get; set; } = string.Empty;

    public string TeamType { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string AvailabilityStatus { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int TotalAssignments { get; set; }

    public string VersionToken { get; set; } = string.Empty;
}

public class RescueTeamConcurrencyConflictDto
{
    public string Message { get; set; } = string.Empty;

    public string CurrentVersionToken { get; set; } = string.Empty;
}

public class TeamAssignmentResponseDto
{
    public int AssignmentId { get; set; }

    public int TeamId { get; set; }

    public string? TeamName { get; set; }

    public int ReportId { get; set; }

    public string? ReportCity { get; set; }

    public string? ReportDisasterType { get; set; }

    public int AssignedBy { get; set; }

    public string? AssignedByName { get; set; }

    public DateTime AssignmentTime { get; set; }

    public DateTime? CompletionTime { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class RescueTeamRecommendationDto
{
    public int TeamId { get; set; }

    public string TeamName { get; set; } = string.Empty;

    public string TeamType { get; set; } = string.Empty;

    public string AvailabilityStatus { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public double DistanceKm { get; set; }

    public double PriorityScore { get; set; }

    public string? PreferredTeamType { get; set; }

    public string SeverityLevel { get; set; } = string.Empty;

    public string RecommendationReason { get; set; } = string.Empty;
}
