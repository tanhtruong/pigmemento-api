using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pigmemento.Api.Auth;
using Pigmemento.Api.Data;
using Pigmemento.Api.Data.Seed;
using Pigmemento.Api.Models;
using Pigmemento.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------
// Load .env variables early
// --------------------------------------
Env.Load(); // loads .env from project root

// --------------------------------------
// Services setup
// --------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --------------------------------------
// Database (Npgsql + .env variables)
// --------------------------------------
var connectionString =
    Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
    ?? throw new InvalidOperationException("POSTGRES_CONNECTION not set");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// --------------------------------------
// JWT configuration (from .env)
// --------------------------------------
var jwtSettings = new JwtSettings
{
    Issuer = Environment.GetEnvironmentVariable("JWT_Issuer") ?? "https://api.pigmemento.app",
    Audience = Environment.GetEnvironmentVariable("JWT_Audience") ?? "pigmemento-clients",
    Key = Environment.GetEnvironmentVariable("JWT_Key") ?? throw new InvalidOperationException("JWT_KEY missing"),
    ExpirationMinutes = int.TryParse(Environment.GetEnvironmentVariable("JWT_ExpirationMinutes"), out var exp)
        ? exp : 480
};

builder.Services.Configure<JwtSettings>(options =>
{
    options.Issuer = jwtSettings.Issuer;
    options.Audience = jwtSettings.Audience;
    options.Key = jwtSettings.Key;
    options.ExpirationMinutes = jwtSettings.ExpirationMinutes;
});

var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

// --------------------------------------
// Auth + JWT setup
// --------------------------------------
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

// --------------------------------------
// Application Services
// --------------------------------------
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<CaseSeeder>();

// --------------------------------------
// CORS (Expo + Web + Prod domain)
// --------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("PigmementoCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:19006",   // Expo
                "http://localhost:3000",    // web dev
                "https://pigmemento.app",
                "https://www.pigmemento.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// --------------------------------------
// Pipeline
// --------------------------------------
app.UseCors("PigmementoCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();