using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class Inventory
{
    public int InventoryId { get; set; }

    public int WarehouseId { get; set; }

    public int ResourceId { get; set; }

    public decimal Quantity { get; set; }

    public decimal MinThreshold { get; set; }

    public decimal MaxCapacity { get; set; }

    public DateTime LastUpdated { get; set; }

    public virtual ICollection<InventoryAlert> InventoryAlerts { get; set; } = new List<InventoryAlert>();

    public virtual Resource Resource { get; set; } = null!;

    public virtual ICollection<ResourceAllocation> ResourceAllocations { get; set; } = new List<ResourceAllocation>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
