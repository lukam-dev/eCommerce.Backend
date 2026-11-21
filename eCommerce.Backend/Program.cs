using System.Security.Claims;
using System.Threading.RateLimiting;
using eCommerce.Backend;
using eCommerce.Backend.Tracing;
using eCommerce.BL.Services;
using eCommerce.DL;
using eCommerce.DL.Repositories;

using eCommerce.Domain.Persistence;
using eCommerce.ServiceInterfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
var demo = builder.Configuration.GetValue<bool>("Authentication:DemoEnabled");
builder.Services.AddSwaggerGen(o =>
{
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = demo ? SecuritySchemeType.ApiKey : SecuritySchemeType.Http,
        Name = demo ? "X-Demo-User" : "Authorization", In = ParameterLocation.Header,
        Scheme = "bearer", BearerFormat = "JWT",
        Description = demo ? "Development only: enter demo to access the seeded cart." : "OIDC access token."
    });
    o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});
builder.Services.AddDbContext<CartDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Cart") ?? throw new InvalidOperationException("Configure ConnectionStrings:Cart.")));

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<TraceLogRepository>();
builder.Services.AddSingleton<TraceLogQueue>();
builder.Services.AddHostedService<TraceLogWorker>();

if (demo && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Demo authentication is allowed only in Development.");
if (demo)
    builder.Services.AddAuthentication("Demo").AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DemoAuthenticationHandler>("Demo", _ => { });
else
{
    var authority = builder.Configuration["Authentication:Authority"];
    var audience = builder.Configuration["Authentication:Audience"];
    if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri) || uri.Scheme != "https" || string.IsNullOrWhiteSpace(audience))
        throw new InvalidOperationException("Configure an HTTPS Authentication:Authority and Authentication:Audience.");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
    {
        o.Authority = authority;
        o.Audience = audience;
        o.RequireHttpsMetadata = true;
        o.MapInboundClaims = false;
        o.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
    });
}
builder.Services.AddAuthorization(o => o.AddPolicy("CartOwner", p => p.RequireAuthenticatedUser().RequireClaim("sub")));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("cart", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue("sub") ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
    o.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await Results.Problem(statusCode: 429, title: "Too many requests. Retry later.").ExecuteAsync(context.HttpContext);
    };
});
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });
builder.Logging.AddJsonConsole();

var app = builder.Build();
app.UseMiddleware<TraceLogMiddleware>();
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    // Preserve exception diagnostics when handlers return true on .NET 10.
    SuppressDiagnosticsCallback = _ => false
});
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers.CacheControl = "no-store";
    await next();
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// TLS terminates at the trusted ingress in production; do not trust arbitrary forwarded headers.
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

if (demo) app.Logger.LogWarning("DEMO AUTHENTICATION ENABLED: local development only.");
app.Run();

public partial class Program { }
