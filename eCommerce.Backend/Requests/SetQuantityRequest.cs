using System.ComponentModel.DataAnnotations;
namespace eCommerce.Backend.Requests;
public sealed record SetQuantityRequest([Range(1, 99)] int Quantity);

