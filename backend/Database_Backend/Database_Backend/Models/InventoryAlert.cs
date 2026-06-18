using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class InventoryAlert
{
    public int InventoryId { get; set; }

    public int AlertId { get; set; }

    public string AlertType { get; set; } = null!;

    public DateTime AlertTime { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ResolvedAt { get; set; }

    public virtual Inventory Inventory { get; set; } = null!;
}
