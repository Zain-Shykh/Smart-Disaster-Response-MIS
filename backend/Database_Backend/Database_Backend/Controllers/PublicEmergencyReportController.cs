using Database_Backend.Models;
using Database_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class PublicEmergencyReportController : ControllerBase
{
    private static readonly HashSet<string> AllowedSeverityLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low",
        "Medium",
        "High",
        "Critical"
    };

    private readonly DatabaseProjectContext _context;

    public PublicEmergencyReportController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<EmergencyReportResponseDto>> ReportDisaster(
        [FromBody] PublicReportCreateDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NationalId) || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest("Citizen National ID, First Name, and Last Name are required.");
        }

        if (string.IsNullOrWhiteSpace(request.Street) || string.IsNullOrWhiteSpace(request.Area) || string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.Province) || string.IsNullOrWhiteSpace(request.DisasterType))
        {
            return BadRequest("Street, Area, City, Province, and DisasterType are required.");
        }

        var severityLevel = "Medium";
        if (!string.IsNullOrWhiteSpace(request.SeverityLevel))
        {
            severityLevel = AllowedSeverityLevels.FirstOrDefault(item => item.Equals(request.SeverityLevel.Trim(), StringComparison.OrdinalIgnoreCase));
            if (severityLevel == null)
            {
                return BadRequest("SeverityLevel must be one of: Low, Medium, High, Critical.");
            }
        }

        var nationalId = request.NationalId.Trim();
        var citizen = await _context.Citizens.FirstOrDefaultAsync(c => c.NationalId == nationalId, cancellationToken);

        if (citizen == null)
        {
            citizen = new Citizen
            {
                NationalId = nationalId,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Street = request.Street.Trim(),
                Area = request.Area.Trim(),
                City = request.City.Trim(),
                Province = request.Province.Trim(),
                Email = $"guest_{nationalId}@local.sdrmis"
            };
            _context.Citizens.Add(citizen);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var report = new EmergencyReport
        {
            CitizenId = citizen.CitizenId,
            Street = request.Street.Trim(),
            Area = request.Area.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            DisasterType = request.DisasterType.Trim(),
            SeverityLevel = severityLevel,
            ReportTime = DateTime.Now,
            Status = "Pending",
            Source = "Helpline",
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        _context.EmergencyReports.Add(report);
        
        citizen.TotalReports += 1;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { Message = "Report submitted successfully", ReportId = report.ReportId });
    }
}

public class PublicReportCreateDto
{
    [Required]
    [MaxLength(20)]
    public string NationalId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

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

    [MaxLength(20)]
    public string SeverityLevel { get; set; } = "Medium";

    [MaxLength(2000)]
    public string? Description { get; set; }
}
