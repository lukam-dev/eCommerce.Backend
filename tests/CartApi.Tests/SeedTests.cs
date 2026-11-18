using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using eCommerce.Database;
using eCommerce.ServiceInterfaces.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CartApi.Tests;
public sealed partial class CartTests
{
    [Fact]
    public async Task Seed_is_readable_through_api_and_rerun_preserves_user_changes()
    {
        using var client = Client("demo");
        var products = (await client.GetFromJsonAsync<ProductResponse[]>("/api/v1/products"))!;
        Assert.Equal(3, products.Length);
        Assert.Contains(products, p => p.Id == "monitor" && p.UnitPriceMinor == 24900);
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var response = await client.GetAsync($"/api/v1/carts/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var seeded = (await response.Content.ReadFromJsonAsync<CartResponse>())!;
        Assert.Equal(12970, seeded.TotalMinor);
        Assert.Equal(2, seeded.Items.Count);
        var update = await Put(client, id, "mouse", 3, response.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        await DatabaseMigrator.ApplyAsync(ConnectionString, seedDemo: true);
        var preserved = (await client.GetFromJsonAsync<CartResponse>($"/api/v1/carts/{id}"))!;
        Assert.Equal(3, preserved.Items.Single(i => i.ProductId == "mouse").Quantity);
        Assert.Equal(2, preserved.Version);
        using var other = Client("alice");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/carts/{id}")).StatusCode);
    }

    [Fact]
    public async Task Swagger_exposes_demo_authentication_and_version_header()
    {
        using var client = Client();
        var schema = await client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("X-Demo-User", schema);
        Assert.Contains("If-Match", schema);
        Assert.Contains("/api/v1/products", schema);
        using var document = JsonDocument.Parse(schema);
        var security = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("apiKey", security.GetProperty("type").GetString());
        Assert.Equal("X-Demo-User", security.GetProperty("name").GetString());
        Assert.True(document.RootElement.GetProperty("security")[0].TryGetProperty("Bearer", out _));
    }
}
