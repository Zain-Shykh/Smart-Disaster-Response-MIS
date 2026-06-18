using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class Patient
{
    public int PatientId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public int? Age { get; set; }

    public string? Gender { get; set; }

    public string? NationalId { get; set; }

    public string? BloodType { get; set; }

    public string? ContactPhone { get; set; }

    public virtual ICollection<PatientAdmission> PatientAdmissions { get; set; } = new List<PatientAdmission>();
}
