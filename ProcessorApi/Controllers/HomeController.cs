using Microsoft.AspNetCore.Mvc;
using ProcessorApi.Models;

namespace ProcessorApi.Controllers;

[ApiController]
[Route("[controller]")]
public class HomeController : ControllerBase
{
    public HomeController(RequestDbContext db)
    {
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
        return Ok("Processor API Working.");
    }
}