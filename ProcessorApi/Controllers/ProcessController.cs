using Microsoft.AspNetCore.Mvc;
using ProcessorApi.Models;

namespace ProcessorApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ProcessController : ControllerBase
{
    private readonly RequestDbContext _db;

    public ProcessController(RequestDbContext db)
    {
        _db = db;
    }

    [HttpGet("/process")]
    public async Task<IActionResult> GetProcessStatusAsync()
    {
        var log = new RequestLog { Id = Guid.NewGuid(), StartTime = DateTime.UtcNow, SourceService = "caller-app" };
        _db.RequestLogs.Add(log);
        await _db.SaveChangesAsync();

        var delay = Random.Shared.Next(0, 5000);
        await Task.Delay(delay);

        log.EndTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(log.Id);
    }
}