using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using DotNetEnv;

using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Pigmemento.Api.Auth.Core;
using Pigmemento.Api.Auth.Jwt;
using Microsoft.AspNetCore.Identity;
using Pigmemento.Api.Models;
using System.Security.Claims;

using Amazon.S3;
using Pigmemento.Api.Core.Services;
using Pigmemento.Api.Core.Interfaces;

Env.Load(); // keep if you use a .env file

var builder = WebApplication.CreateBuilder(args);

// Make sure env vars are available
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ---------- JWT from ENV (single source of truth) ----------
var jwtKey = Environment.GetEnvironmentVariable("JWT__Key");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT__Issuer") ?? "";
var jwtAudience = Environment.GetEnvironmentVariable("JWT__Audience") ?? "";
var jwtExpireMinutes = int.TryParse(Environment.GetEnvironmentVariable("JWT__ExpirationMinutes"), out var m) ? m : 480;

// Validate early
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("JWT__Key must be set and at least 32 characters.");
// -----------------------------------------------------------

// Build one options object reused everywhere
var jwtOpts = new JwtOptions
{
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    SigningKey = jwtKey,
    AccessTokenMinutes = jwtExpireMinutes
};

// Auth services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<ISpacedRepetitionService, SpacedRepetitionService>();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
// If your JwtTokenService expects JwtOptions directly:
builder.Services.AddSingleton<IJwtTokenService>(_ => new JwtTokenService(jwtOpts));
// If it expects IOptions<JwtOptions>, use:
// builder.Services.AddSingleton<IJwtTokenService>(_ => new JwtTokenService(Options.Create(jwtOpts)));

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var accountId = cfg["STORAGE_ACCOUNT_ID"];
    var serviceUrl = $"https://{accountId}.r2.cloudflarestorage.com";

    var s3cfg = new AmazonS3Config
    {
        ServiceURL = serviceUrl,
        ForcePathStyle = true,
        AuthenticationRegion = "auto",
        // UseChunkEncoding = false,      // <-- key line for R2
        // Optional: helps some S3-compatible stores
        // DisablePayloadSigning = true
    };

    return new AmazonS3Client(
        cfg["STORAGE_ACCESS_KEY_ID"],
        cfg["STORAGE_SECRET_ACCESS_KEY"],
        s3cfg
    );
});

builder.Services.AddSingleton<StorageService>();

// --- Authentication / Authorization ---
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SigningKey)),
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtOpts.Issuer),
            ValidIssuer = string.IsNullOrWhiteSpace(jwtOpts.Issuer) ? null : jwtOpts.Issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtOpts.Audience),
            ValidAudience = string.IsNullOrWhiteSpace(jwtOpts.Audience) ? null : jwtOpts.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

// --- Swagger with Bearer support ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Pigmemento API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, Array.Empty<string>() } });
});

// --- DB ---
string BuildPgConnectionString()
{
    var raw =
        builder.Configuration.GetConnectionString("Postgres")
        ?? Environment.GetEnvironmentVariable("POSTGRES_CONNSTR")
        ?? Environment.GetEnvironmentVariable("DATABASE_URL");

    if (string.IsNullOrWhiteSpace(raw))
        throw new InvalidOperationException("No Postgres connection string configured.");

    if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var host = uri.Host;
        var port = uri.IsDefaultPort ? 5432 : uri.Port;
        var database = uri.AbsolutePath.TrimStart('/');
        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }

    return raw;
}

var conn = BuildPgConnectionString();
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(conn));

// CORS
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Apply all migrations (creates __EFMigrationsHistory, citext extension, etc.)
    db.Database.Migrate();

    // Seed AFTER the schema is correct
    AppSeeder.Seed(db);
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// Minimal test token (uses same env-driven jwtOpts)
app.MapPost("/auth/token", (string userId) =>
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SigningKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new System.Security.Claims.Claim("sub", userId),
        new System.Security.Claims.Claim("uid", userId)
    };

    var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: string.IsNullOrWhiteSpace(jwtOpts.Issuer) ? null : jwtOpts.Issuer,
        audience: string.IsNullOrWhiteSpace(jwtOpts.Audience) ? null : jwtOpts.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(jwtOpts.AccessTokenMinutes),
        signingCredentials: creds);

    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(jwt);
    return Results.Ok(new { access_token = token, token_type = "Bearer", expires_in = jwtOpts.AccessTokenMinutes * 60 });
});

app.Run();