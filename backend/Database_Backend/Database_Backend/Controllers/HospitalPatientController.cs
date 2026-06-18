using Database_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator,EmergencyOperator,FieldOfficer,Field Officer")]
public class HospitalPatientController : ControllerBase
{
    private static readonly HashSet<string> AllowedAdmissionConditions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Critical",
        "Serious",
        "Stable"
    };

    private static readonly HashSet<string> AllowedAdmissionStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admitted",
        "Discharged",
        "Transferred"
    };

    private static readonly HashSet<string> AllowedBloodTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "A+",
        "A-",
        "B+",
        "B-",
        "AB+",
        "AB-",
        "O+",
        "O-"
    };

    private readonly DatabaseProjectContext _context;

    public HospitalPatientController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpGet("hospitals")]
    public async Task<ActionResult<IEnumerable<HospitalDto>>> GetHospitals(
        [FromQuery] string? city,
        [FromQuery] int? minAvailableBeds,
        CancellationToken cancellationToken)
    {
        IQueryable<Hospital> hospitals = _context.Hospitals
            .AsNoTracking()
            .Include(item => item.HospitalSpecializations);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityValue = city.Trim();
            hospitals = hospitals.Where(item => item.City == cityValue);
        }

        if (minAvailableBeds.HasValue)
        {
            hospitals = hospitals.Where(item => item.AvailableBeds >= minAvailableBeds.Value);
        }

        var result = await hospitals
            .OrderBy(item => item.HospitalId)
            .Select(item => new HospitalDto
            {
                HospitalId = item.HospitalId,
                HospitalName = item.HospitalName,
                Street = item.Street,
                Area = item.Area,
                City = item.City,
                Province = item.Province,
                TotalBeds = item.TotalBeds,
                AvailableBeds = item.AvailableBeds,
                OccupancyRate = item.OccupancyRate,
                ContactPhone = item.ContactPhone,
                ContactEmail = item.ContactEmail,
                Specializations = item.HospitalSpecializations.Select(spec => spec.Specialization).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("hospitals")]
    public async Task<ActionResult<HospitalDto>> CreateHospital(
        [FromBody] HospitalCreateDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.HospitalName) || string.IsNullOrWhiteSpace(request.Street) || string.IsNullOrWhiteSpace(request.Area) || string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.Province))
        {
            return BadRequest("HospitalName, Street, Area, City, and Province are required.");
        }

        if (request.TotalBeds < 0)
        {
            return BadRequest("TotalBeds cannot be negative.");
        }

        if (request.AvailableBeds < 0 || request.AvailableBeds > request.TotalBeds)
        {
            return BadRequest("AvailableBeds must be between 0 and TotalBeds.");
        }

        var hospital = new Hospital
        {
            HospitalName = request.HospitalName.Trim(),
            Street = request.Street.Trim(),
            Area = request.Area.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            TotalBeds = request.TotalBeds,
            AvailableBeds = request.AvailableBeds,
            ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim(),
            ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim()
        };

        _context.Hospitals.Add(hospital);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetHospitals), new { city = hospital.City }, MapHospital(hospital, new List<string>()));
    }

    [HttpPatch("hospitals/{hospitalId:int}/beds")]
    public async Task<ActionResult<HospitalDto>> UpdateHospitalBeds(
        int hospitalId,
        [FromBody] HospitalBedUpdateDto request,
        CancellationToken cancellationToken)
    {
        var hospital = await _context.Hospitals
            .Include(item => item.HospitalSpecializations)
            .FirstOrDefaultAsync(item => item.HospitalId == hospitalId, cancellationToken);

        if (hospital is null)
        {
            return NotFound();
        }

        if (request.TotalBeds.HasValue)
        {
            if (request.TotalBeds.Value < 0)
            {
                return BadRequest("TotalBeds cannot be negative.");
            }

            hospital.TotalBeds = request.TotalBeds.Value;
        }

        if (request.AvailableBeds.HasValue)
        {
            hospital.AvailableBeds = request.AvailableBeds.Value;
        }

        if (hospital.AvailableBeds < 0 || hospital.AvailableBeds > hospital.TotalBeds)
        {
            return BadRequest("AvailableBeds must be between 0 and TotalBeds.");
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapHospital(hospital, hospital.HospitalSpecializations.Select(spec => spec.Specialization).ToList()));
    }

    [HttpPost("hospitals/{hospitalId:int}/specializations")]
    public async Task<ActionResult<HospitalDto>> AddHospitalSpecialization(
        int hospitalId,
        [FromBody] HospitalSpecializationCreateDto request,
        CancellationToken cancellationToken)
    {
        var hospital = await _context.Hospitals
            .Include(item => item.HospitalSpecializations)
            .FirstOrDefaultAsync(item => item.HospitalId == hospitalId, cancellationToken);

        if (hospital is null)
        {
            return NotFound($"Hospital {hospitalId} was not found.");
        }

        var specialization = request.Specialization.Trim();

        if (hospital.HospitalSpecializations.Any(item => item.Specialization.Equals(specialization, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict("Specialization already exists for this hospital.");
        }

        hospital.HospitalSpecializations.Add(new HospitalSpecialization
        {
            HospitalId = hospitalId,
            Specialization = specialization
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapHospital(hospital, hospital.HospitalSpecializations.Select(spec => spec.Specialization).ToList()));
    }

    [HttpGet("hospitals/search")]
    public async Task<ActionResult<IEnumerable<HospitalRoutingCandidateDto>>> SearchHospitalsBySpecialization(
        [FromQuery] string specialization,
        [FromQuery] string? city,
        [FromQuery] int bedRequirement = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(specialization))
        {
            return BadRequest("specialization is required.");
        }

        if (bedRequirement < 1)
        {
            return BadRequest("bedRequirement must be at least 1.");
        }

        var specializationValue = specialization.Trim();

        var specializationExists = await _context.HospitalSpecializations
            .AsNoTracking()
            .AnyAsync(item => item.Specialization == specializationValue, cancellationToken);

        if (!specializationExists)
        {
            return BadRequest($"Specialization '{specializationValue}' does not exist.");
        }

        IQueryable<Hospital> hospitals = _context.Hospitals
            .AsNoTracking()
            .Include(item => item.HospitalSpecializations)
            .Where(item => item.AvailableBeds >= bedRequirement)
            .Where(item => item.HospitalSpecializations.Any(spec => spec.Specialization == specializationValue));

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityValue = city.Trim();
            hospitals = hospitals.Where(item => item.City == cityValue);
        }

        var result = await hospitals
            .OrderByDescending(item => item.AvailableBeds)
            .ThenBy(item => item.HospitalName)
            .Select(item => new HospitalRoutingCandidateDto
            {
                HospitalId = item.HospitalId,
                HospitalName = item.HospitalName,
                City = item.City,
                Province = item.Province,
                AvailableBeds = item.AvailableBeds,
                Specializations = item.HospitalSpecializations.Select(spec => spec.Specialization).ToList()
            })
            .ToListAsync(cancellationToken);

        if (result.Count == 0)
        {
            return NotFound("No hospitals match the provided routing criteria.");
        }

        return Ok(result);
    }

    [HttpPost("hospitals/{hospitalId:int}/route-patient")]
    public async Task<ActionResult<PatientAdmissionDto>> RoutePatientToHospital(
        int hospitalId,
        [FromBody] HospitalRoutePatientRequest request,
        CancellationToken cancellationToken)
    {
        var condition = NormalizeAdmissionCondition(request.Condition);
        if (condition is null)
        {
            return BadRequest("Condition must be one of: Critical, Serious, Stable.");
        }

        var status = NormalizeAdmissionStatus(request.Status);
        if (status is null)
        {
            return BadRequest("Status must be one of: Admitted, Discharged, Transferred.");
        }

        var hospital = await _context.Hospitals
            .Include(item => item.HospitalSpecializations)
            .FirstOrDefaultAsync(item => item.HospitalId == hospitalId, cancellationToken);

        if (hospital is null)
        {
            return NotFound($"Hospital {hospitalId} was not found.");
        }

        if (!await _context.Patients.AnyAsync(item => item.PatientId == request.PatientId, cancellationToken))
        {
            return NotFound($"Patient {request.PatientId} was not found.");
        }

        if (request.ReportId.HasValue && !await _context.EmergencyReports.AnyAsync(item => item.ReportId == request.ReportId.Value, cancellationToken))
        {
            return NotFound($"Emergency report {request.ReportId.Value} was not found.");
        }

        var requiredSpecialization = request.RequiredSpecialization.Trim();

        var specializationExists = await _context.HospitalSpecializations
            .AsNoTracking()
            .AnyAsync(item => item.Specialization == requiredSpecialization, cancellationToken);

        if (!specializationExists)
        {
            return BadRequest($"Specialization '{requiredSpecialization}' does not exist.");
        }

        var hospitalHasSpecialization = hospital.HospitalSpecializations
            .Any(item => item.Specialization == requiredSpecialization);

        if (!hospitalHasSpecialization)
        {
            return NotFound($"Hospital {hospitalId} does not support specialization '{requiredSpecialization}'.");
        }

        if (hospital.AvailableBeds < 1)
        {
            return NotFound($"Hospital {hospitalId} has no available beds.");
        }

        var admission = new PatientAdmission
        {
            PatientId = request.PatientId,
            HospitalId = hospitalId,
            ReportId = request.ReportId,
            AdmissionTime = request.AdmissionTime ?? DateTime.Now,
            Condition = condition,
            Status = status
        };

        _context.PatientAdmissions.Add(admission);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Routing request could not be saved. Trigger or constraints rejected the operation.");
        }

        var created = await _context.PatientAdmissions
            .AsNoTracking()
            .Include(item => item.Patient)
            .Include(item => item.Hospital)
            .Include(item => item.Report)
            .FirstAsync(item => item.AdmissionId == admission.AdmissionId, cancellationToken);

        return CreatedAtAction(nameof(GetAdmissions), new { hospitalId = created.HospitalId }, MapAdmission(created));
    }

    [HttpPost("hospitals/route-patient/auto")]
    public async Task<ActionResult<AutoRoutePatientResultDto>> AutoRoutePatient(
        [FromBody] HospitalAutoRoutePatientRequest request,
        CancellationToken cancellationToken)
    {
        var condition = NormalizeAdmissionCondition(request.Condition);
        if (condition is null)
        {
            return BadRequest("Condition must be one of: Critical, Serious, Stable.");
        }

        var status = NormalizeAdmissionStatus(request.Status);
        if (status is null)
        {
            return BadRequest("Status must be one of: Admitted, Discharged, Transferred.");
        }

        if (request.BedRequirement < 1)
        {
            return BadRequest("BedRequirement must be at least 1.");
        }

        if (!await _context.Patients.AnyAsync(item => item.PatientId == request.PatientId, cancellationToken))
        {
            return NotFound($"Patient {request.PatientId} was not found.");
        }

        EmergencyReport? report = null;
        if (request.ReportId.HasValue)
        {
            report = await _context.EmergencyReports
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ReportId == request.ReportId.Value, cancellationToken);

            if (report is null)
            {
                return NotFound($"Emergency report {request.ReportId.Value} was not found.");
            }
        }

        var requiredSpecialization = request.RequiredSpecialization.Trim();
        var specializationExists = await _context.HospitalSpecializations
            .AsNoTracking()
            .AnyAsync(item => item.Specialization == requiredSpecialization, cancellationToken);

        if (!specializationExists)
        {
            return BadRequest($"Specialization '{requiredSpecialization}' does not exist.");
        }

        var preferredCity = !string.IsNullOrWhiteSpace(request.PreferredCity)
            ? request.PreferredCity.Trim()
            : report?.City;
        var preferredProvince = !string.IsNullOrWhiteSpace(request.PreferredProvince)
            ? request.PreferredProvince.Trim()
            : report?.Province;

        var candidates = await _context.Hospitals
            .AsNoTracking()
            .Include(item => item.HospitalSpecializations)
            .Where(item => item.AvailableBeds >= request.BedRequirement)
            .Where(item => item.HospitalSpecializations.Any(spec => spec.Specialization == requiredSpecialization))
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return Conflict(CreateEscalationResult(
                request.PatientId,
                request.ReportId,
                requiredSpecialization,
                "No hospital currently has required specialization and bed capacity.",
                preferredCity,
                preferredProvince,
                request.BedRequirement));
        }

        var tier = "Global";
        var tierCandidates = candidates;

        if (!string.IsNullOrWhiteSpace(preferredCity))
        {
            var cityCandidates = candidates
                .Where(item => item.City.Equals(preferredCity, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (cityCandidates.Count > 0)
            {
                tier = "City";
                tierCandidates = cityCandidates;
            }
            else if (!string.IsNullOrWhiteSpace(preferredProvince))
            {
                var provinceCandidates = candidates
                    .Where(item => item.Province.Equals(preferredProvince, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (provinceCandidates.Count > 0)
                {
                    tier = "Province";
                    tierCandidates = provinceCandidates;
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(preferredProvince))
        {
            var provinceCandidates = candidates
                .Where(item => item.Province.Equals(preferredProvince, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (provinceCandidates.Count > 0)
            {
                tier = "Province";
                tierCandidates = provinceCandidates;
            }
        }

        if (tierCandidates.Count == 0)
        {
            return Conflict(CreateEscalationResult(
                request.PatientId,
                request.ReportId,
                requiredSpecialization,
                "No hospital available in preferred routing tiers.",
                preferredCity,
                preferredProvince,
                request.BedRequirement));
        }

        var selectedHospital = tierCandidates
            .OrderByDescending(item => CalculateHospitalBalanceScore(item))
            .ThenByDescending(item => item.AvailableBeds)
            .ThenBy(item => item.HospitalName)
            .First();

        var admission = new PatientAdmission
        {
            PatientId = request.PatientId,
            HospitalId = selectedHospital.HospitalId,
            ReportId = request.ReportId,
            AdmissionTime = request.AdmissionTime ?? DateTime.Now,
            Condition = condition,
            Status = status
        };

        _context.PatientAdmissions.Add(admission);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Auto-routing request could not be saved. Trigger or constraints rejected the operation.");
        }

        var created = await _context.PatientAdmissions
            .AsNoTracking()
            .Include(item => item.Patient)
            .Include(item => item.Hospital)
            .Include(item => item.Report)
            .FirstAsync(item => item.AdmissionId == admission.AdmissionId, cancellationToken);

        var result = new AutoRoutePatientResultDto
        {
            Routed = true,
            EscalationRequired = false,
            RoutingTierUsed = tier,
            FallbackApplied = !tier.Equals("City", StringComparison.OrdinalIgnoreCase),
            CandidateCount = tierCandidates.Count,
            SelectedHospitalId = selectedHospital.HospitalId,
            SelectedHospitalName = selectedHospital.HospitalName,
            SelectedHospitalCity = selectedHospital.City,
            SelectedHospitalProvince = selectedHospital.Province,
            SelectedHospitalAvailableBeds = selectedHospital.AvailableBeds,
            RequiredSpecialization = requiredSpecialization,
            BedRequirement = request.BedRequirement,
            Admission = MapAdmission(created),
            Message = tier.Equals("City", StringComparison.OrdinalIgnoreCase)
                ? "Patient routed within preferred city hospitals."
                : $"Patient routed using {tier} fallback for load balancing."
        };

        return CreatedAtAction(nameof(GetAdmissions), new { hospitalId = created.HospitalId }, result);
    }

    [HttpGet("patients")]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetPatients(
        [FromQuery] string? nationalId,
        CancellationToken cancellationToken)
    {
        IQueryable<Patient> patients = _context.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(nationalId))
        {
            var id = nationalId.Trim();
            patients = patients.Where(item => item.NationalId == id);
        }

        var result = await patients
            .OrderBy(item => item.PatientId)
            .Select(item => new PatientDto
            {
                PatientId = item.PatientId,
                FirstName = item.FirstName,
                LastName = item.LastName,
                Age = item.Age,
                Gender = item.Gender,
                NationalId = item.NationalId,
                BloodType = item.BloodType,
                ContactPhone = item.ContactPhone
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("patients")]
    public async Task<ActionResult<PatientDto>> CreatePatient(
        [FromBody] PatientCreateDto request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidatePatientRequest(request.FirstName, request.LastName, request.Age, request.BloodType);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var patient = new Patient
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Age = request.Age,
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
            NationalId = string.IsNullOrWhiteSpace(request.NationalId) ? null : request.NationalId.Trim(),
            BloodType = NormalizeBloodType(request.BloodType),
            ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim()
        };

        _context.Patients.Add(patient);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Patient could not be saved. NationalID might already exist.");
        }

        return CreatedAtAction(nameof(GetPatients), new { nationalId = patient.NationalId }, MapPatient(patient));
    }

    [HttpGet("admissions")]
    public async Task<ActionResult<IEnumerable<PatientAdmissionDto>>> GetAdmissions(
        [FromQuery] int? hospitalId,
        [FromQuery] int? patientId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        IQueryable<PatientAdmission> admissions = _context.PatientAdmissions
            .AsNoTracking()
            .Include(item => item.Patient)
            .Include(item => item.Hospital)
            .Include(item => item.Report);

        if (hospitalId.HasValue)
        {
            admissions = admissions.Where(item => item.HospitalId == hospitalId.Value);
        }

        if (patientId.HasValue)
        {
            admissions = admissions.Where(item => item.PatientId == patientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = NormalizeAdmissionStatus(status);
            if (normalizedStatus is null)
            {
                return BadRequest("Admission status must be one of: Admitted, Discharged, Transferred.");
            }

            admissions = admissions.Where(item => item.Status == normalizedStatus);
        }

        var result = await admissions
            .OrderByDescending(item => item.AdmissionTime)
            .Select(item => MapAdmission(item))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("admissions")]
    public async Task<ActionResult<PatientAdmissionDto>> CreateAdmission(
        [FromBody] PatientAdmissionCreateDto request,
        CancellationToken cancellationToken)
    {
        var condition = NormalizeAdmissionCondition(request.Condition);
        if (condition is null)
        {
            return BadRequest("Condition must be one of: Critical, Serious, Stable.");
        }

        var status = NormalizeAdmissionStatus(request.Status);
        if (status is null)
        {
            return BadRequest("Status must be one of: Admitted, Discharged, Transferred.");
        }

        if (request.DischargeTime.HasValue && request.DischargeTime.Value < request.AdmissionTime)
        {
            return BadRequest("DischargeTime must be greater than or equal to AdmissionTime.");
        }

        if (!await _context.Patients.AnyAsync(item => item.PatientId == request.PatientId, cancellationToken))
        {
            return NotFound($"Patient {request.PatientId} was not found.");
        }

        if (!await _context.Hospitals.AnyAsync(item => item.HospitalId == request.HospitalId, cancellationToken))
        {
            return NotFound($"Hospital {request.HospitalId} was not found.");
        }

        if (request.ReportId.HasValue && !await _context.EmergencyReports.AnyAsync(item => item.ReportId == request.ReportId.Value, cancellationToken))
        {
            return NotFound($"Emergency report {request.ReportId.Value} was not found.");
        }

        var admission = new PatientAdmission
        {
            PatientId = request.PatientId,
            HospitalId = request.HospitalId,
            ReportId = request.ReportId,
            AdmissionTime = request.AdmissionTime,
            DischargeTime = request.DischargeTime,
            Condition = condition,
            Status = status
        };

        _context.PatientAdmissions.Add(admission);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Admission could not be saved. Trigger or constraints rejected the operation.");
        }

        var created = await _context.PatientAdmissions
            .AsNoTracking()
            .Include(item => item.Patient)
            .Include(item => item.Hospital)
            .Include(item => item.Report)
            .FirstAsync(item => item.AdmissionId == admission.AdmissionId, cancellationToken);

        return CreatedAtAction(nameof(GetAdmissions), new { hospitalId = created.HospitalId }, MapAdmission(created));
    }

    [HttpPatch("admissions/{admissionId:int}/status")]
    public async Task<ActionResult<PatientAdmissionDto>> UpdateAdmissionStatus(
        int admissionId,
        [FromBody] PatientAdmissionStatusUpdateDto request,
        CancellationToken cancellationToken)
    {
        var admission = await _context.PatientAdmissions.FirstOrDefaultAsync(item => item.AdmissionId == admissionId, cancellationToken);
        if (admission is null)
        {
            return NotFound();
        }

        var status = NormalizeAdmissionStatus(request.Status);
        if (status is null)
        {
            return BadRequest("Status must be one of: Admitted, Discharged, Transferred.");
        }

        admission.Status = status;

        if ((status.Equals("Discharged", StringComparison.OrdinalIgnoreCase) || status.Equals("Transferred", StringComparison.OrdinalIgnoreCase)) && admission.DischargeTime is null)
        {
            admission.DischargeTime = DateTime.Now;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Admission status could not be updated.");
        }

        var updated = await _context.PatientAdmissions
            .AsNoTracking()
            .Include(item => item.Patient)
            .Include(item => item.Hospital)
            .Include(item => item.Report)
            .FirstAsync(item => item.AdmissionId == admission.AdmissionId, cancellationToken);

        return Ok(MapAdmission(updated));
    }

    /// <summary>
    /// Get hospital capacity from vw_Hospital_Capacity view.
    /// Shows bed availability and occupancy rates.
    /// </summary>
    [HttpGet("hospitals/capacity")]
    public async Task<ActionResult<IEnumerable<HospitalCapacityDto>>> GetHospitalCapacityFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwHospitalCapacity
            .AsNoTracking()
            .Select(v => new HospitalCapacityDto
            {
                HospitalId = v.HospitalId,
                HospitalName = v.HospitalName,
                TotalBeds = v.TotalBeds,
                AvailableBeds = v.AvailableBeds,
                OccupancyRate = v.OccupancyRate
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get current patient admissions from vw_Patient_Admissions view.
    /// Shows admission details with related information.
    /// </summary>
    [HttpGet("admissions/view")]
    public async Task<ActionResult<IEnumerable<PatientAdmissionsViewDto>>> GetPatientAdmissionsFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwPatientAdmissions
            .AsNoTracking()
            .Select(v => new PatientAdmissionsViewDto
            {
                AdmissionId = v.AdmissionId,
                PatientId = v.PatientId,
                HospitalId = v.HospitalId,
                AdmissionTime = v.AdmissionTime,
                Condition = v.Condition,
                Status = v.Status,
                ReportId = v.ReportId
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    private static string? NormalizeAdmissionCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition) || !AllowedAdmissionConditions.Contains(condition.Trim()))
        {
            return null;
        }

        return AllowedAdmissionConditions.First(item => item.Equals(condition.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeAdmissionStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || !AllowedAdmissionStatuses.Contains(status.Trim()))
        {
            return null;
        }

        return AllowedAdmissionStatuses.First(item => item.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeBloodType(string? bloodType)
    {
        if (string.IsNullOrWhiteSpace(bloodType))
        {
            return null;
        }

        if (!AllowedBloodTypes.Contains(bloodType.Trim()))
        {
            return null;
        }

        return AllowedBloodTypes.First(item => item.Equals(bloodType.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? ValidatePatientRequest(string firstName, string lastName, int? age, string? bloodType)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return "FirstName and LastName are required.";
        }

        if (age.HasValue && (age.Value < 0 || age.Value > 130))
        {
            return "Age must be between 0 and 130 when provided.";
        }

        if (!string.IsNullOrWhiteSpace(bloodType) && NormalizeBloodType(bloodType) is null)
        {
            return "BloodType must be one of: A+, A-, B+, B-, AB+, AB-, O+, O-.";
        }

        return null;
    }

    private static HospitalDto MapHospital(Hospital hospital, List<string> specializations)
    {
        return new HospitalDto
        {
            HospitalId = hospital.HospitalId,
            HospitalName = hospital.HospitalName,
            Street = hospital.Street,
            Area = hospital.Area,
            City = hospital.City,
            Province = hospital.Province,
            TotalBeds = hospital.TotalBeds,
            AvailableBeds = hospital.AvailableBeds,
            OccupancyRate = hospital.OccupancyRate,
            ContactPhone = hospital.ContactPhone,
            ContactEmail = hospital.ContactEmail,
            Specializations = specializations
        };
    }

    private static AutoRoutePatientResultDto CreateEscalationResult(
        int patientId,
        int? reportId,
        string requiredSpecialization,
        string reason,
        string? preferredCity,
        string? preferredProvince,
        int bedRequirement)
    {
        return new AutoRoutePatientResultDto
        {
            Routed = false,
            EscalationRequired = true,
            EscalationLevel = "Regional",
            PatientId = patientId,
            ReportId = reportId,
            RequiredSpecialization = requiredSpecialization,
            BedRequirement = bedRequirement,
            PreferredCity = preferredCity,
            PreferredProvince = preferredProvince,
            Message = reason,
            SuggestedActions =
            [
                "Escalate to regional coordination center.",
                "Request temporary bed expansion from nearby hospitals.",
                "Re-run auto routing after bed status refresh."
            ]
        };
    }

    private static double CalculateHospitalBalanceScore(Hospital hospital)
    {
        var totalBeds = Math.Max(hospital.TotalBeds, 1);
        var availableRatio = (double)hospital.AvailableBeds / totalBeds;
        var occupiedRatio = (double)(totalBeds - hospital.AvailableBeds) / totalBeds;

        return (availableRatio * 0.7) + ((1.0 - occupiedRatio) * 0.3);
    }

    private static PatientDto MapPatient(Patient patient)
    {
        return new PatientDto
        {
            PatientId = patient.PatientId,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Age = patient.Age,
            Gender = patient.Gender,
            NationalId = patient.NationalId,
            BloodType = patient.BloodType,
            ContactPhone = patient.ContactPhone
        };
    }

    private static PatientAdmissionDto MapAdmission(PatientAdmission admission)
    {
        return new PatientAdmissionDto
        {
            AdmissionId = admission.AdmissionId,
            PatientId = admission.PatientId,
            PatientName = $"{admission.Patient.FirstName} {admission.Patient.LastName}",
            HospitalId = admission.HospitalId,
            HospitalName = admission.Hospital.HospitalName,
            ReportId = admission.ReportId,
            ReportCity = admission.Report?.City,
            AdmissionTime = admission.AdmissionTime,
            DischargeTime = admission.DischargeTime,
            Condition = admission.Condition,
            Status = admission.Status,
            LengthOfStayHours = admission.LengthOfStayHours
        };
    }

    /// <summary>
    /// Admit a patient to a hospital using stored procedure (sp_AdmitPatient).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("patients/{patientId:int}/admit-sp")]
    public async Task<ActionResult<PatientAdmissionResult>> AdmitPatientStoredProc(
        int patientId,
        [FromBody] PatientAdmitRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Patients.AnyAsync(p => p.PatientId == patientId, cancellationToken))
            {
                return NotFound($"Patient {patientId} was not found.");
            }

            if (!await _context.Hospitals.AnyAsync(h => h.HospitalId == request.HospitalId, cancellationToken))
            {
                return NotFound($"Hospital {request.HospitalId} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@PatientID", patientId),
                new SqlParameter("@HospitalID", request.HospitalId),
                new SqlParameter("@Condition", request.Condition),
                new SqlParameter("@ReportID", (object?)request.ReportId ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_AdmitPatient", parameters);

            if (result.ContainsKey("ResultStatus") && result.ContainsKey("AdmissionID"))
            {
                var resultObj = new PatientAdmissionResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    AdmissionID = (int)(result["AdmissionID"] ?? 0),
                    AvailableBeds = (int)(result["AvailableBeds"] ?? 0)
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
    /// Discharge a patient using stored procedure (sp_DischargePatient).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("patients/{patientId:int}/admissions/{admissionId:int}/discharge-sp")]
    public async Task<ActionResult<PatientDischargeResult>> DischargePatientStoredProc(
        int patientId,
        int admissionId,
        [FromBody] PatientDischargeRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Patients.AnyAsync(p => p.PatientId == patientId, cancellationToken))
            {
                return NotFound($"Patient {patientId} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@AdmissionID", admissionId),
                new SqlParameter("@DischargeCondition", (object?)request.DischargeCondition ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_DischargePatient", parameters);

            if (result.ContainsKey("ResultStatus") && result.ContainsKey("AdmissionID"))
            {
                var resultObj = new PatientDischargeResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    AdmissionID = (int)(result["AdmissionID"] ?? 0),
                    DischargeTime = result.ContainsKey("DischargeTime") ? (DateTime?)result["DischargeTime"] : null
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

public class HospitalDto
{
    public int HospitalId { get; set; }

    public string HospitalName { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public int TotalBeds { get; set; }

    public int AvailableBeds { get; set; }

    public decimal? OccupancyRate { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    public List<string> Specializations { get; set; } = new();
}

public class HospitalCreateDto
{
    [Required]
    [MaxLength(150)]
    public string HospitalName { get; set; } = string.Empty;

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

    [Range(1, int.MaxValue)]
    public int TotalBeds { get; set; }

    [Range(0, int.MaxValue)]
    public int AvailableBeds { get; set; }

    [MaxLength(30)]
    public string? ContactPhone { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? ContactEmail { get; set; }
}

public class HospitalBedUpdateDto
{
    [Range(1, int.MaxValue)]
    public int? TotalBeds { get; set; }

    [Range(0, int.MaxValue)]
    public int? AvailableBeds { get; set; }
}

public class HospitalSpecializationCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;
}

public class HospitalRoutingCandidateDto
{
    public int HospitalId { get; set; }

    public string HospitalName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public int AvailableBeds { get; set; }

    public List<string> Specializations { get; set; } = new();
}

public class HospitalRoutePatientRequest
{
    [Range(1, int.MaxValue)]
    public int PatientId { get; set; }

    public int? ReportId { get; set; }

    [Required]
    [MaxLength(100)]
    public string RequiredSpecialization { get; set; } = string.Empty;

    public DateTime? AdmissionTime { get; set; }

    [Required]
    [MaxLength(30)]
    public string Condition { get; set; } = "Serious";

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Admitted";
}

public class HospitalAutoRoutePatientRequest
{
    [Range(1, int.MaxValue)]
    public int PatientId { get; set; }

    public int? ReportId { get; set; }

    [Required]
    [MaxLength(100)]
    public string RequiredSpecialization { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int BedRequirement { get; set; } = 1;

    [MaxLength(120)]
    public string? PreferredCity { get; set; }

    [MaxLength(120)]
    public string? PreferredProvince { get; set; }

    public DateTime? AdmissionTime { get; set; }

    [Required]
    [MaxLength(30)]
    public string Condition { get; set; } = "Serious";

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Admitted";
}

public class AutoRoutePatientResultDto
{
    public bool Routed { get; set; }

    public bool EscalationRequired { get; set; }

    public string? EscalationLevel { get; set; }

    public int? PatientId { get; set; }

    public int? ReportId { get; set; }

    public string RequiredSpecialization { get; set; } = string.Empty;

    public int BedRequirement { get; set; }

    public string? PreferredCity { get; set; }

    public string? PreferredProvince { get; set; }

    public string? RoutingTierUsed { get; set; }

    public bool FallbackApplied { get; set; }

    public int CandidateCount { get; set; }

    public int? SelectedHospitalId { get; set; }

    public string? SelectedHospitalName { get; set; }

    public string? SelectedHospitalCity { get; set; }

    public string? SelectedHospitalProvince { get; set; }

    public int? SelectedHospitalAvailableBeds { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<string> SuggestedActions { get; set; } = new();

    public PatientAdmissionDto? Admission { get; set; }
}

public class PatientDto
{
    public int PatientId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public int? Age { get; set; }

    public string? Gender { get; set; }

    public string? NationalId { get; set; }

    public string? BloodType { get; set; }

    public string? ContactPhone { get; set; }
}

public class PatientCreateDto
{
    [Required]
    [MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Range(0, 150)]
    public int? Age { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(30)]
    public string? NationalId { get; set; }

    [MaxLength(5)]
    public string? BloodType { get; set; }

    [MaxLength(30)]
    public string? ContactPhone { get; set; }
}

public class PatientAdmissionDto
{
    public int AdmissionId { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int HospitalId { get; set; }

    public string HospitalName { get; set; } = string.Empty;

    public int? ReportId { get; set; }

    public string? ReportCity { get; set; }

    public DateTime AdmissionTime { get; set; }

    public DateTime? DischargeTime { get; set; }

    public string Condition { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? LengthOfStayHours { get; set; }
}

public class PatientAdmissionCreateDto
{
    [Range(1, int.MaxValue)]
    public int PatientId { get; set; }

    [Range(1, int.MaxValue)]
    public int HospitalId { get; set; }

    public int? ReportId { get; set; }

    public DateTime AdmissionTime { get; set; }

    public DateTime? DischargeTime { get; set; }

    [Required]
    [MaxLength(30)]
    public string Condition { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Admitted";
}

public class PatientAdmissionStatusUpdateDto
{
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;
}

// ============================================================================
// DATABASE VIEW DTOs
// ============================================================================

public class HospitalCapacityDto
{
    public int HospitalId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public int TotalBeds { get; set; }
    public int AvailableBeds { get; set; }
    public decimal? OccupancyRate { get; set; }
}

public class PatientAdmissionsViewDto
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
// STORED PROCEDURE DTOs
// ============================================================================

public class PatientAdmitRequestDto
{
    [Range(1, int.MaxValue)]
    public int HospitalId { get; set; }

    [Required]
    [MaxLength(30)]
    public string Condition { get; set; } = string.Empty;

    public int? ReportId { get; set; }
}

public class PatientDischargeRequestDto
{
    [MaxLength(500)]
    public string? DischargeCondition { get; set; }
}

public class PatientAdmitSpRequestDto
{
    [Range(1, int.MaxValue)]
    public int PatientID { get; set; }

    [Range(1, int.MaxValue)]
    public int HospitalID { get; set; }

    [Range(1, int.MaxValue)]
    public int AdmittedBy { get; set; }

    [MaxLength(500)]
    public string? Diagnosis { get; set; }

    [MaxLength(30)]
    public string? Severity { get; set; }
}

public class PatientDischargeSpRequestDto
{
    [Range(1, int.MaxValue)]
    public int DischargedBy { get; set; }

    [MaxLength(1000)]
    public string? DischargeNotes { get; set; }
}

public class PatientAdmissionResult
{
    public string ResultStatus { get; set; } = string.Empty;
    public int AdmissionID { get; set; }
    public int AvailableBeds { get; set; }
}

public class PatientDischargeResult
{
    public string ResultStatus { get; set; } = string.Empty;
    public int AdmissionID { get; set; }
    public DateTime? DischargeTime { get; set; }
}
