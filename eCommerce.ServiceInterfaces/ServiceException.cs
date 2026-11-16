namespace eCommerce.ServiceInterfaces;
public sealed class ServiceException(ServiceError error, string message) : Exception(message)
{
    public ServiceError Error { get; } = error;
}

