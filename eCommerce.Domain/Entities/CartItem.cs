using System;
using System.Collections.Generic;

namespace eCommerce.Domain.Entities;

public partial class CartItem
{
    public Guid CartId { get; set; }

    public string ProductId { get; set; } = null!;

    public int Quantity { get; set; }

    public long UnitPriceMinor { get; set; }

    public virtual Cart Cart { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
