using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class RescueTeam
{
    public int TeamId { get; set; }

    public string TeamName { get; set; } = null!;

    public string TeamType { get; set; } = null!;

    public string Street { get; set; } = null!;

    public string Area { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Province { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string AvailabilityStatus { get; set; } = null!;

    public int Capacity { get; set; }

    public int TotalAssignments { get; set; }

    public virtual ICollection<RescueTeamSpecialization> RescueTeamSpecializations { get; set; } = new List<RescueTeamSpecialization>();

    public virtual ICollection<TeamActivity> TeamActivities { get; set; } = new List<TeamActivity>();

    public virtual ICollection<TeamAssignment> TeamAssignments { get; set; } = new List<TeamAssignment>();
}
