using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class HospitalSpecialization
{
    public int HospitalId { get; set; }

    public string Specialization { get; set; } = null!;

    public virtual Hospital Hospital { get; set; } = null!;
}
