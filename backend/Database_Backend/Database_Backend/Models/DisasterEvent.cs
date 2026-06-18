using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class DisasterEvent
{
    public int EventId { get; set; }

    public string EventName { get; set; } = null!;

    public string DisasterType { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? DurationMinutes { get; set; }

    public string Street { get; set; } = null!;

    public string Area { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Province { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int AffectedPopulation { get; set; }

    public int TotalReports { get; set; }

    public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();

    public virtual ICollection<EmergencyReport> EmergencyReports { get; set; } = new List<EmergencyReport>();

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<ResourceAllocation> ResourceAllocations { get; set; } = new List<ResourceAllocation>();

    public virtual ICollection<TeamAssignment> TeamAssignments { get; set; } = new List<TeamAssignment>();
}
