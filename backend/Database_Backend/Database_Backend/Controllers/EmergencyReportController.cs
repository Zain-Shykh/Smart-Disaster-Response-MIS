using Database_Backend.Models;
using Database_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator,EmergencyOperator,FieldOfficer")]
public class EmergencyReportController : ControllerBase
{
    private static readonly HashSet<string> AllowedSeverityLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low",
        "Medium",
        "High",
        "Critical"
    };

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending",
        "InProgress",
        "Resolved",
        "Closed"
    };

    private static readonly HashSet<string> AllowedSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "Mobile",
        "Helpline",
        "MonitoringSystem"
    };

    private readonly DatabaseProjectContext _context;

    public EmergencyReportController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmergencyReportResponseDto>>> GetEmergencyReports(
        [FromQuery] EmergencyReportQueryDto query,
        CancellationToken cancellationToken)
    {
        IQueryable<EmergencyReport> reports = _context.EmergencyReports
            .AsNoTracking()
            .Include(report => report.Citizen)
            .Include(report => report.Event);

        if (query.CitizenId.HasValue)
        {
            reports = reports.Where(report => report.CitizenId == query.CitizenId.Value);
        }

        if (query.EventId.HasValue)
        {
            reports = reports.Where(report => report.EventId == query.EventId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            reports = reports.Where(report => report.City == city);
        }

        if (!string.IsNullOrWhiteSpace(query.DisasterType))
        {
            var disasterType = query.DisasterType.Trim();
            reports = reports.Where(report => report.DisasterType == disasterType);
        }

        if (!string.IsNullOrWhiteSpace(query.SeverityLevel))
        {
            var severityLevel = NormalizeSeverityLevel(query.SeverityLevel);
            if (severityLevel is null)
            {
                return BadRequest("SeverityLevel must be one of: Low, Medium, High, Critical.");
            }

            reports = reports.Where(report => report.SeverityLevel == severityLevel);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = NormalizeStatus(query.Status);
            if (status is null)
            {
                return BadRequest("Status must be one of: Pending, InProgress, Resolved, Closed.");
            }

            reports = reports.Where(report => report.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            var source = NormalizeSource(query.Source);
            if (source is null)
            {
                return BadRequest("Source must be one of: Mobile, Helpline, MonitoringSystem.");
            }

            reports = reports.Where(report => report.Source == source);
        }

        if (query.From.HasValue)
        {
            reports = reports.Where(report => report.ReportTime >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            reports = reports.Where(report => report.ReportTime <= query.To.Value);
        }

        var reportEntities = await reports.ToListAsync(cancellationToken);

        var result = reportEntities
            .OrderBy(report => IncidentPriorityService.Calculate(report.SeverityLevel, report.Event?.AffectedPopulation ?? 0).PriorityLevel)
            .ThenByDescending(report => IncidentPriorityService.Calculate(report.SeverityLevel, report.Event?.AffectedPopulation ?? 0).PriorityScore)
            .ThenBy(report => report.ReportTime)
            .Select(MapToResponse)
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmergencyReportResponseDto>> GetEmergencyReport(int id, CancellationToken cancellationToken)
    {
        var report = await LoadReportAsync(id, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(report));
    }

    [HttpPost]
    public async Task<ActionResult<EmergencyReportResponseDto>> CreateEmergencyReport(
        [FromBody] EmergencyReportCreateDto request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(
            request.CitizenId,
            request.EventId,
            request.Street,
            request.Area,
            request.City,
            request.Province,
            request.DisasterType,
            request.SeverityLevel,
            request.Status,
            request.Source);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        if (!await _context.Citizens.AnyAsync(citizen => citizen.CitizenId == request.CitizenId, cancellationToken))
        {
            return NotFound($"Citizen {request.CitizenId} was not found.");
        }

        if (request.EventId.HasValue && !await _context.DisasterEvents.AnyAsync(disasterEvent => disasterEvent.EventId == request.EventId.Value, cancellationToken))
        {
            return NotFound($"Disaster event {request.EventId.Value} was not found.");
        }

        var report = new EmergencyReport
        {
            CitizenId = request.CitizenId,
            EventId = request.EventId,
            Street = request.Street.Trim(),
            Area = request.Area.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            DisasterType = request.DisasterType.Trim(),
            SeverityLevel = NormalizeSeverityLevel(request.SeverityLevel)!,
            ReportTime = request.ReportTime ?? DateTime.Now,
            Status = NormalizeStatus(request.Status)!,
            Source = NormalizeSource(request.Source)!,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        _context.EmergencyReports.Add(report);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The emergency report could not be saved. Check foreign keys and field values.");
        }

        var createdReport = await LoadReportAsync(report.ReportId, cancellationToken);
        return CreatedAtAction(nameof(GetEmergencyReport), new { id = report.ReportId }, MapToResponse(createdReport ?? report));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmergencyReportResponseDto>> UpdateEmergencyReport(
        int id,
        [FromBody] EmergencyReportUpdateDto request,
        CancellationToken cancellationToken)
    {
        var report = await _context.EmergencyReports.FirstOrDefaultAsync(item => item.ReportId == id, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        var validationError = ValidateRequest(
            request.CitizenId,
            request.EventId,
            request.Street,
            request.Area,
            request.City,
            request.Province,
            request.DisasterType,
            request.SeverityLevel,
            request.Status,
            request.Source);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        if (!await _context.Citizens.AnyAsync(citizen => citizen.CitizenId == request.CitizenId, cancellationToken))
        {
            return NotFound($"Citizen {request.CitizenId} was not found.");
        }

        if (request.EventId.HasValue && !await _context.DisasterEvents.AnyAsync(disasterEvent => disasterEvent.EventId == request.EventId.Value, cancellationToken))
        {
            return NotFound($"Disaster event {request.EventId.Value} was not found.");
        }

        report.CitizenId = request.CitizenId;
        report.EventId = request.EventId;
        report.Street = request.Street.Trim();
        report.Area = request.Area.Trim();
        report.City = request.City.Trim();
        report.Province = request.Province.Trim();
        report.Latitude = request.Latitude;
        report.Longitude = request.Longitude;
        report.DisasterType = request.DisasterType.Trim();
        report.SeverityLevel = NormalizeSeverityLevel(request.SeverityLevel)!;
        report.ReportTime = request.ReportTime;
        report.Status = NormalizeStatus(request.Status)!;
        report.Source = NormalizeSource(request.Source)!;
        report.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The emergency report could not be updated. Check foreign keys and field values.");
        }

        var updatedReport = await LoadReportAsync(report.ReportId, cancellationToken);
        return Ok(MapToResponse(updatedReport ?? report));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<EmergencyReportResponseDto>> UpdateReportStatus(
        int id,
        [FromBody] EmergencyReportStatusUpdateDto request,
        CancellationToken cancellationToken)
    {
        var report = await _context.EmergencyReports.FirstOrDefaultAsync(item => item.ReportId == id, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        var status = NormalizeStatus(request.Status);
        if (status is null)
        {
            return BadRequest("Status must be one of: Pending, InProgress, Resolved, Closed.");
        }

        report.Status = status;

        var elapsedMinutes = (int)Math.Max(0, Math.Floor((DateTime.Now - report.ReportTime).TotalMinutes));
        if (status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) && report.ResponseTimeMinutes is null)
        {
            report.ResponseTimeMinutes = elapsedMinutes;
        }

        if ((status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) || status.Equals("Closed", StringComparison.OrdinalIgnoreCase)) && report.ResolutionTimeMinutes is null)
        {
            report.ResolutionTimeMinutes = elapsedMinutes;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The emergency report status could not be updated.");
        }

        var updatedReport = await LoadReportAsync(report.ReportId, cancellationToken);
        return Ok(MapToResponse(updatedReport ?? report));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEmergencyReport(int id, CancellationToken cancellationToken)
    {
        var report = await _context.EmergencyReports.FirstOrDefaultAsync(item => item.ReportId == id, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        _context.EmergencyReports.Remove(report);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The emergency report could not be deleted.");
        }

        return NoContent();
    }

    [HttpPut("{id:int}/priority")]
    public async Task<ActionResult<EmergencyReportPriorityDto>> RecalculatePriority(int id, CancellationToken cancellationToken)
    {
        var report = await _context.EmergencyReports
            .AsNoTracking()
            .Include(item => item.Event)
            .FirstOrDefaultAsync(item => item.ReportId == id, cancellationToken);

        if (report is null)
        {
            return NotFound();
        }

        var priority = IncidentPriorityService.Calculate(report.SeverityLevel, report.Event?.AffectedPopulation ?? 0);

        return Ok(new EmergencyReportPriorityDto
        {
            ReportId = report.ReportId,
            SeverityLevel = report.SeverityLevel,
            AffectedPopulation = report.Event?.AffectedPopulation ?? 0,
            PriorityLevel = priority.PriorityLevel,
            PriorityLabel = priority.PriorityLabel,
            PriorityScore = priority.PriorityScore,
            EstimatedResponseMinutes = priority.EstimatedResponseMinutes
        });
    }

    private static string? NormalizeSeverityLevel(string? severityLevel)
    {
        if (string.IsNullOrWhiteSpace(severityLevel) || !AllowedSeverityLevels.Contains(severityLevel.Trim()))
        {
            return null;
        }

        return AllowedSeverityLevels.First(level => level.Equals(severityLevel.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || !AllowedStatuses.Contains(status.Trim()))
        {
            return null;
        }

        return AllowedStatuses.First(item => item.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source) || !AllowedSources.Contains(source.Trim()))
        {
            return null;
        }

        return AllowedSources.First(item => item.Equals(source.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? ValidateRequest(
        int citizenId,
        int? eventId,
        string street,
        string area,
        string city,
        string province,
        string disasterType,
        string severityLevel,
        string status,
        string source)
    {
        if (citizenId <= 0)
        {
            return "CitizenId must be greater than zero.";
        }

        if (NormalizeSeverityLevel(severityLevel) is null)
        {
            return "SeverityLevel must be one of: Low, Medium, High, Critical.";
        }

        if (NormalizeStatus(status) is null)
        {
            return "Status must be one of: Pending, InProgress, Resolved, Closed.";
        }

        if (NormalizeSource(source) is null)
        {
            return "Source must be one of: Mobile, Helpline, MonitoringSystem.";
        }

        if (eventId.HasValue && eventId.Value <= 0)
        {
            return "EventId must be greater than zero when provided.";
        }

        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(area) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(province) || string.IsNullOrWhiteSpace(disasterType))
        {
            return "Street, Area, City, Province, and DisasterType are required.";
        }

        return null;
    }

    private static EmergencyReportResponseDto MapToResponse(EmergencyReport report)
    {
        var priority = IncidentPriorityService.Calculate(report.SeverityLevel, report.Event?.AffectedPopulation ?? 0);

        return new EmergencyReportResponseDto
        {
            ReportId = report.ReportId,
            CitizenId = report.CitizenId,
            CitizenName = report.Citizen is null ? null : $"{report.Citizen.FirstName} {report.Citizen.LastName}",
            EventId = report.EventId,
            EventName = report.Event?.EventName,
            Street = report.Street,
            Area = report.Area,
            City = report.City,
            Province = report.Province,
            Latitude = report.Latitude,
            Longitude = report.Longitude,
            DisasterType = report.DisasterType,
            SeverityLevel = report.SeverityLevel,
            ReportTime = report.ReportTime,
            Status = report.Status,
            Source = report.Source,
            Description = report.Description,
            ResponseTimeMinutes = report.ResponseTimeMinutes,
            ResolutionTimeMinutes = report.ResolutionTimeMinutes,
            PriorityLevel = priority.PriorityLevel,
            PriorityLabel = priority.PriorityLabel,
            PriorityScore = priority.PriorityScore,
            EstimatedResponseMinutes = priority.EstimatedResponseMinutes
        };
    }

    /// <summary>
    /// Get pending emergency reports from vw_EmergencyReports_Pending view.
    /// Shows unassigned reports awaiting team assignment.
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<EmergencyReportPendingDto>>> GetPendingReportsFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwEmergencyReportsPending
            .AsNoTracking()
            .Select(v => new EmergencyReportPendingDto
            {
                ReportId = v.ReportId,
                CitizenId = v.CitizenId,
                EventId = v.EventId,
                DisasterType = v.DisasterType,
                SeverityLevel = v.SeverityLevel,
                ReportTime = v.ReportTime,
                Status = v.Status,
                Street = v.Street,
                Area = v.Area,
                City = v.City
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get emergency reports grouped by event from vw_EmergencyReports_ByEvent view.
    /// Supports event-level analysis and reporting.
    /// </summary>
    [HttpGet("by-event")]
    public async Task<ActionResult<IEnumerable<EmergencyReportByEventDto>>> GetReportsByEventFromView(
        [FromQuery] int? eventId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.VwEmergencyReportsByEvent.AsNoTracking();

        if (eventId.HasValue)
        {
            query = query.Where(v => v.EventId == eventId.Value);
        }

        var result = await query
            .Select(v => new EmergencyReportByEventDto
            {
                ReportId = v.ReportId,
                EventId = v.EventId,
                EventName = v.EventName,
                DisasterType = v.DisasterType,
                SeverityLevel = v.SeverityLevel,
                Status = v.Status,
                ReportTime = v.ReportTime
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    private async Task<EmergencyReport?> LoadReportAsync(int reportId, CancellationToken cancellationToken)
    {
        return await _context.EmergencyReports
            .AsNoTracking()
            .Include(report => report.Citizen)
            .Include(report => report.Event)
            .FirstOrDefaultAsync(report => report.ReportId == reportId, cancellationToken);
    }
}

public class EmergencyReportQueryDto
{
    public int? CitizenId { get; set; }

    public int? EventId { get; set; }

    public string? City { get; set; }

    public string? DisasterType { get; set; }

    public string? SeverityLevel { get; set; }

    public string? Status { get; set; }

    public string? Source { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
}

public class EmergencyReportCreateDto
{
    [Range(1, int.MaxValue)]
    public int CitizenId { get; set; }

    public int? EventId { get; set; }

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
    [MaxLength(100)]
    public string DisasterType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string SeverityLevel { get; set; } = string.Empty;

    public DateTime? ReportTime { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Pending";

    [Required]
    [MaxLength(40)]
    public string Source { get; set; } = "Mobile";

    [MaxLength(2000)]
    public string? Description { get; set; }
}

public class EmergencyReportUpdateDto
{
    [Range(1, int.MaxValue)]
    public int CitizenId { get; set; }

    public int? EventId { get; set; }

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
    [MaxLength(100)]
    public string DisasterType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string SeverityLevel { get; set; } = string.Empty;

    public DateTime ReportTime { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Source { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
}

public class EmergencyReportStatusUpdateDto
{
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;
}

public class EmergencyReportResponseDto
{
    public int ReportId { get; set; }

    public int CitizenId { get; set; }

    public string? CitizenName { get; set; }

    public int? EventId { get; set; }

    public string? EventName { get; set; }

    public string Street { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string DisasterType { get; set; } = string.Empty;

    public string SeverityLevel { get; set; } = string.Empty;

    public DateTime ReportTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ResponseTimeMinutes { get; set; }

    public int? ResolutionTimeMinutes { get; set; }

    public int PriorityLevel { get; set; }

    public string PriorityLabel { get; set; } = string.Empty;

    public decimal PriorityScore { get; set; }

    public int EstimatedResponseMinutes { get; set; }
}

public class EmergencyReportPriorityDto
{
    public int ReportId { get; set; }

    public string SeverityLevel { get; set; } = string.Empty;

    public int AffectedPopulation { get; set; }

    public int PriorityLevel { get; set; }

    public string PriorityLabel { get; set; } = string.Empty;

    public decimal PriorityScore { get; set; }

    public int EstimatedResponseMinutes { get; set; }
}

// ============================================================================
// DATABASE VIEW DTOs
// ============================================================================

public class EmergencyReportPendingDto
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

public class EmergencyReportByEventDto
{
    public int ReportId { get; set; }
    public int? EventId { get; set; }
    public string? EventName { get; set; }
    public string DisasterType { get; set; } = string.Empty;
    public string SeverityLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ReportTime { get; set; }
}
