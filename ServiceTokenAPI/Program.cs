using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Enums;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ServiceTokenDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("ServiceTokens")));

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ── Forward headers (needed when behind Nginx) ────────────────────────────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                             | ForwardedHeaders.XForwardedProto
                             | ForwardedHeaders.XForwardedPrefix;
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();
});

// ── Controllers & Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services
    .AddOptions<GeneralOptions>()
    .Bind(builder.Configuration.GetSection("GeneralOptions"))
    .ValidateOnStart();

var app = builder.Build();

// ── Migrations ────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServiceTokenDbContext>();
    await db.Database.MigrateAsync();
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseForwardedHeaders();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Service Token API v1");
});

// Only redirect to HTTPS locally — nginx handles it in production
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowConfiguredOrigins");

app.UseAuthorization();

app.MapControllers();

app.Run();
