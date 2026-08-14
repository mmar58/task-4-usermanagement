using System.Text;
using backend.Data;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using backend.Middleware;

// Load .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure EF Core with PostgreSQL
var dbHost = Env.GetString("DB_HOST") ?? "localhost";
var dbPort = Env.GetString("DB_PORT") ?? "5432";
var dbName = Env.GetString("DB_NAME") ?? "usermanagement";
var dbUser = Env.GetString("DB_USER") ?? "postgres";
var dbPassword = Env.GetString("DB_PASSWORD") ?? "postgres";

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

var dbCa = Env.GetString("DB_CA");
if (!string.IsNullOrWhiteSpace(dbCa))
{
    var caPath = Path.Combine(Path.GetTempPath(), "db_ca.crt");
    File.WriteAllText(caPath, dbCa.Replace("\\n", "\n"));
    connectionString += $";Ssl Mode=Require;RootCertificate={caPath}";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure JWT Authentication
var jwtSecret = Env.GetString("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured");
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// Configure CORS for SvelteKit frontend
var appOriginsStr = Env.GetString("APP_ORIGINS");
var appOrigins = string.IsNullOrWhiteSpace(appOriginsStr) 
    ? new[] { "http://localhost:5174", "http://127.0.0.1:5174" } 
    : appOriginsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(appOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
app.UseUserStatusCheck();

app.MapControllers();

app.Run();
