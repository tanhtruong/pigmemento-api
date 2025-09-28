using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string BuildPgConnectionString()
{
    var raw =
        builder.Configuration.GetConnectionString("Postgres")
        ?? Environment.GetEnvironmentVariable("POSTGRES_CONNSTR")
        ?? Environment.GetEnvironmentVariable("DATABASE_URL"); // e.g. postgres://...

    if (string.IsNullOrWhiteSpace(raw))
        throw new InvalidOperationException(
            "No Postgres connection string configured. " +
            "Set ConnectionStrings:Postgres or POSTGRES_CONNSTR or DATABASE_URL.");

    // Convert URL-style → key/value
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

        // Many managed hosts (Render, Railway, Supabase) require SSL
        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }

    // Already key/value format
    return raw;
}

var conn = BuildPgConnectionString();

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(conn));

// Broad CORS for dev + mobile
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true));
});

var app = builder.Build();

// Auto-migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();