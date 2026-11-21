$ErrorActionPreference = 'Stop'
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousConnection = $env:ConnectionStrings__Cart
Push-Location (Join-Path $PSScriptRoot '..')
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    if (-not $env:ConnectionStrings__Cart) {
        $env:ConnectionStrings__Cart = 'Server=127.0.0.1,14330;Database=eCommerce;User Id=sa;Password=LocalOnly_Cart2026!;Encrypt=True;TrustServerCertificate=True'
    }
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Tool restore failed.' }
    dotnet ef dbcontext scaffold 'Name=ConnectionStrings:Cart' Microsoft.EntityFrameworkCore.SqlServer --project eCommerce.DL --startup-project eCommerce.Backend --context CartDbContext --context-dir . --output-dir ../eCommerce.Domain/Entities --namespace eCommerce.Domain.Entities --context-namespace eCommerce.DL --table Carts --table CartItems --table Products --table TraceLogs --table TraceLogErrors --no-onconfiguring --force
    if ($LASTEXITCODE -ne 0) { throw 'Scaffold failed.' }
} finally {
    Pop-Location
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:ConnectionStrings__Cart = $previousConnection
}

