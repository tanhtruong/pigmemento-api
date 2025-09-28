using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Contracts;
using Pigmemento.Api.Data;
using Pigmemento.Api.Models;

namespace Pigmemento.Api.Controllers;



[ApiController]
[Route("[controller]")]
public class WaitlistController : ControllerBase
{
    private readonly AppDbContext _db;
    public WaitlistController(AppDbContext db) => _db = db;

    [HttpGet("/health")]
    public IActionResult Health() => Ok("OK");

    [HttpPost]
    public async Task<IActionResult> Join([FromBody] WaitlistCreate payload, CancellationToken ct)
    {
        var email = payload.Email.Trim();
        var name  = (payload.Name ?? "").Trim();

        var exists = await _db.WaitlistSubscribers.AnyAsync(w => w.Email == email, ct);
    
        if (exists)
        {
            return Ok("Already on the waitlist.");
        }

        _db.WaitlistSubscribers.Add(new WaitlistSubscriber { Email = email, Name = name });
        try
        {
            await _db.SaveChangesAsync(ct);
            return Created(string.Empty, "Added to waitlist!");
        }
        catch (DbUpdateException)
        {
            return Ok("Already on the waitlist.");
        }
    }
}
