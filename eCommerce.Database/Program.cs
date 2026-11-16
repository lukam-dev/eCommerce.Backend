using eCommerce.Database;
var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Cart")
    ?? throw new InvalidOperationException("Set ConnectionStrings__Cart.");
var seed = string.Equals(Environment.GetEnvironmentVariable("Database__SeedDemo"), "true", StringComparison.OrdinalIgnoreCase);
await DatabaseMigrator.ApplyAsync(connection, seed);
Console.WriteLine("SQL migrations completed" + (seed ? " and demo data seeded." : "."));

