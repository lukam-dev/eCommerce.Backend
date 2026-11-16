using eCommerce.Domain.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace eCommerce.DL.Repositories;
internal static class StorageOperation
{
    public static async Task<T> RunAsync<T>(Func<Task<T>> operation)
    {
        try { return await operation(); }
        catch (DbUpdateConcurrencyException ex) { throw new ConcurrencyException(ex); }
        catch (Exception ex) when ((ex as SqlException ?? ex.InnerException as SqlException)?.Number is
            -2 or 1205 or 1222 or 4060 or 40197 or 40501 or 40613 or 10928 or 10929)
        { throw new StorageUnavailableException(ex); }
    }
}

