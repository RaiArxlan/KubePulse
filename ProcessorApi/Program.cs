using Microsoft.EntityFrameworkCore;
using ProcessorApi.Interface;
using ProcessorApi.Models;
using ProcessorApi.Services;

var builder = WebApplication.CreateBuilder(args);
var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var connectionString = isDocker
    ? builder.Configuration.GetConnectionString("DockerConnectionString")
    : builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddHostedService<ProcessConsumerService>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddDbContextFactory<RequestDbContext>(opt => { opt.UseNpgsql(connectionString); });
builder.Services.AddControllers();

if (isDocker)
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(8080);
    });
}

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An error occurred.");
    });
});

app.UseRouting();
app.MapControllers();

// Automatically apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RequestDbContext>();
    dbContext.Database.Migrate();
}


await app.RunAsync();