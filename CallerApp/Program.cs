using CallerApp;

var builder = WebApplication.CreateBuilder(args);

var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

// Services
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<RequestWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RequestWorker>());
if (isDocker)
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(8080);
    });
}

var app = builder.Build();

// Middleware
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

await app.RunAsync();
