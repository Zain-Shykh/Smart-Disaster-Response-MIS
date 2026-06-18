using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class PatientAdmission
{
    public int AdmissionId { get; set; }

    public int PatientId { get; set; }

    public int HospitalId { get; set; }

    public int? ReportId { get; set; }

    public DateTime AdmissionTime { get; set; }

    public DateTime? DischargeTime { get; set; }

    public string Condition { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int? LengthOfStayHours { get; set; }

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual EmergencyReport? Report { get; set; }
}
