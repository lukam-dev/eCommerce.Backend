using System.Net;
using System.Text.Json;
using eCommerce.DL;
using eCommerce.Domain.Entities;
using eCommerce.ServiceInterfaces;
using eCommerce.ServiceInterfaces.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CartApi.Tests;

public sealed partial class CartTests
{
    [Fact]
    public async Task Api_requests_store_route_templates_and_correlated_server_errors()
    {
        using var tracedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IProductService>(_ => new FailingProductService())));
        using var client = tracedFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Demo-User", "demo");

        using var success = await client.GetAsync("/api/v1/carts/11111111-1111-1111-1111-111111111111");
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        var successId = Assert.Single(success.Headers.GetValues("X-Trace-Id"));

        using var failure = await client.GetAsync("/api/v1/products?search=private-input");
        Assert.Equal(HttpStatusCode.InternalServerError, failure.StatusCode);
        var failureId = Assert.Single(failure.Headers.GetValues("X-Trace-Id"));
        using var body = JsonDocument.Parse(await failure.Content.ReadAsStringAsync());
        Assert.Equal(failureId, body.RootElement.GetProperty("traceId").GetString());

        using var scope = tracedFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        var entries = new List<TraceLog>();
        for (var attempt = 0; attempt < 50 && entries.Count < 2; attempt++)
        {
            entries = await db.TraceLogs.AsNoTracking().Include(log => log.TraceLogError)
                .Where(log => log.TraceId == successId || log.TraceId == failureId).ToListAsync();
            if (entries.Count < 2)
                await Task.Delay(100);
        }

        var requestLog = Assert.Single(entries, log => log.TraceId == successId);
        Assert.Equal("GET", requestLog.Method);
        Assert.Equal(200, requestLog.StatusCode);
        Assert.Equal("api/v1/carts/{id:guid}", requestLog.RouteTemplate);
        Assert.True(requestLog.DurationMs >= 0);
        Assert.Null(requestLog.TraceLogError);

        var errorLog = Assert.Single(entries, log => log.TraceId == failureId);
        Assert.Equal(500, errorLog.StatusCode);
        Assert.Equal("api/v1/products", errorLog.RouteTemplate);
        Assert.Equal(typeof(InvalidOperationException).FullName, errorLog.TraceLogError?.ExceptionType);
    }

    private sealed class FailingProductService : IProductService
    {
        public Task<IReadOnlyList<ProductResponse>> ListAsync(int offset, int limit, string? search, CancellationToken ct) =>
            throw new InvalidOperationException("Test failure.");
    }
}
