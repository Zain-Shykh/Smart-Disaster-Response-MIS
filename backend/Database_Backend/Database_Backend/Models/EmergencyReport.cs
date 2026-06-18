using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class EmergencyReport
{
    public int ReportId { get; set; }

    public int CitizenId { get; set; }

    public int? EventId { get; set; }

    public string Street { get; set; } = null!;

    public string Area { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Province { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string DisasterType { get; set; } = null!;

    public string SeverityLevel { get; set; } = null!;

    public DateTime ReportTime { get; set; }

    public string Status { get; set; } = null!;

    public string Source { get; set; } = null!;

    public string? Description { get; set; }

    public int? ResponseTimeMinutes { get; set; }

    public int? ResolutionTimeMinutes { get; set; }

    public virtual Citizen Citizen { get; set; } = null!;

    public virtual DisasterEvent? Event { get; set; }

    public virtual ICollection<PatientAdmission> PatientAdmissions { get; set; } = new List<PatientAdmission>();


}
