using Database_Backend.Models;
using Database_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator,EmergencyOperator,FieldOfficer,Field Officer,WarehouseManager,Warehouse Manager")]
public class DisasterEventController : ControllerBase
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Active",
        "Contained",
        "Resolved"
    };

    private readonly DatabaseProjectContext _context;

    public DisasterEventController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DisasterEventResponseDto>>> GetDisasterEvents(
        [FromQuery] DisasterEventQueryDto query,
        CancellationToken cancellationToken)
    {
        IQueryable<DisasterEvent> events = _context.DisasterEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.EventName))
        {
            var eventName = query.EventName.Trim();
            events = events.Where(item => item.EventName == eventName);
        }

        if (!string.IsNullOrWhiteSpace(query.DisasterType))
        {
            var disasterType = query.DisasterType.Trim();
            events = events.Where(item => item.DisasterType == disasterType);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            events = events.Where(item => item.City == city);
        }

        if (!string.IsNullOrWhiteSpace(query.Province))
        {
            var province = query.Province.Trim();
            events = events.Where(item => item.Province == province);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = NormalizeStatus(query.Status);
            if (status is null)
            {
                return BadRequest("Status must be one of: Active, Contained, Resolved.");
            }

            events = events.Where(item => item.Status == status);
        }

        if (query.StartFrom.HasValue)
        {
            events = events.Where(item => item.StartTime >= query.StartFrom.Value);
        }

        if (query.StartTo.HasValue)
        {
            events = events.Where(item => item.StartTime <= query.StartTo.Value);
        }

        var result = await events
            .OrderByDescending(item => item.Status == "Active")
            .ThenByDescending(item => item.StartTime)
            .Select(item => new DisasterEventResponseDto
            {
                EventId = item.EventId,
                EventName = item.EventName,
                DisasterType = item.DisasterType,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                DurationMinutes = item.DurationMinutes,
                Street = item.Street,
                Area = item.Area,
                City = item.City,
                Province = item.Province,
                Status = item.Status,
                AffectedPopulation = item.AffectedPopulation,
                TotalReports = item.TotalReports
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DisasterEventResponseDto>> GetDisasterEvent(int id, CancellationToken cancellationToken)
    {
        var disasterEvent = await _context.DisasterEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.EventId == id, cancellationToken);

        if (disasterEvent is null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(disasterEvent));
    }

    [HttpPost]
    public async Task<ActionResult<DisasterEventResponseDto>> CreateDisasterEvent(
        [FromBody] DisasterEventCreateDto request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request.EventName, request.DisasterType, request.StartTime, request.EndTime, request.Status, request.Street, request.Area, request.City, request.Province, request.AffectedPopulation);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var disasterEvent = new DisasterEvent
        {
            EventName = request.EventName.Trim(),
            DisasterType = request.DisasterType.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Street = request.Street.Trim(),
            Area = request.Area.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            Status = NormalizeStatus(request.Status)!,
            AffectedPopulation = request.AffectedPopulation,
            TotalReports = 0
        };

        _context.DisasterEvents.Add(disasterEvent);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The disaster event could not be saved.");
        }

        return CreatedAtAction(nameof(GetDisasterEvent), new { id = disasterEvent.EventId }, MapToResponse(disasterEvent));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DisasterEventResponseDto>> UpdateDisasterEvent(
        int id,
        [FromBody] DisasterEventUpdateDto request,
        CancellationToken cancellationToken)
    {
        var disasterEvent = await _context.DisasterEvents.FirstOrDefaultAsync(item => item.EventId == id, cancellationToken);
        if (disasterEvent is null)
        {
            return NotFound();
        }

        var validationError = ValidateRequest(request.EventName, request.DisasterType, request.StartTime, request.EndTime, request.Status, request.Street, request.Area, request.City, request.Province, request.AffectedPopulation);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var currentToken = ComputeVersionToken(disasterEvent);
        if (!string.IsNullOrWhiteSpace(request.VersionToken) && !string.Equals(request.VersionToken, currentToken, StringComparison.Ordinal))
        {
            return Conflict(new ConcurrencyConflictDto
            {
                Message = "Disaster event was modified by another operation.",
                CurrentVersionToken = currentToken
            });
        }

        disasterEvent.EventName = request.EventName.Trim();
        disasterEvent.DisasterType = request.DisasterType.Trim();
        disasterEvent.StartTime = request.StartTime;
        disasterEvent.EndTime = request.EndTime;
        disasterEvent.Street = request.Street.Trim();
        disasterEvent.Area = request.Area.Trim();
        disasterEvent.City = request.City.Trim();
        disasterEvent.Province = request.Province.Trim();
        disasterEvent.Status = NormalizeStatus(request.Status)!;
        disasterEvent.AffectedPopulation = request.AffectedPopulation;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The disaster event could not be updated.");
        }

        return Ok(MapToResponse(disasterEvent));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<DisasterEventResponseDto>> UpdateDisasterEventStatus(
        int id,
        [FromBody] DisasterEventStatusUpdateDto request,
        CancellationToken cancellationToken)
    {
        var disasterEvent = await _context.DisasterEvents.FirstOrDefaultAsync(item => item.EventId == id, cancellationToken);
        if (disasterEvent is null)
        {
            return NotFound();
        }

        var status = NormalizeStatus(request.Status);
        if (status is null)
        {
            return BadRequest("Status must be one of: Active, Contained, Resolved.");
        }

        var currentToken = ComputeVersionToken(disasterEvent);
        if (!string.IsNullOrWhiteSpace(request.VersionToken) && !string.Equals(request.VersionToken, currentToken, StringComparison.Ordinal))
        {
            return Conflict(new ConcurrencyConflictDto
            {
                Message = "Disaster event status was modified by another operation.",
                CurrentVersionToken = currentToken
            });
        }

        disasterEvent.Status = status;
        if (status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) && disasterEvent.EndTime is null)
        {
            disasterEvent.EndTime = DateTime.Now;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The disaster event status could not be updated.");
        }

        return Ok(MapToResponse(disasterEvent));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDisasterEvent(int id, CancellationToken cancellationToken)
    {
        var disasterEvent = await _context.DisasterEvents.FirstOrDefaultAsync(item => item.EventId == id, cancellationToken);
        if (disasterEvent is null)
        {
            return NotFound();
        }

        _context.DisasterEvents.Remove(disasterEvent);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The disaster event could not be deleted.");
        }

        return NoContent();
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || !AllowedStatuses.Contains(status.Trim()))
        {
            return null;
        }

        return AllowedStatuses.First(item => item.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? ValidateRequest(
        string eventName,
        string disasterType,
        DateTime startTime,
        DateTime? endTime,
        string status,
        string street,
        string area,
        string city,
        string province,
        int affectedPopulation)
    {
        if (string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(disasterType) || string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(area) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(province))
        {
            return "EventName, DisasterType, Street, Area, City, and Province are required.";
        }

        if (startTime == default)
        {
            return "StartTime is required.";
        }

        if (endTime.HasValue && endTime.Value < startTime)
        {
            return "EndTime must be greater than or equal to StartTime.";
        }

        if (NormalizeStatus(status) is null)
        {
            return "Status must be one of: Active, Contained, Resolved.";
        }

        if (affectedPopulation < 0)
        {
            return "AffectedPopulation cannot be negative.";
        }

        return null;
    }

    private static DisasterEventResponseDto MapToResponse(DisasterEvent disasterEvent)
    {
        return new DisasterEventResponseDto
        {
            EventId = disasterEvent.EventId,
            EventName = disasterEvent.EventName,
            DisasterType = disasterEvent.DisasterType,
            StartTime = disasterEvent.StartTime,
            EndTime = disasterEvent.EndTime,
            DurationMinutes = disasterEvent.DurationMinutes,
            Street = disasterEvent.Street,
            Area = disasterEvent.Area,
            City = disasterEvent.City,
            Province = disasterEvent.Province,
            Status = disasterEvent.Status,
            AffectedPopulation = disasterEvent.AffectedPopulation,
            TotalReports = disasterEvent.TotalReports,
            VersionToken = ComputeVersionToken(disasterEvent)
        };
    }

    private static string ComputeVersionToken(DisasterEvent disasterEvent)
    {
        return ConcurrencyTokenService.Compute(
            disasterEvent.EventId,
            disasterEvent.EventName,
            disasterEvent.DisasterType,
            disasterEvent.StartTime,
            disasterEvent.EndTime,
            disasterEvent.Street,
            disasterEvent.Area,
            disasterEvent.City,
            disasterEvent.Province,
            disasterEvent.Status,
            disasterEvent.AffectedPopulation,
            disasterEvent.TotalReports);
    }
}

public class DisasterEventQueryDto
{
    public string? EventName { get; set; }

    public string? DisasterType { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    public string? Status { get; set; }

    public DateTime? StartFrom { get; set; }

    public DateTime? StartTo { get; set; }
}

public class DisasterEventCreateDto
{
    [Required]
    [MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisasterType { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

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

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int AffectedPopulation { get; set; }
}

public class DisasterEventUpdateDto
{
    [Required]
    [MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisasterType { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

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

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int AffectedPopulation { get; set; }

    public string? VersionToken { get; set; }
}

public class DisasterEventStatusUpdateDto
{
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    public string? VersionToken { get; set; }
}

public class DisasterEventResponseDto
{
    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string DisasterType { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? DurationMinutes { get; set; }

    public string Street { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int AffectedPopulation { get; set; }

    public int TotalReports { get; set; }

    public string VersionToken { get; set; } = string.Empty;
}

public class ConcurrencyConflictDto
{
    public string Message { get; set; } = string.Empty;

    public string CurrentVersionToken { get; set; } = string.Empty;
}
