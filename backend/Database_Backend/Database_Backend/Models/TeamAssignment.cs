using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class TeamAssignment
{
    public int AssignmentId { get; set; }

    public int TeamId { get; set; }

    public int EventId { get; set; }

    public int AssignedBy { get; set; }

    public DateTime AssignmentTime { get; set; }

    public DateTime? CompletionTime { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<ApprovalRequest> ApprovalRequests { get; set; } = new List<ApprovalRequest>();

    public virtual User AssignedByNavigation { get; set; } = null!;

    public virtual DisasterEvent Event { get; set; } = null!;

    public virtual RescueTeam Team { get; set; } = null!;
}
