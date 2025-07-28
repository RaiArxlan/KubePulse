using Microsoft.EntityFrameworkCore;
using ProcessorApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);
var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var connectionString = isDocker
    ? builder.Configuration.GetConnectionString("DockerConnectionString")
    : builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<RequestDbContext>(opt =>
    opt.UseNpgsql(connectionString));
builder.Services.AddControllers();
var app = builder.Build();
app.MapGet("/process", async (RequestDbContext db) =>
{
    var log = new RequestLog { Id = Guid.NewGuid(), StartTime = DateTime.UtcNow, SourceService = "caller-app" };
    db.RequestLogs.Add(log);
    await db.SaveChangesAsync();

    var delay = Random.Shared.Next(0, 5000);
    await Task.Delay(delay);

    log.EndTime = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(log.Id);
});
app.Run();
