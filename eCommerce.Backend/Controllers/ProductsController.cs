using System.ComponentModel.DataAnnotations;
using eCommerce.ServiceInterfaces;
using eCommerce.ServiceInterfaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eCommerce.Backend.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize(Policy = "CartOwner")]
[EnableRateLimiting("cart")]
public sealed class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> Get(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, 100)] int limit = 10,
        [FromQuery, StringLength(200)] string? search = null,
        CancellationToken ct = default) =>
        Ok(await products.ListAsync(offset, limit, search, ct));
}

