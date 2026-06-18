using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class Resource
{
    public int ResourceId { get; set; }

    public string ResourceName { get; set; } = null!;

    public string ResourceType { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
