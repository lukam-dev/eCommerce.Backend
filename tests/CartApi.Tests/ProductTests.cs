using System.Net;
using System.Net.Http.Json;
using eCommerce.DL;
using eCommerce.Domain.Entities;
using eCommerce.ServiceInterfaces.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CartApi.Tests;

public sealed partial class CartTests
{
    [Fact]
    public async Task Products_use_default_limit_and_apply_search_before_pagination()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();
            db.Products.AddRange(Enumerable.Range(0, 12).Select(i => new Product
            {
                Id = $"test-{i:D2}", Name = $"Pagination sample {i:D2}", Currency = "EUR", UnitPriceMinor = 100
            }));
            await db.SaveChangesAsync();
        }
        using var client = Client();
        var all = (await client.GetFromJsonAsync<ProductResponse[]>("/api/v1/products?limit=100"))!;
        var first = (await client.GetFromJsonAsync<ProductResponse[]>("/api/v1/products"))!;
        Assert.Equal(15, all.Length);
        Assert.Equal(10, first.Length);
        Assert.Equal(all.Take(10).Select(p => p.Id), first.Select(p => p.Id));

        var next = (await client.GetFromJsonAsync<ProductResponse[]>("/api/v1/products?offset=10"))!;
        Assert.Equal(all.Skip(10).Select(p => p.Id), next.Select(p => p.Id));

        var filtered = (await client.GetFromJsonAsync<ProductResponse[]>("/api/v1/products?search=Pagination%20sample&offset=2&limit=3"))!;
        Assert.Equal(new[] { "test-02", "test-03", "test-04" }, filtered.Select(p => p.Id));

        var byId = (await client.GetFromJsonAsync<ProductResponse[]>("/api/v1/products?search=%20test-11%20"))!;
        Assert.Equal("test-11", Assert.Single(byId).Id);

        var blank = (await client.GetFromJsonAsync<ProductResponse[]>("/api/v1/products?search=%20%20&limit=100"))!;
        Assert.Equal(all.Select(p => p.Id), blank.Select(p => p.Id));
        Assert.Empty((await client.GetFromJsonAsync<ProductResponse[]>("/api/v1/products?offset=100"))!);
        Assert.Empty((await client.GetFromJsonAsync<ProductResponse[]>("/api/v1/products?search=missing-product"))!);
    }

    [Fact]
    public async Task Products_reject_invalid_pagination_and_search_parameters()
    {
        using var client = Client();
        var queries = new[] { "offset=-1", "limit=0", "limit=-1", "limit=101", "offset=invalid", "limit=invalid", "search=" + new string('x', 201) };
        foreach (var query in queries)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/products?" + query)).StatusCode);
    }
}

