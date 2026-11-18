# eCommerce Backend

A shopping cart service built with .NET 10 LTS and ASP.NET Core 10. The API, BL, DL, ServiceInterfaces, and Domain are separate projects.
The service uses **SQL Server 2022 Developer in Docker** and follows a **database-first** workflow. SQL migrations define the schema, and EF models are reverse-engineered from the database.

[Architecture and delivery strategy](docs/arch.md)

## Run the full stack

```powershell
docker compose up -d --build
```

Open Swagger at **http://localhost:5005/swagger**, select **Authorize**, and enter **demo**.

| Request | Result |
|---|---|
| GET /api/v1/products | keyboard, mouse, monitor |
| GET /api/v1/carts/11111111-1111-1111-1111-111111111111 | Demo cart with one keyboard and two mice, totaling EUR 129.70 |

Use the PUT and DELETE endpoints to update the cart. Copy the ETag from the latest GET response into **If-Match**, including its quotation marks, as in `"1"`.
The PUT request body is `{"quantity":2}`. Use the new ETag after each update.
Prices in API responses are in cents. Carts expire after 30 days.

The products endpoint accepts queries such as `?offset=0&limit=10&search=keyboard`.
The default offset is 0. The default limit is 10, and allowed values range from 1 to 100.
The optional search term can be up to 200 characters and matches product IDs and names. A blank search returns unfiltered results.
SQL Server filters the results before paging and sorts them by product ID.

## Run the API locally from Visual Studio or the CLI

Prerequisite: .NET 10 SDK. 

Start SQL Server and run the migrations in Docker, then launch the API.

```powershell
docker compose up -d --build sqlserver database
docker compose wait database
dotnet run --project eCommerce.Backend --launch-profile http
```

If the Compose API is already using port 5005, stop it with `docker compose stop cart-api`.
The migration job creates the database, applies migrations, and seeds the data.

## Connect to SQL Server

Use these settings in SSMS, Visual Studio SQL Server Object Explorer, or another SQL client.

| Setting | Local value |
|---|---|
| Server | 127.0.0.1,14330 |
| Database | eCommerce |
| Authentication | SQL Server Authentication |
| User | sa |
| Password | LocalOnly_Cart2026! |
| Encrypt | True |
| Trust server certificate | True for local development only |

These credentials are public examples for local development only. All production secrets must go to `.env`.

## Database-first migrations and seed data

The `eCommerce.Database` project runs as a separate Docker job and contains these scripts.

- `Migrations/V001__InitialSchema.sql` creates tables, primary and foreign keys, default values, and CHECK constraints.
- `Migrations/V002__CartExpiryIndex.sql` adds an index for cart expiration.
- `Migrations/V003__CaseSensitiveOwnerIdentity.sql` makes user identity comparisons case sensitive.
- `Seeds/DemoData.sql` adds three products and a demo cart when `Database__SeedDemo=true`.

After adding a migration, run the following commands to apply it, regenerate the EF model, and verify the API.

```powershell
docker compose build database
docker compose run --rm database
./scripts/Scaffold-Database.ps1
dotnet test eCommerce.Backend.sln -c Release
docker compose up -d --build cart-api
```

EF handles mapping and queries. The SQL scripts own schema changes without `EnsureCreated`, `Database.Migrate`, or EF code-first migrations. 

## Project structure

| Project | Responsibility |
|---|---|
| eCommerce.Backend | HTTP controllers, authentication, Swagger, error handling, and dependency injection |
| eCommerce.ServiceInterfaces | Service contracts, DTOs, and business errors |
| eCommerce.BL | Cart rules, quantities, ownership, pricing, expiration, and versioning |
| eCommerce.DL | Generated DbContext and SQL Server repository implementations |
| eCommerce.Domain | Generated entities and repository contracts |
| eCommerce.Database | SQL migrations, migration history, and seed scripts |

BL depends on Domain and ServiceInterfaces. DL depends on Domain. Controllers access business logic through service interfaces. Each entity and the DbContext have their own source files.

## Tests, CI, and health checks

```powershell
docker compose up -d sqlserver
dotnet test eCommerce.Backend.sln -c Release
```

The integration tests create separate `CartTests_...` databases on SQL Server, apply the same migrations and seed data, verify the API, and remove only the databases they created.

### GitHub Actions

The Cart API CI workflow runs on pull requests and pushes to `main`. Open the repository's **Actions** tab and select **Cart API CI** to see the jobs and logs for a run. The workflow is defined in [`.github/workflows/ci.yml`](.github/workflows/ci.yml).

### Health checks and local logs

- Use `/health/live` to check liveness and `/health/ready` to check readiness.
- Run `docker compose logs cart-api database` to inspect API and migration logs.
- Run `docker compose down` to stop the stack and keep the data volume.

## Security and production scope

The `X-Demo-User` header is available only in Development.
For JWT authentication, configure `Authentication__Authority` with an HTTPS URL, set `Authentication__Audience`, and set `Authentication__DemoEnabled=false`.
Use `Database__SeedDemo=false` for the production migration job.

## Versions

All projects and tests target `net10.0`. EF Core and the local `dotnet-ef` tool use version 10.0.15, and Swagger uses Swashbuckle 10.
Docker uses the `10.0` SDK image for builds, `aspnet:10.0` for the API, and `runtime:10.0` for the migration job.
GitHub Actions installs the SDK specified in `global.json`.

The [architecture document](./docs/arch.md) covers the high-level target architecture and the flow.
