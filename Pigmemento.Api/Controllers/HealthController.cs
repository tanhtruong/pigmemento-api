using Microsoft.AspNetCore.Mvc;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new {ok = true, service = "Pigmemento.Api"});
}