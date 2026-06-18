using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class ApprovalHistory
{
    public int RequestId { get; set; }

    public int HistoryId { get; set; }

    public int ActionBy { get; set; }

    public DateTime ActionTime { get; set; }

    public string Decision { get; set; } = null!;

    public string? Comments { get; set; }

    public virtual User ActionByNavigation { get; set; } = null!;

    public virtual ApprovalRequest Request { get; set; } = null!;
}
