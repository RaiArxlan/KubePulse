using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessorApi.Interface;
using ProcessorApi.Models;

namespace ProcessorApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ProcessController : ControllerBase
{
    private readonly RequestDbContext _db;

    private readonly IDbContextFactory<RequestDbContext> _dbContextFactory;
    
    private readonly IRabbitMqPublisher _rabbitMqPublisher;

    private readonly ILogger<ProcessController> _logger;

    public ProcessController(RequestDbContext db, IDbContextFactory<RequestDbContext> dbContextFactory, IRabbitMqPublisher rabbitMqPublisher, ILogger<ProcessController> logger)
    {
        _db = db;
        _dbContextFactory = dbContextFactory;
        _rabbitMqPublisher = rabbitMqPublisher;
        _logger = logger;
    }

    [HttpGet("/process")]
    public async Task<IActionResult> ProcessSimulationAsync()
    {
        var log = new RequestLog { Id = Guid.NewGuid(), StartTime = DateTime.UtcNow, SourceService = "caller-app" };
        _db.RequestLogs.Add(log);
        await _db.SaveChangesAsync();

        var delay = Random.Shared.Next(0, 5000);
        await Task.Delay(delay);

        log.EndTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation($"Processed request with ID: {log.Id}, Duration: {log.EndTime - log.StartTime}");

        return Ok(log.Id);
    }

    [HttpGet("/process2")]
    public async Task<IActionResult> ProcessSimulation2Async()
    {
        using var db = _dbContextFactory.CreateDbContext();
        var log = new RequestLog { Id = Guid.NewGuid(), StartTime = DateTime.UtcNow, SourceService = "caller-app" };
        db.RequestLogs.Add(log);
        await db.SaveChangesAsync();
        var delay = Random.Shared.Next(0, 5000);
        await Task.Delay(delay);
        log.EndTime = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation($"Processed request with ID: {log.Id}, Duration: {log.EndTime - log.StartTime}");

        return Ok(log.Id);
    }

    /// <summary>
    /// Accept incoming requests and put them in a message queue for processing.
    /// Messages will be processed by a background service that reads from the queue.
    /// </summary>
    /// <returns></returns>
    [HttpGet("/ProcessWithMessageQueue")]
    public async Task<IActionResult> ProcessSimulationWithMessageQueueAsync()
    {
        var log = new RequestLog { Id = Guid.NewGuid(), StartTime = DateTime.UtcNow, SourceService = "caller-app" };

        var json = System.Text.Json.JsonSerializer.Serialize(log);

        await _rabbitMqPublisher.Publish("ProcessQueue", json);

        _logger.LogInformation($"Published request with ID: {log.Id} to message queue.");

        return Accepted(log.Id);
    }
}