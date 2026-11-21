using eCommerce.Domain.Entities;

namespace eCommerce.DL.Repositories;

public sealed class TraceLogRepository(CartDbContext db)
{
    public async Task AppendAsync(IReadOnlyList<TraceLog> entries, CancellationToken ct)
    {
        db.TraceLogs.AddRange(entries);
        await db.SaveChangesAsync(ct);
    }
}
