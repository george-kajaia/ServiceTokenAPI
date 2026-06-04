using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Enums;
using ServiceTokenApi.Hubs;
using ServiceTokenApi.Options;
using ServiceTokenApi.Services.Flitt;

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
            .AllowCredentials();   // required for SignalR WebSocket handshake
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

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

builder.Services
    .AddOptions<GeneralOptions>()
    .Bind(builder.Configuration.GetSection("GeneralOptions"))
    .ValidateOnStart();

// ── Flitt embedded-checkout payments ──────────────────────────────────────────
builder.Services.Configure<FlittOptions>(builder.Configuration.GetSection(FlittOptions.SectionName));
builder.Services.AddHttpClient<IFlittPaymentService, FlittPaymentService>((sp, client) =>
{
    var flitt = sp.GetRequiredService<IOptions<FlittOptions>>().Value;
    client.BaseAddress = new Uri(flitt.BaseUrl);        // e.g. https://pay.flitt.com/api/
});

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

// ── SignalR hub ───────────────────────────────────────────────────────────────
// Must be mapped AFTER UseCors so the CORS policy applies to the WebSocket upgrade.
app.MapHub<RedemptionHub>("/hubs/redemption");

app.Run();
