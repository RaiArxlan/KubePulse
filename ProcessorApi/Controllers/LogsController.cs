using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessorApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;

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

    [HttpGet("/Logs/{minutes}")]
    public async Task<IActionResult> Index(int minutes = 15)
    {
        var logs = await _db.RequestLogs
                        .OrderByDescending(x => x.StartTime)
                        .Where(x => x.StartTime >= DateTime.UtcNow.AddMinutes(-minutes))
                        .Take(5)
                        .ToListAsync();
        return Ok(logs);
    }

    [HttpGet("/ProcessedRecordsInMinutes/{minutes}")]
    public async Task<IActionResult> ProcessedRecordsInMinutes(int minutes = 15)
    {
        try
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-minutes);

            var results = await GetRequestLogStatsAsync(startTime, endTime);

            return Ok(System.Text.Json.JsonSerializer.Serialize(results));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    private async Task<List<RequestsPerMinuteDto>> GetRequestLogStatsAsync(DateTime startTime, DateTime endTime)
    {
        var results = new List<RequestsPerMinuteDto>();

        await using var connection = _db.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT 
            date_trunc('minute', ""StartTime"") AS ""MinuteBucket"", 
            COUNT(*) AS ""TotalRequests""
        FROM ""RequestLogs""
        WHERE ""StartTime"" BETWEEN @startTime AND @endTime
        GROUP BY ""MinuteBucket""
        ORDER BY ""MinuteBucket""";

        var startParam = command.CreateParameter();
        startParam.ParameterName = "@startTime";
        startParam.Value = startTime;
        command.Parameters.Add(startParam);

        var endParam = command.CreateParameter();
        endParam.ParameterName = "@endTime";
        endParam.Value = endTime;
        command.Parameters.Add(endParam);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new RequestsPerMinuteDto
            {
                MinuteBucket = reader.GetDateTime(0),
                TotalRequests = reader.GetInt32(1)
            });
        }

        return results;
    }

}

public class RequestsPerMinuteDto
{
    public DateTime MinuteBucket { get; set; }
    public int TotalRequests { get; set; }
}