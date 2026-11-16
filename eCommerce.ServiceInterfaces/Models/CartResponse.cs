namespace eCommerce.ServiceInterfaces.Models;
public sealed record CartResponse(Guid Id, string Currency, int Version, DateTimeOffset ExpiresAt, IReadOnlyList<ItemResponse> Items, long TotalMinor);

