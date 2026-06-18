using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class ResourceAllocation
{
    public int AllocationId { get; set; }

    public int InventoryId { get; set; }

    public int EventId { get; set; }

    public int RequestedBy { get; set; }

    public decimal Quantity { get; set; }

    public DateTime RequestTime { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? DispatchedAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public virtual ICollection<ApprovalRequest> ApprovalRequests { get; set; } = new List<ApprovalRequest>();

    public virtual DisasterEvent Event { get; set; } = null!;

    public virtual Inventory Inventory { get; set; } = null!;

    public virtual User RequestedByNavigation { get; set; } = null!;
}
