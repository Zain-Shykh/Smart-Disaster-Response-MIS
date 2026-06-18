using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class DonorPhone
{
    public int DonorId { get; set; }

    public string Phone { get; set; } = null!;

    public virtual Donor Donor { get; set; } = null!;
}
