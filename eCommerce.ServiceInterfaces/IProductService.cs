using eCommerce.ServiceInterfaces.Models;

namespace eCommerce.ServiceInterfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> ListAsync(int offset, int limit, string? search, CancellationToken ct);
}

