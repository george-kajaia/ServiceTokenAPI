using System.Runtime;
using Microsoft.EntityFrameworkCore;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ServiceTokenDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("ServiceTokens")));

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(); // This will now resolve

builder.Services
    .AddOptions<GeneralOptions>()
    .Bind(builder.Configuration.GetSection("GeneralOptions"))
    .ValidateOnStart();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServiceTokenDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
