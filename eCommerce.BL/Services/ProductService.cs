using eCommerce.Domain.Persistence;
using eCommerce.ServiceInterfaces;
using eCommerce.ServiceInterfaces.Models;

namespace eCommerce.BL.Services;

public sealed class ProductService(IProductRepository products) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> ListAsync(int offset, int limit, string? search, CancellationToken ct)
    {
        if (offset < 0 || limit is < 1 or > 100 || search?.Length > 200)
            throw new ServiceException(ServiceError.InvalidInput, "Offset must be non-negative, limit between 1 and 100, and search at most 200 characters.");

        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        return (await products.ListAsync(offset, limit, search, ct))
            .Select(p => new ProductResponse(p.Id, p.Name, p.Currency, p.UnitPriceMinor)).ToArray();
    }
}

