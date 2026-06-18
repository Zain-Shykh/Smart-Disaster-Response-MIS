using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class CitizenPhone
{
    public int CitizenId { get; set; }

    public string Phone { get; set; } = null!;

    public virtual Citizen Citizen { get; set; } = null!;
}
