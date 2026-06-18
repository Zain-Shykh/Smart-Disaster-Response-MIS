using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public string WarehouseName { get; set; } = null!;

    public string Street { get; set; } = null!;

    public string Area { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Province { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int Capacity { get; set; }

    public int ManagerId { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual User Manager { get; set; } = null!;
}
