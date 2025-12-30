using ServiceTokenAPI.DBContext;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ServiceTokenDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("ServiceTokens")));

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServiceTokenDbContext>();
    await db.Database.MigrateAsync();
}

//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
