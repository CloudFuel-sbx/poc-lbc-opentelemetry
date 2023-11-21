using Microsoft.AspNetCore.Mvc;

namespace LBC.OpenTelemetry.POC.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get(CancellationToken cancellationToken)
    {
        return Ok("Healthy");
    }
}