using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class SecureController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IActionResult Get() => Ok(new { message = "You are authenticated 🎉" });
}