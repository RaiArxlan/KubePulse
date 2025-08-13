using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessorApi.Models;

namespace ProcessorApi.Controllers;

[ApiController]
[Route("[controller]")]
public class LogsController : ControllerBase
{
    private readonly RequestDbContext _db;

    public LogsController(RequestDbContext db)
    {
        _db = db;
    }

    [HttpGet("/Logs")]
    public async Task<IActionResult> Index()
    {
        var logs = await _db.RequestLogs
                        .OrderByDescending(x => x.StartTime)
                        .Take(5)
                        .ToListAsync();
        return Ok(logs);
    }
}