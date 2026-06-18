using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class RescueTeamSpecialization
{
    public int TeamId { get; set; }

    public string Specialization { get; set; } = null!;

    public virtual RescueTeam Team { get; set; } = null!;
}
