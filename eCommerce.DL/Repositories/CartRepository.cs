using eCommerce.Domain.Entities;
using eCommerce.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
namespace eCommerce.DL.Repositories;
public sealed class CartRepository(CartDbContext db) : ICartRepository
{
    public Task<Cart?> GetOwnedAsync(Guid id, string ownerId, CancellationToken ct) =>
        StorageOperation.RunAsync(() => db.Carts.Include(c => c.CartItems).SingleOrDefaultAsync(c => c.Id == id && c.OwnerId == ownerId, ct));
    public void Add(Cart cart) => db.Carts.Add(cart);
    public async Task SaveChangesAsync(CancellationToken ct) =>
        await StorageOperation.RunAsync(() => db.SaveChangesAsync(ct));
}


