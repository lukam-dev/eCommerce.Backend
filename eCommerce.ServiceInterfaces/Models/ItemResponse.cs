namespace eCommerce.ServiceInterfaces.Models;
public sealed record ItemResponse(string ProductId, int Quantity, long UnitPriceMinor, long LineTotalMinor);

