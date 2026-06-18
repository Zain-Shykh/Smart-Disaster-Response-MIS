using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class Hospital
{
    public int HospitalId { get; set; }

    public string HospitalName { get; set; } = null!;

    public string Street { get; set; } = null!;

    public string Area { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Province { get; set; } = null!;

    public int TotalBeds { get; set; }

    public int AvailableBeds { get; set; }

    public decimal? OccupancyRate { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    public virtual ICollection<HospitalSpecialization> HospitalSpecializations { get; set; } = new List<HospitalSpecialization>();

    public virtual ICollection<PatientAdmission> PatientAdmissions { get; set; } = new List<PatientAdmission>();
}
