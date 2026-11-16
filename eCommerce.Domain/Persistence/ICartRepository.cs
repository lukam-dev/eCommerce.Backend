using eCommerce.Domain.Entities;
namespace eCommerce.Domain.Persistence;
public interface ICartRepository
{
    Task<Cart?> GetOwnedAsync(Guid id, string ownerId, CancellationToken ct);
    void Add(Cart cart);
    Task SaveChangesAsync(CancellationToken ct);
}

