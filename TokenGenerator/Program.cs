using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var jwtSettings = app.Configuration.GetSection("Jwt");
var signingKey = jwtSettings["Key"]!;
var issuer = jwtSettings["Issuer"]!;
var audience = jwtSettings["Audience"]!;
var defaultExpiresInHours = double.Parse(jwtSettings["DefaultExpiresInHours"] ?? "1");
var defaultUsername = jwtSettings["DefaultUsername"] ?? "testuser";

// POST /api/token - generate a JWT token
app.MapPost("/api/token", (TokenRequest request) =>
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, request.Username),
        new(ClaimTypes.Email, request.Email ?? "")
    };

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(request.ExpiresInHours ?? defaultExpiresInHours),
        signingCredentials: creds);

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new
    {
        token = tokenString,
        expiresAt = token.ValidTo,
        username = request.Username
    });
});

// GET /api/token/quick - generate a token with defaults for quick testing
app.MapGet("/api/token/quick", () =>
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: [new Claim(ClaimTypes.Name, defaultUsername)],
        expires: DateTime.UtcNow.AddHours(defaultExpiresInHours),
        signingCredentials: creds);

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new
    {
        token = tokenString,
        expiresAt = token.ValidTo,
        username = defaultUsername
    });
});

app.Run();

record TokenRequest(string Username, string? Email, double? ExpiresInHours);
