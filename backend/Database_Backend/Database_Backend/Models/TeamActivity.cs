using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class TeamActivity
{
    public int TeamId { get; set; }

    public int ActivityId { get; set; }

    public string ActivityType { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? DurationMinutes { get; set; }

    public string? Notes { get; set; }

    public string? Outcome { get; set; }

    public virtual RescueTeam Team { get; set; } = null!;
}
