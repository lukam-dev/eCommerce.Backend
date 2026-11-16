namespace eCommerce.Domain.Persistence;
public sealed class ConcurrencyException(Exception inner) : Exception("The cart was modified by another request.", inner);

