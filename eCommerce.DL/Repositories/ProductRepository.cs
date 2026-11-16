using eCommerce.Domain.Entities;
using eCommerce.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.DL.Repositories;

public sealed class ProductRepository(CartDbContext db) : IProductRepository
{
    public Task<Product?> GetAsync(string id, CancellationToken ct) =>
        StorageOperation.RunAsync(() => db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct));

    public async Task<IReadOnlyList<Product>> ListAsync(int offset, int limit, string? search, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Id.Contains(search) || p.Name.Contains(search));

        return await StorageOperation.RunAsync(() => query.OrderBy(p => p.Id).Skip(offset).Take(limit).ToListAsync(ct));
    }
}

