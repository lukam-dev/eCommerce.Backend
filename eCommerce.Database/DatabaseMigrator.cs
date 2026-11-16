using System.Data;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace eCommerce.Database;

// SQL scripts are the schema source of truth. EF never creates or updates the database.
public static class DatabaseMigrator
{
    public static async Task ApplyAsync(string connectionString, bool seedDemo, CancellationToken ct = default)
    {
        var settings = new SqlConnectionStringBuilder(connectionString) { Pooling = false };
        var database = settings.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database)) throw new InvalidOperationException("Specify a database.");
        var masterSettings = new SqlConnectionStringBuilder(settings.ConnectionString) { InitialCatalog = "master" };
        await using (var master = new SqlConnection(masterSettings.ConnectionString))
        {
            await master.OpenAsync(ct);
            await using var create = new SqlCommand(
                "IF DB_ID(@name) IS NULL BEGIN DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@name); EXEC(@sql); END", master);
            create.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = database;
            await create.ExecuteNonQueryAsync(ct);
        }
        await using var connection = new SqlConnection(settings.ConnectionString);
        await connection.OpenAsync(ct);
        // Serialize deployments against the same database, including the journal bootstrap.
        await ExecuteAsync(connection, null, """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = N'eCommerce.SchemaMigration',
                @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 60000;
            IF @result < 0 THROW 51000, 'Cannot acquire migration lock.', 1;
            IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
                CREATE TABLE dbo.SchemaVersions (
                    ScriptName nvarchar(255) NOT NULL CONSTRAINT PK_SchemaVersions PRIMARY KEY,
                    Checksum char(64) NOT NULL,
                    AppliedAt datetimeoffset NOT NULL CONSTRAINT DF_SchemaVersions_AppliedAt DEFAULT SYSDATETIMEOFFSET()
                );
            """, ct);
        var assembly = typeof(DatabaseMigrator).Assembly;
        foreach (var script in assembly.GetManifestResourceNames().Where(n => n.Contains(".Migrations.")).OrderBy(n => n, StringComparer.Ordinal))
        {
            var sql = await ReadAsync(assembly, script, ct);
            var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
            await using var check = new SqlCommand("SELECT Checksum FROM dbo.SchemaVersions WHERE ScriptName = @script", connection);
            check.Parameters.Add("@script", SqlDbType.NVarChar, 255).Value = script;
            var previous = await check.ExecuteScalarAsync(ct);
            if (previous is string existing)
            {
                if (existing != checksum) throw new InvalidOperationException($"Applied migration was modified: {script}. Add a new migration instead.");
                continue;
            }
            using var transaction = connection.BeginTransaction();
            await ExecuteAsync(connection, transaction, sql, ct);
            await using var journal = new SqlCommand("INSERT dbo.SchemaVersions(ScriptName, Checksum) VALUES (@script, @checksum)", connection, transaction);
            journal.Parameters.Add("@script", SqlDbType.NVarChar, 255).Value = script;
            journal.Parameters.Add("@checksum", SqlDbType.Char, 64).Value = checksum;
            await journal.ExecuteNonQueryAsync(ct);
            await transaction.CommitAsync(ct);
            Console.WriteLine($"Applied {script}");
        }
        if (seedDemo)
        {
            using var transaction = connection.BeginTransaction();
            foreach (var script in assembly.GetManifestResourceNames().Where(n => n.Contains(".Seeds.")).OrderBy(n => n, StringComparer.Ordinal))
                await ExecuteAsync(connection, transaction, await ReadAsync(assembly, script, ct), ct);
            await transaction.CommitAsync(ct);
        }
        // Closing the connection releases the session-owned migration lock.
    }

    private static async Task ExecuteAsync(SqlConnection connection, SqlTransaction? transaction, string sql, CancellationToken ct)
    {
        await using var command = new SqlCommand(sql, connection, transaction) { CommandTimeout = 90 };
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<string> ReadAsync(Assembly assembly, string name, CancellationToken ct)
    {
        using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
        return await reader.ReadToEndAsync(ct);
    }
}


