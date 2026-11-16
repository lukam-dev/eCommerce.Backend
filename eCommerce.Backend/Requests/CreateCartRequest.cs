using System.ComponentModel.DataAnnotations;
namespace eCommerce.Backend.Requests;
public sealed record CreateCartRequest([Required, RegularExpression("^EUR$")] string Currency);

