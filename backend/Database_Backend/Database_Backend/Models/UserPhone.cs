using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class UserPhone
{
    public int UserId { get; set; }

    public string Phone { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
