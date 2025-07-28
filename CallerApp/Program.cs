using CallerApp;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<RequestWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RequestWorker>());

var app = builder.Build();

// Middleware
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
