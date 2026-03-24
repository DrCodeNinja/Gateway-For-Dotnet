using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        policy.WithOrigins(allowedOrigins ?? ["http://localhost:3000"])
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
        };
    });

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    options.AddFixedWindowLimiter("strict", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });
});

// Health Checks
var catalogUrl = builder.Configuration["ReverseProxy:Clusters:catalog-cluster:Destinations:destination1:Address"]!;
var ordersUrl = builder.Configuration["ReverseProxy:Clusters:orders-cluster:Destinations:destination1:Address"]!;

builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri($"{catalogUrl}health"), name: "catalog-service",
        tags: ["service"])
    .AddUrlGroup(new Uri($"{ordersUrl}health"), name: "orders-service",
        tags: ["service"]);

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Log incoming request details
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var request = context.Request;
    logger.LogInformation("Request: {Method} {Scheme}://{Host}{Path}{QueryString} from {RemoteIp}",
        request.Method,
        request.Scheme,
        request.Host,
        request.Path,
        request.QueryString,
        context.Connection.RemoteIpAddress);
    await next();
});

app.UseCors("AllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // No checks, just confirms the gateway is running
});

app.MapReverseProxy();

app.Run();
