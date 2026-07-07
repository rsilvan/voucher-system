using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VoucherSystem.Api.Endpoints;
using VoucherSystem.Application;
using VoucherSystem.Infrastructure;
using VoucherSystem.Workers;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<VoucherSystemDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT settings
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSection);
var jwtSettings = jwtSection.Get<JwtSettings>()!;

// Email settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });
builder.Services.AddAuthorization();

// Application & Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

// Background workers
builder.Services.AddHostedService<PromotionWorker>();

// Swagger/OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Voucher System API", Version = "v1" });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// In-memory cache for rate limiting
builder.Services.AddMemoryCache();

var app = builder.Build();

// Migrate and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VoucherSystemDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.SeedAsync(db);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Hardening middleware
app.UseMiddleware<VoucherSystem.Api.Middleware.GlobalExceptionMiddleware>();
app.UseMiddleware<VoucherSystem.Api.Middleware.RequestLoggingMiddleware>();

// User context middleware (loads permissions)
app.UseMiddleware<VoucherSystem.Api.Middleware.UserContextMiddleware>();

// Project context middleware (resolves current project, validates status)
app.UseMiddleware<VoucherSystem.Api.Middleware.ProjectContextMiddleware>();

// Login rate limit middleware (applies to POST /api/auth/login)
app.UseMiddleware<VoucherSystem.Api.Middleware.LoginRateLimitMiddleware>();

// Map endpoints
app.MapSelfServiceEndpoints();
app.MapAuthEndpoints();
app.MapMemberEndpoints();
app.MapInvitationEndpoints();
app.MapRoleEndpoints();
app.MapAuditEndpoints();
app.MapSystemEndpoints();
app.MapProjectAccessEndpoints();
app.MapProjectEndpoints();
app.MapPromotionEndpoints();
app.MapBrandEndpoints();
app.MapGeoLocationEndpoints();
app.MapStoreEndpoints();
app.MapAreaEndpoints();
app.MapMetricsEndpoints();

// Health check
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

app.Run();

public partial class Program { }
