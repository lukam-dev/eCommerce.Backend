using System;
using System.Collections.Generic;

namespace eCommerce.Domain.Entities;

public partial class TraceLogError
{
    public long TraceLogId { get; set; }

    public string? ExceptionType { get; set; }

    public virtual TraceLog TraceLog { get; set; } = null!;
}
