using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class Donation
{
    public int DonationId { get; set; }

    public int DonorId { get; set; }

    public int EventId { get; set; }

    public decimal Amount { get; set; }

    public DateTime DonationDate { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? ReceiptNumber { get; set; }

    public virtual Donor Donor { get; set; } = null!;

    public virtual DisasterEvent Event { get; set; } = null!;
}
