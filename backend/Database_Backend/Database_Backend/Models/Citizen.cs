using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class Citizen
{
    public int CitizenId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string NationalId { get; set; } = null!;

    public string? Email { get; set; }

    public string Street { get; set; } = null!;

    public string Area { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Province { get; set; } = null!;

    public int TotalReports { get; set; }

    public virtual ICollection<CitizenPhone> CitizenPhones { get; set; } = new List<CitizenPhone>();

    public virtual ICollection<EmergencyReport> EmergencyReports { get; set; } = new List<EmergencyReport>();
}
