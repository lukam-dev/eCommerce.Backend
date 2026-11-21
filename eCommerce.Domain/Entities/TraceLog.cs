using System;
using System.Collections.Generic;

namespace eCommerce.Domain.Entities;

public partial class TraceLog
{
    public long Id { get; set; }

    public string TraceId { get; set; } = null!;

    public string Method { get; set; } = null!;

    public string? RouteTemplate { get; set; }

    public int StatusCode { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public long DurationMs { get; set; }

    public virtual TraceLogError? TraceLogError { get; set; }
}
