using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class Expense
{
    public int ExpenseId { get; set; }

    public int EventId { get; set; }

    public int? ApprovedBy { get; set; }

    public string Category { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime ExpenseDate { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public virtual ICollection<ApprovalRequest> ApprovalRequests { get; set; } = new List<ApprovalRequest>();

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual DisasterEvent Event { get; set; } = null!;
}
