using eCommerce.DL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace eCommerce.Backend;
public sealed class DatabaseHealthCheck(IServiceScopeFactory scopes) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();
            await db.Carts.AnyAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch { return HealthCheckResult.Unhealthy("Database unavailable."); }
    }
}

