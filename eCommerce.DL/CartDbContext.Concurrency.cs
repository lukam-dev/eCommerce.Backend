using eCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace eCommerce.DL;
public partial class CartDbContext
{
    // Hand-maintained extension: reverse engineering cannot infer an application-managed version token.
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Cart>().Property(c => c.Version).IsConcurrencyToken();
}

