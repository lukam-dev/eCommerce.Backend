using System.Net;
using System.Net.Http.Json;
using eCommerce.DL;
using eCommerce.ServiceInterfaces.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CartApi.Tests;

public sealed partial class CartTests : IAsyncLifetime
{

    private readonly WebApplicationFactory<Program> factory;
    public CartTests()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Cart"] = ConnectionString,
                ["Authentication:DemoEnabled"] = "true"
            }));
        });
    }
    private HttpClient Client(string? owner = "alice")
    {
        var client = factory.CreateClient();
        if (owner is not null) client.DefaultRequestHeaders.Add("X-Demo-User", owner);
        return client;
    }
    private static async Task<(CartResponse Cart, string Tag)> Create(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/carts", new { currency = "EUR" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return ((await response.Content.ReadFromJsonAsync<CartResponse>())!, response.Headers.ETag!.ToString());
    }
    private static Task<HttpResponseMessage> Put(HttpClient client, Guid id, string product, int quantity, string? tag)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/carts/{id}/items/{product}")
            { Content = JsonContent.Create(new { quantity }) };
        if (tag is not null) request.Headers.TryAddWithoutValidation("If-Match", tag);
        return client.SendAsync(request);
    }

    [Fact]
    public async Task Authentication_and_owner_isolation()
    {
        using var anonymous = Client(null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/carts", new { currency = "EUR" })).StatusCode);
        using var alice = Client();
        var (cart, tag) = await Create(alice);
        using var differentCase = Client("ALICE");
        Assert.Equal(HttpStatusCode.NotFound, (await differentCase.GetAsync($"/api/v1/carts/{cart.Id}")).StatusCode);
        using var bob = Client("bob");
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/v1/carts/{cart.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Put(bob, cart.Id, "mouse", 1, tag)).StatusCode);
        var deletion = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/carts/{cart.Id}/items/mouse");
        deletion.Headers.TryAddWithoutValidation("If-Match", tag);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.SendAsync(deletion)).StatusCode);
    }

    [Fact]
    public async Task Cart_lifecycle_prices_and_persistence()
    {
        using var client = Client();
        var (cart, tag) = await Create(client);
        var added = await Put(client, cart.Id, "keyboard", 2, tag);
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        var result = (await added.Content.ReadFromJsonAsync<CartResponse>())!;
        Assert.Equal(15980, result.TotalMinor);
        Assert.Equal(2, result.Version);
        // New request and new DbContext read persisted rows rather than process memory.
        using var other = Client();
        var reloaded = await other.GetFromJsonAsync<CartResponse>($"/api/v1/carts/{cart.Id}");
        Assert.Equal(result, reloaded! with { Items = result.Items });
        Assert.Single(reloaded!.Items);
        Assert.Equal(2, reloaded.Items[0].Quantity);
        var deleted = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/carts/{cart.Id}/items/keyboard");
        deleted.Headers.TryAddWithoutValidation("If-Match", added.Headers.ETag!.ToString());
        var response = await client.SendAsync(deleted);
        Assert.Empty((await response.Content.ReadFromJsonAsync<CartResponse>())!.Items);
    }

    [Fact]
    public async Task Validates_input_product_and_preconditions()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/carts", new { currency = "USD" })).StatusCode);
        var (cart, tag) = await Create(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await Put(client, cart.Id, "mouse", 0, tag)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Put(client, cart.Id, "mouse", 100, tag)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Put(client, cart.Id, "missing", 1, tag)).StatusCode);
        Assert.Equal(428, (int)(await Put(client, cart.Id, "mouse", 1, null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Put(client, cart.Id, "mouse", 1, tag)).StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await Put(client, cart.Id, "mouse", 2, tag)).StatusCode);
        Assert.Equal(2490, (await client.GetFromJsonAsync<CartResponse>($"/api/v1/carts/{cart.Id}"))!.TotalMinor);
    }

    [Fact]
    public async Task Competing_writes_cannot_overwrite_each_other()
    {
        using var client = Client();
        var (cart, tag) = await Create(client);
        var responses = await Task.WhenAll(Put(client, cart.Id, "mouse", 2, tag), Put(client, cart.Id, "keyboard", 1, tag));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.PreconditionFailed);
        var stored = (await client.GetFromJsonAsync<CartResponse>($"/api/v1/carts/{cart.Id}"))!;
        Assert.Single(stored.Items);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Expired_carts_are_not_accessible()
    {
        using var client = Client();
        var (cart, tag) = await Create(client);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();
            var entity = await db.Carts.SingleAsync(c => c.Id == cart.Id);
            entity.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/carts/{cart.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Put(client, cart.Id, "mouse", 1, tag)).StatusCode);
    }

    [Fact]
    public async Task Health_and_rate_limit()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        var (cart, _) = await Create(client);
        HttpResponseMessage? response = null;
        for (var i = 0; i < 120; i++) response = await client.GetAsync($"/api/v1/carts/{cart.Id}");
        Assert.Equal(HttpStatusCode.TooManyRequests, response!.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }
}


