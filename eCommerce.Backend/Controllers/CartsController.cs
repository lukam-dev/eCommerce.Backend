using System.Globalization;
using System.Security.Claims;
using eCommerce.Backend.Requests;
using eCommerce.ServiceInterfaces;
using eCommerce.ServiceInterfaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eCommerce.Backend.Controllers;

[ApiController]
[Route("api/v1/carts")]
[Authorize(Policy = "CartOwner")]
[EnableRateLimiting("cart")]
public sealed class CartsController(ICartService carts) : ControllerBase
{
    private string Owner => User.FindFirstValue("sub")!;

    [HttpPost]
    public async Task<ActionResult<CartResponse>> Create(CreateCartRequest request, CancellationToken ct)
    {
        var cart = await carts.CreateAsync(Owner, request.Currency, ct);
        SetEtag(cart);
        return CreatedAtAction(nameof(Get), new { id = cart.Id }, cart);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CartResponse>> Get(Guid id, CancellationToken ct)
    {
        var cart = await carts.GetAsync(id, Owner, ct);
        SetEtag(cart);
        return Ok(cart);
    }

    [HttpPut("{id:guid}/items/{productId}")]
    public async Task<ActionResult<CartResponse>> SetItem(Guid id, string productId, SetQuantityRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        var error = ParseVersion(ifMatch, out var version);
        if (error is not null) return error;
        var cart = await carts.SetItemAsync(id, Owner, productId, request.Quantity, version, ct);
        SetEtag(cart);
        return Ok(cart);
    }

    [HttpDelete("{id:guid}/items/{productId}")]
    public async Task<ActionResult<CartResponse>> RemoveItem(Guid id, string productId,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        var error = ParseVersion(ifMatch, out var version);
        if (error is not null) return error;
        var cart = await carts.RemoveItemAsync(id, Owner, productId, version, ct);
        SetEtag(cart);
        return Ok(cart);
    }

    private ObjectResult? ParseVersion(string? value, out int version)
    {
        version = 0;
        if (value is null) return Problem(statusCode: 428, title: "Supply the ETag from the last cart response in If-Match.");
        if (value.Length < 3 || value[0] != '"' || value[^1] != '"' ||
            !int.TryParse(value[1..^1], NumberStyles.None, CultureInfo.InvariantCulture, out version) ||
            value != $"\"{version}\"" || version < 1)
            return Problem(statusCode: 412, title: "Supply a single strong cart ETag, for example \"1\".");
        return null;
    }

    private void SetEtag(CartResponse cart) => Response.Headers.ETag = $"\"{cart.Version}\"";
}

