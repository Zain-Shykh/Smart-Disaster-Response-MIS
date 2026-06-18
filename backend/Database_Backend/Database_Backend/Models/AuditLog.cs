using System;
using System.Collections.Generic;

namespace Database_Backend.Models;

public partial class AuditLog
{
    public long LogId { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public string RecordId { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime Timestamp { get; set; }

    public string? Ipaddress { get; set; }

    public virtual User? User { get; set; }
}
