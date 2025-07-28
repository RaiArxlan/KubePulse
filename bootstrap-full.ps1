# bootstrap-full.ps1
param (
    [string]$solutionName = "KubePulse.sln"
)

$ErrorActionPreference = "Stop"
Write-Host "Starting full solution generation..."

# 1. Create projects
dotnet new razor -o CallerApp
dotnet new webapi -o ProcessorApi

dotnet new sln -n KubePulse -o . -f net8.0 --force
dotnet sln $solutionName add CallerApp/CallerApp.csproj
dotnet sln $solutionName add ProcessorApi/ProcessorApi.csproj

# 2. Add required NuGet packages
dotnet add ProcessorApi/ProcessorApi.csproj package Npgsql
dotnet add ProcessorApi/ProcessorApi.csproj package Microsoft.EntityFrameworkCore
dotnet add ProcessorApi/ProcessorApi.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add CallerApp/CallerApp.csproj package Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
dotnet add CallerApp/CallerApp.csproj package System.Net.Http.Json

# 3. Create shared models and DbContext in ProcessorApi
$modelsDir = "ProcessorApi/Models"
New-Item -ItemType Directory -Force -Path $modelsDir

@"
using System;
namespace ProcessorApi.Models
{
    public class RequestLog
    {
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string SourceService { get; set; }
    }
}
"@ | Out-File -Encoding UTF8 "$modelsDir/RequestLog.cs"

@"
using Microsoft.EntityFrameworkCore;
namespace ProcessorApi.Models
{
    public class RequestDbContext : DbContext
    {
        public RequestDbContext(DbContextOptions<RequestDbContext> options) : base(options) {}
        public DbSet<RequestLog> RequestLogs { get; set; }
    }
}
"@ | Out-File -Encoding UTF8 "$modelsDir/RequestDbContext.cs"

# 4. Minimal API startup code in ProcessorApi
Set-Content -Path "ProcessorApi/Program.cs" -Value @"
using Microsoft.EntityFrameworkCore;
using ProcessorApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<RequestDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString(\"DefaultConnection\")));
builder.Services.AddControllers();
var app = builder.Build();
app.MapGet(\"/process\", async (RequestDbContext db) =>
{
    var log = new RequestLog { Id = Guid.NewGuid(), StartTime = DateTime.UtcNow, SourceService = \"caller-app\" };
    db.RequestLogs.Add(log);
    await db.SaveChangesAsync();

    var delay = Random.Shared.Next(0, 5000);
    await Task.Delay(delay);

    log.EndTime = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(log.Id);
});
app.Run();
"@

# 5. Razor Pages UI and background worker in CallerApp
@"
@page
@model ControlModel
<form method=\"post\">
  <label>Interval (seconds):</label>
  <input type=\"number\" asp-for=\"Interval\" min=\"1\" value=\"5\"/>
  <button type=\"submit\">Update</button>
</form>
"@ | Out-File -Encoding UTF8 "CallerApp/Pages/Control.cshtml"

@"
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace CallerApp.Pages
{
    public class ControlModel : PageModel
    {
        private readonly RequestWorker _worker;
        [BindProperty]
        public int Interval { get; set; } = 5;
        public ControlModel(RequestWorker worker) => _worker = worker;
        public void OnPost()
        {
            _worker.SetInterval(Interval);
        }
    }
}
"@ | Out-File -Encoding UTF8 "CallerApp/Pages/Control.cshtml.cs"

@"
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
namespace CallerApp
{
    public class RequestWorker : BackgroundService
    {
        private readonly IHttpClientFactory _factory;
        private int _interval = 5;
        public RequestWorker(IHttpClientFactory factory) => _factory = factory;
        public void SetInterval(int sec) => _interval = sec;
        protected override async Task ExecuteAsync(CancellationToken token)
        {
            var client = _factory.CreateClient();
            while (!token.IsCancellationRequested)
            {
                try { await client.GetAsync(\"http://processor-api/process\"); } catch {}
                await Task.Delay(_interval * 1000, token);
            }
        }
    }
}
"@ | Out-File -Encoding UTF8 "CallerApp/RequestWorker.cs"

@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace CallerApp
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(web =>
                {
                    web.UseStartup<Startup>();
                });
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddHttpClient();
            services.AddSingleton<RequestWorker>();
            services.AddHostedService(p => p.GetRequiredService<RequestWorker>());
        }
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
"@ | Out-File -Encoding UTF8 "CallerApp/Program.cs"

# 6. Dockerfiles
@"
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT [\"dotnet\", \"CallerApp.dll\"]
"@ | Out-File -Encoding UTF8 "CallerApp/Dockerfile"

@"
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT [\"dotnet\", \"ProcessorApi.dll\"]
"@ | Out-File -Encoding UTF8 "ProcessorApi/Dockerfile"

# 7. docker-compose.yml
@"
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: requests
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: secret
    volumes:
      - pgdata:/var/lib/postgresql/data
    ports:
      - 5432:5432

  processor-api:
    build: ./ProcessorApi
    environment:
      - ASPNETCORE_URLS=http://+:80
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=requests;Username=admin;Password=secret
    depends_on:
      - postgres

  caller-app:
    build: ./CallerApp
    environment:
      - ASPNETCORE_URLS=http://+:80
    depends_on:
      - processor-api

volumes:
  pgdata:
"@ | Out-File -Encoding UTF8 "docker-compose.yml"

# 8. Kubernetes manifests in k8s/
New-Item -ItemType Directory -Force -Path "k8s"

# postgres.yaml
@"
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: pg-pvc
spec:
  accessModes: [ReadWriteOnce]
  resources:
    requests:
      storage: 1Gi
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: postgres
spec:
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels: { app: postgres }
    spec:
      containers:
      - name: postgres
        image: postgres:16
        env:
        - name: POSTGRES_DB
          value: requests
        - name: POSTGRES_USER
          value: admin
        - name: POSTGRES_PASSWORD
          value: secret
        volumeMounts:
        - name: data
          mountPath: /var/lib/postgresql/data
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: pg-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: postgres
spec:
  type: ClusterIP
  ports:
    - port: 5432
"@ | Out-File -Encoding UTF8 "k8s/postgres.yaml"

# processor-api.yaml
@"
apiVersion: apps/v1
kind: Deployment
metadata:
  name: processor-api
spec:
  replicas: 1
  selector:
    matchLabels: { app: processor-api }
  template:
    metadata:
      labels: { app: processor-api }
    spec:
      containers:
      - name: processor-api
        image: kubepulse/processor-api:latest
        ports: [{ containerPort: 80 }]
        env:
        - name: ASPNETCORE_URLS
          value: http://+:80
        - name: ConnectionStrings__DefaultConnection
          value: Host=postgres;Database=requests;Username=admin;Password=secret
        resources:
          requests: { cpu: \"100m\" }
          limits: { cpu: \"300m\" }
---
apiVersion: v1
kind: Service
metadata:
  name: processor-api
spec:
  selector:
    app: processor-api
  ports:
    - port: 80
"@ | Out-File -Encoding UTF8 "k8s/processor-api.yaml"

# caller-app.yaml
@"
apiVersion: apps/v1
kind: Deployment
metadata:
  name: caller-app
spec:
  replicas: 1
  selector:
    matchLabels: { app: caller-app }
  template:
    metadata:
      labels: { app: caller-app }
    spec:
      containers:
      - name: caller-app
        image: kubepulse/caller-app:latest
        ports: [{ containerPort: 80 }]
        env:
        - name: ASPNETCORE_URLS
          value: http://+:80
        resources:
          requests: { cpu: \"100m\" }
"@ | Out-File -Encoding UTF8 "k8s/caller-app.yaml"

# hpa.yaml
@"
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: processor-api-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: processor-api
  minReplicas: 0
  maxReplicas: 5
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 50
"@ | Out-File -Encoding UTF8 "k8s/hpa.yaml"

Write-Host "🎉 Full KubePulse solution generated."
