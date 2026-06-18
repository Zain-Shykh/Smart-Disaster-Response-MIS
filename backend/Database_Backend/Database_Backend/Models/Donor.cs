using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class Donor
{
    public int DonorId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string DonorType { get; set; } = null!;

    public string? OrganizationName { get; set; }

    public string? Email { get; set; }

    public string Street { get; set; } = null!;

    public string Area { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Province { get; set; } = null!;

    public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();

    public virtual ICollection<DonorPhone> DonorPhones { get; set; } = new List<DonorPhone>();
}
