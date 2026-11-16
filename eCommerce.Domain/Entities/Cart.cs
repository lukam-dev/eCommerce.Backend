using System;
using System.Collections.Generic;

namespace eCommerce.Domain.Entities;

public partial class Cart
{
    public Guid Id { get; set; }

    public string OwnerId { get; set; } = null!;

    public string Currency { get; set; } = null!;

    public int Version { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
