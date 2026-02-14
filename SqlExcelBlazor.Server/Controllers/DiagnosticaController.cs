using Microsoft.AspNetCore.Mvc;

namespace SqlExcelBlazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticaController : ControllerBase
{
    private readonly ILogger<DiagnosticaController> _logger;
    private readonly IWebHostEnvironment _env;

    public DiagnosticaController(ILogger<DiagnosticaController> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            Message = "pong",
            Timestamp = DateTime.UtcNow,
            Server = "SqlExcelBlazor.Server",
            Environment = _env.EnvironmentName
        });
    }

    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        
        return Ok(new
        {
            Server = new
            {
                Environment = _env.EnvironmentName,
                ContentRootPath = _env.ContentRootPath,
                WebRootPath = _env.WebRootPath,
                ApplicationName = _env.ApplicationName
            },
            Request = new
            {
                Scheme = Request.Scheme,
                Host = Request.Host.ToString(),
                Path = Request.Path.ToString(),
                QueryString = Request.QueryString.ToString(),
                Headers = headers
            },
            Connection = new
            {
                RemoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                RemotePort = HttpContext.Connection.RemotePort,
                LocalIpAddress = HttpContext.Connection.LocalIpAddress?.ToString(),
                LocalPort = HttpContext.Connection.LocalPort
            }
        });
    }

    [HttpGet("test-cors")]
    public IActionResult TestCors()
    {
        Response.Headers.Append("X-Custom-Header", "CORS-Test");
        return Ok(new
        {
            Message = "Se vedi questo messaggio, CORS funziona!",
            Timestamp = DateTime.UtcNow
        });
    }
}
