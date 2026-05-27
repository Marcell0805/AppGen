using Microsoft.AspNetCore.Mvc;

namespace Hello_World.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy", application = "Hello_World" });
}
