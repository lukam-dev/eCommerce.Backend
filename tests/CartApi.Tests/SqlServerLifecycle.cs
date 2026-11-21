using eCommerce.Database;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CartApi.Tests;
public sealed partial class CartTests
{
    private readonly string databaseName = $"CartTests_{Guid.NewGuid():N}";
    private string ConnectionString
    {
        get
        {
            var settings = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CART_TEST_CONNECTION")
                ?? "Server=127.0.0.1,14330;User Id=sa;Password=LocalOnly_Cart2026!;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10")
                { InitialCatalog = databaseName, Pooling = false };
            return settings.ConnectionString;
        }
    }

    public Task InitializeAsync() => DatabaseMigrator.ApplyAsync(ConnectionString, seedDemo: true);

    public async Task DisposeAsync()
    {
        factory.Dispose();
        // Only the unique database allocated by this test is removed; the demo database is untouched.
        if (!databaseName.StartsWith("CartTests_", StringComparison.Ordinal)) throw new InvalidOperationException();
        var master = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(master.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            $"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END", connection);
        command.Parameters.AddWithValue("@name", databaseName);
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Migration_history_is_versioned_and_repeatable()
    {
        await DatabaseMigrator.ApplyAsync(ConnectionString, seedDemo: true);
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var count = new SqlCommand("SELECT COUNT(*) FROM dbo.SchemaVersions", connection);
        Assert.Equal(4, (int)(await count.ExecuteScalarAsync())!);
        await using var constraint = new SqlCommand("INSERT dbo.CartItems(CartId,ProductId,Quantity,UnitPriceMinor) VALUES('11111111-1111-1111-1111-111111111111','monitor',0,24900)", connection);
        var error = await Assert.ThrowsAsync<SqlException>(() => constraint.ExecuteNonQueryAsync());
        Assert.Equal(547, error.Number);
    }
}


