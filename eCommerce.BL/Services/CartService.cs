using eCommerce.Domain.Entities;
using eCommerce.Domain.Persistence;
using eCommerce.ServiceInterfaces;
using eCommerce.ServiceInterfaces.Models;

namespace eCommerce.BL.Services;

public sealed class CartService(ICartRepository carts, IProductRepository products) : ICartService
{
    public async Task<CartResponse> CreateAsync(string ownerId, string currency, CancellationToken ct)
    {
        if (currency != "EUR") throw new ServiceException(ServiceError.InvalidInput, "Only EUR is supported.");
        var cart = new Cart { OwnerId = ownerId, Currency = currency };
        carts.Add(cart);
        await carts.SaveChangesAsync(ct);
        return Map(cart);
    }

    public async Task<CartResponse> GetAsync(Guid id, string ownerId, CancellationToken ct) =>
        Map(await LoadAsync(id, ownerId, ct));

    public async Task<CartResponse> SetItemAsync(Guid id, string ownerId, string productId, int quantity, int expectedVersion, CancellationToken ct)
    {
        if (quantity is < 1 or > 99) throw new ServiceException(ServiceError.InvalidInput, "Quantity must be between 1 and 99.");
        var cart = await LoadAsync(id, ownerId, ct);
        CheckVersion(cart, expectedVersion);
        var product = await products.GetAsync(productId, ct)
            ?? throw new ServiceException(ServiceError.NotFound, "Product not found.");
        if (product.Currency != cart.Currency) throw new ServiceException(ServiceError.CurrencyMismatch, "Currency mismatch.");
        var item = cart.CartItems.SingleOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            if (cart.CartItems.Count >= 100) throw new ServiceException(ServiceError.CartLimitExceeded, "A cart can contain at most 100 distinct products.");
            item = new CartItem { CartId = id, ProductId = productId };
            cart.CartItems.Add(item);
        }
        item.Quantity = quantity;
        item.UnitPriceMinor = product.UnitPriceMinor;
        return await SaveAsync(cart, ct);
    }

    public async Task<CartResponse> RemoveItemAsync(Guid id, string ownerId, string productId, int expectedVersion, CancellationToken ct)
    {
        var cart = await LoadAsync(id, ownerId, ct);
        CheckVersion(cart, expectedVersion);
        var item = cart.CartItems.SingleOrDefault(i => i.ProductId == productId);
        if (item is not null) cart.CartItems.Remove(item);
        return await SaveAsync(cart, ct);
    }

    private async Task<Cart> LoadAsync(Guid id, string ownerId, CancellationToken ct)
    {
        var cart = await carts.GetOwnedAsync(id, ownerId, ct);
        if (cart is null || cart.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new ServiceException(ServiceError.NotFound, "Cart not found.");
        return cart;
    }

    private static void CheckVersion(Cart cart, int expectedVersion)
    {
        if (cart.Version != expectedVersion)
            throw new ServiceException(ServiceError.VersionConflict, "Cart has changed. Read it again before retrying.");
    }

    private async Task<CartResponse> SaveAsync(Cart cart, CancellationToken ct)
    {
        cart.Version++;
        try { await carts.SaveChangesAsync(ct); }
        catch (ConcurrencyException)
        {
            throw new ServiceException(ServiceError.VersionConflict, "Cart has changed. Read it again before retrying.");
        }
        return Map(cart);
    }

    private static CartResponse Map(Cart cart)
    {
        var items = cart.CartItems.OrderBy(i => i.ProductId).Select(i =>
            new ItemResponse(i.ProductId, i.Quantity, i.UnitPriceMinor, checked(i.Quantity * i.UnitPriceMinor))).ToArray();
        return new(cart.Id, cart.Currency, cart.Version, cart.ExpiresAt, items, items.Sum(i => i.LineTotalMinor));
    }
}


