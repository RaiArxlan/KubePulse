using Microsoft.EntityFrameworkCore;
using ProcessorApi.Models;

var builder = WebApplication.CreateBuilder(args);
var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var connectionString = isDocker
	? builder.Configuration.GetConnectionString("DockerConnectionString")
	: builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<RequestDbContext>(opt =>
	opt.UseNpgsql(connectionString));
builder.Services.AddControllers();
var app = builder.Build();

app.UseCors();

// Automatically apply migrations at startup
using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<RequestDbContext>();
	dbContext.Database.Migrate();
}

//GET
app.Map("/", () =>
{
	return Results.Ok("Processor API Working.");
});

//GET /process
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

//GET /logs
app.MapGet("/logs", async (RequestDbContext db) =>
{
	var logs = await db.RequestLogs
						.OrderByDescending(x => x.StartTime)
						.Take(5)
						.ToListAsync();
	return Results.Ok(logs);
});

await app.RunAsync();