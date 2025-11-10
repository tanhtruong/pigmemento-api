using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pigmemento.Api.Data.Seed;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("admin/cases")]
[Authorize(Roles = "admin")]
public class AdminCasesController : ControllerBase
{
    private readonly CaseSeeder _seeder;
    private readonly IWebHostEnvironment _env;

    public AdminCasesController(CaseSeeder seeder, IWebHostEnvironment env)
    {
        _seeder = seeder;
        _env = env;
    }

    /// <summary>
    /// Import cases from a CSV on the server.
    /// Default path: Data/Seed/metadata.csv (relative to content root).
    /// </summary>
    /// POST /admin/cases/import
    [HttpPost("import")]
    [RequestSizeLimit(50_000_000)] // 50 MB, adjust as needed
    public async Task<ActionResult<object>> ImportCases([FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                error = "No file uploaded or file was empty. Use form field 'file'."
            });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                error = "Invalid file type. Expected a .csv file."
            });
        }
        
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _seeder.SeedFromCsvAsync(stream);

            return Ok(new
            {
                message = "Case import competed.",
                imported = result.Imported,
                skippedExisting = result.SkippedExisting,
                fileName = file.FileName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Import failed.",
                detail = ex.Message
            });
        }
    }

}