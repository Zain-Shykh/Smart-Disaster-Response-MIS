using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class ApprovalRequest
{
    public int RequestId { get; set; }

    public int RequestedBy { get; set; }

    public int? ReviewedBy { get; set; }

    public string RequestType { get; set; } = null!;

    public DateTime RequestTime { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public int? AllocationId { get; set; }

    public int? AssignmentId { get; set; }

    public int? ExpenseId { get; set; }

    public virtual ResourceAllocation? Allocation { get; set; }

    public virtual ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();

    public virtual TeamAssignment? Assignment { get; set; }

    public virtual Expense? Expense { get; set; }

    public virtual User RequestedByNavigation { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }
}
