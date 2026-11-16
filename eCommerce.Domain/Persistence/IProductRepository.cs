using eCommerce.Domain.Entities;

namespace eCommerce.Domain.Persistence;

public interface IProductRepository
{
    Task<Product?> GetAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<Product>> ListAsync(int offset, int limit, string? search, CancellationToken ct);
}

