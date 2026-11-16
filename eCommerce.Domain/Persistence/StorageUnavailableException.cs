namespace eCommerce.Domain.Persistence;
public sealed class StorageUnavailableException(Exception inner) : Exception("Storage is temporarily unavailable.", inner);

