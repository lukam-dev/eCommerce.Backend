using eCommerce.ServiceInterfaces.Models;
namespace eCommerce.ServiceInterfaces;
public interface ICartService
{
    Task<CartResponse> CreateAsync(string ownerId, string currency, CancellationToken ct);
    Task<CartResponse> GetAsync(Guid id, string ownerId, CancellationToken ct);
    Task<CartResponse> SetItemAsync(Guid id, string ownerId, string productId, int quantity, int expectedVersion, CancellationToken ct);
    Task<CartResponse> RemoveItemAsync(Guid id, string ownerId, string productId, int expectedVersion, CancellationToken ct);
}

